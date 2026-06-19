using System.Diagnostics;
using System.Text.Json;
using LocalAIFactory.Agent.Prompts;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Agent.Execution;

// The single execution path for every model call (chat and ingestion). Honors task-profile policy,
// guarantees single-model operation, and records every run as PromptRun + ModelOutput rows.
public sealed class ModelExecutionService : IModelExecutionService
{
    private readonly ITaskProfileResolver _resolver;
    private readonly IModelRouter _router;
    private readonly IRagContextBuilder _rag;
    private readonly IApiKeyProtector _protector;
    private readonly AppDbContext _db;

    public ModelExecutionService(
        ITaskProfileResolver resolver, IModelRouter router, IRagContextBuilder rag,
        IApiKeyProtector protector, AppDbContext db)
    {
        _resolver = resolver; _router = router; _rag = rag; _protector = protector; _db = db;
    }

    public async Task<ModelExecutionResult> ExecuteAsync(ModelExecutionRequest request, CancellationToken ct = default)
    {
        var resolved = await _resolver.ResolveAsync(request.TaskType, ct);

        // Per-request override (e.g. the chat model dropdown) takes precedence over the profile primary.
        ModelConfiguration? primary = resolved.PrimaryModel;
        if (request.OverrideModelId is int oid)
        {
            var ov = await _db.ModelConfigurations.FirstOrDefaultAsync(m => m.Id == oid, ct);
            if (ov is not null) primary = ov;
        }

        var result = new ModelExecutionResult();
        if (primary is null)
        {
            result.Success = false;
            result.Error = "No model is configured. Add and enable a model under Models, then point the task profile at it.";
            return result;
        }

        // Build retrieval context honoring the profile flags.
        RetrievedContext context = new();
        if (resolved.UseKnowledgeBase || resolved.UseProjectMemory || resolved.UseKnowledgeGraph)
        {
            context = await _rag.BuildAsync(
                request.ProjectId, request.UserPrompt,
                resolved.UseProjectMemory, resolved.UseKnowledgeBase, resolved.UseKnowledgeGraph, ct);
        }
        result.Context = context;

        var baseTemplate = request.SystemPromptOverride ?? await DefaultTemplateAsync(ct);
        var systemPrompt = PromptBuilder.BuildSystemPrompt(baseTemplate, context);

        var run = new PromptRun
        {
            ProjectId = request.ProjectId,
            ChatSessionId = request.ChatSessionId,
            TaskType = request.TaskType,
            UserPrompt = request.UserPrompt,
            RetrievedContextJson = JsonSerializer.Serialize(context.Items)
        };
        _db.PromptRuns.Add(run);
        await _db.SaveChangesAsync(ct);
        result.PromptRunId = run.Id;

        // Primary
        var (canPrimary, primaryNote, primaryKey) = CanUse(primary, resolved, request.AllowCloud);
        if (!canPrimary)
        {
            result.Success = false;
            result.Error = $"Primary model '{primary.Name}' cannot run: {primaryNote}";
            return result;
        }

        var primaryOut = await RunOneAsync(primary, primaryKey, systemPrompt, request, resolved, ct);
        var primaryRow = await SaveOutputAsync(run.Id, primary.Id, ModelOutputKind.Primary, primaryOut, ct);
        if (!primaryOut.Success)
        {
            result.Success = false;
            result.Error = primaryOut.Error ?? "Primary model call failed.";
            return result;
        }
        result.Success = true;
        result.PrimaryOutput = primaryOut.Content;
        result.PrimaryOutputId = primaryRow.Id;
        result.PrimaryModelName = primary.Name;
        var notes = new List<string>();
        if (resolved.ResolutionNote is not null) notes.Add(resolved.ResolutionNote);

        // Optional validation (non-blocking)
        if (resolved.ValidationEnabled && resolved.ValidationModel is not null)
        {
            var (canV, vNote, vKey) = CanUse(resolved.ValidationModel, resolved, request.AllowCloud);
            if (canV)
            {
                var vReq = new ModelExecutionRequest
                {
                    TaskType = request.TaskType,
                    ProjectId = request.ProjectId,
                    UserPrompt = request.UserPrompt
                };
                var vOut = await RunValidationAsync(resolved.ValidationModel, vKey, request.UserPrompt, primaryOut.Content, resolved, ct);
                await SaveOutputAsync(run.Id, resolved.ValidationModel.Id, ModelOutputKind.Validation, vOut, ct);
                if (vOut.Success) result.ValidationOutput = vOut.Content;
            }
            else notes.Add($"Validation skipped: {vNote}");
        }

        // Optional comparison (non-blocking, stored for side-by-side review)
        if (resolved.ComparisonEnabled && resolved.ComparisonModel is not null)
        {
            var (canC, cNote, cKey) = CanUse(resolved.ComparisonModel, resolved, request.AllowCloud);
            if (canC)
            {
                var cOut = await RunOneAsync(resolved.ComparisonModel, cKey, systemPrompt, request, resolved, ct);
                await SaveOutputAsync(run.Id, resolved.ComparisonModel.Id, ModelOutputKind.Comparison, cOut, ct);
            }
            else notes.Add($"Comparison skipped: {cNote}");
        }

        result.Notes = notes.Count == 0 ? null : string.Join(" ", notes);
        return result;
    }

    public async Task<string> CompleteSimpleAsync(TaskType taskType, string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var res = await ExecuteAsync(new ModelExecutionRequest
        {
            TaskType = taskType,
            SystemPromptOverride = systemPrompt,
            UserPrompt = userPrompt,
            AllowCloud = false
        }, ct);
        return res.Success ? res.PrimaryOutput : "";
    }

    private async Task<ChatCompletionResult> RunOneAsync(
        ModelConfiguration model, string? plaintextKey, string systemPrompt,
        ModelExecutionRequest request, ResolvedTaskProfile resolved, CancellationToken ct)
    {
        var provider = _router.Resolve(model.Provider);
        var cfg = WithKey(model, plaintextKey);
        var messages = new List<ChatMessageDto>(request.History) { new(ChatRole.User, request.UserPrompt) };
        var req = new ChatCompletionRequest
        {
            Model = model.ModelName,
            Temperature = resolved.Temperature,
            MaxTokens = resolved.MaxTokens,
            SystemPrompt = systemPrompt,
            Messages = messages
        };
        var sw = Stopwatch.StartNew();
        var res = await provider.CompleteAsync(cfg, req, ct);
        sw.Stop();
        _lastLatencyMs = (int)sw.ElapsedMilliseconds;
        return res;
    }

    private async Task<ChatCompletionResult> RunValidationAsync(
        ModelConfiguration model, string? plaintextKey, string userPrompt, string primaryAnswer,
        ResolvedTaskProfile resolved, CancellationToken ct)
    {
        var provider = _router.Resolve(model.Provider);
        var cfg = WithKey(model, plaintextKey);
        var sys = "You are a strict senior reviewer. Assess the assistant answer for correctness, security, and whether it "
                + "respects the stated requirements. Reply concisely with: VERDICT (OK / RISKS / WRONG), then bullet issues, then a corrected snippet only if needed.";
        var req = new ChatCompletionRequest
        {
            Model = model.ModelName,
            Temperature = 0.0,
            MaxTokens = Math.Min(resolved.MaxTokens, 1024),
            SystemPrompt = sys,
            Messages = new List<ChatMessageDto>
            {
                new(ChatRole.User, $"User request:\n{userPrompt}\n\nAssistant answer to review:\n{primaryAnswer}")
            }
        };
        var sw = Stopwatch.StartNew();
        var res = await provider.CompleteAsync(cfg, req, ct);
        sw.Stop();
        _lastLatencyMs = (int)sw.ElapsedMilliseconds;
        return res;
    }

    private int _lastLatencyMs;

    private async Task<ModelOutput> SaveOutputAsync(int runId, int modelId, ModelOutputKind kind, ChatCompletionResult res, CancellationToken ct)
    {
        var row = new ModelOutput
        {
            PromptRunId = runId,
            ModelConfigurationId = modelId,
            Kind = kind,
            Content = res.Success ? res.Content : ("[error] " + res.Error),
            PromptTokens = res.PromptTokens,
            CompletionTokens = res.CompletionTokens,
            LatencyMs = _lastLatencyMs
        };
        _db.ModelOutputs.Add(row);
        await _db.SaveChangesAsync(ct);
        return row;
    }

    private (bool ok, string note, string? key) CanUse(ModelConfiguration model, ResolvedTaskProfile resolved, bool allowCloud)
    {
        if (model.Provider == ModelProvider.Ollama) return (true, "", null);

        if (resolved.LocalOnly) return (false, "task profile is LocalOnly; cloud models are blocked.", null);
        if (resolved.RequireApprovalBeforeCloudUse && !allowCloud)
            return (false, "cloud use requires explicit approval (enable cloud for this action).", null);

        var key = _protector.Unprotect(model.ApiKeyEncrypted);
        if (string.IsNullOrEmpty(key)) return (false, "no API key configured for this model.", null);
        return (true, "", key);
    }

    private static ModelConfiguration WithKey(ModelConfiguration model, string? plaintextKey) => new()
    {
        Id = model.Id,
        Name = model.Name,
        Provider = model.Provider,
        ModelName = model.ModelName,
        BaseUrl = model.BaseUrl,
        ApiKeyEncrypted = plaintextKey,   // providers read this field as the key to send
        Temperature = model.Temperature,
        MaxTokens = model.MaxTokens,
        ContextWindowHint = model.ContextWindowHint,
        EmbeddingModel = model.EmbeddingModel
    };

    private async Task<string> DefaultTemplateAsync(CancellationToken ct)
    {
        var tpl = await _db.PromptTemplates
            .Where(t => t.Kind == "ChatSystem")
            .OrderByDescending(t => t.IsDefault).FirstOrDefaultAsync(ct);
        return tpl?.Content
            ?? "You are LocalAIFactory, a specialized .NET / MSSQL / EF Core software engineering assistant. Prioritise approved project knowledge.";
    }
}
