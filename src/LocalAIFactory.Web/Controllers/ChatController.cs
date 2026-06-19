using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Core.ViewModels;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Web.Controllers;

public class ChatController : Controller
{
    private readonly AppDbContext _db;
    private readonly IChatOrchestrator _chat;
    private readonly IChunkingService _chunking;
    private readonly IKnowledgeIndexer _indexer;
    private readonly RagOptions _rag;
    private readonly IServiceHealthCache _health;

    public ChatController(AppDbContext db, IChatOrchestrator chat, IChunkingService chunking, IKnowledgeIndexer indexer, IOptions<RagOptions> rag, IServiceHealthCache health)
    {
        _db = db; _chat = chat; _chunking = chunking; _indexer = indexer; _rag = rag.Value; _health = health;
    }

    public async Task<IActionResult> Index(int? sessionId, int? projectId, int? modelId, CancellationToken ct)
    {
        // Phase 1.2: surface model availability (cached; no probe here) so the page can warn up front.
        var health = _health.Current;
        ViewBag.ChatAvailable = health.ChatAvailable;
        ViewBag.HealthMode = health.ModeLabel;

        var vm = new ChatPageViewModel
        {
            Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct),
            Models = await _db.ModelConfigurations.Where(m => m.IsEnabled).OrderByDescending(m => m.IsDefault).ToListAsync(ct),
            Sessions = await _db.ChatSessions.OrderByDescending(s => s.UpdatedUtc).Take(50).ToListAsync(ct),
            SelectedProjectId = projectId,
            SelectedModelId = modelId
        };
        if (sessionId is int sid)
        {
            vm.CurrentSession = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sid, ct);
            vm.Messages = await _db.ChatMessages.Where(m => m.ChatSessionId == sid).OrderBy(m => m.Id).ToListAsync(ct);
            if (vm.CurrentSession != null)
            {
                vm.SelectedProjectId = vm.CurrentSession.ProjectId;
                vm.SelectedModelId = vm.CurrentSession.ModelConfigurationId;
            }

            // Phase 1.1: model names + PromptRun links for the improved message metadata.
            ViewBag.ModelNames = await _db.ModelConfigurations.AsNoTracking().ToDictionaryAsync(m => m.Id, m => m.Name, ct);
            var moIds = vm.Messages.Where(m => m.ModelOutputId != null).Select(m => m.ModelOutputId!.Value).Distinct().ToList();
            var moToRun = await _db.ModelOutputs.AsNoTracking().Where(o => moIds.Contains(o.Id))
                .Select(o => new { o.Id, o.PromptRunId }).ToListAsync(ct);
            var runByMo = moToRun.ToDictionary(x => x.Id, x => x.PromptRunId);
            ViewBag.PromptRunByMessage = vm.Messages
                .Where(m => m.ModelOutputId != null && runByMo.ContainsKey(m.ModelOutputId.Value))
                .ToDictionary(m => m.Id, m => runByMo[m.ModelOutputId!.Value]);
        }
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(ChatSendViewModel form, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(form.Message))
            return RedirectToAction(nameof(Index), new { sessionId = form.SessionId, projectId = form.ProjectId, modelId = form.ModelConfigurationId });

        var result = await _chat.SendAsync(new ChatTurnRequest
        {
            SessionId = form.SessionId,
            ProjectId = form.ProjectId,
            ModelConfigurationId = form.ModelConfigurationId,
            Message = form.Message
        }, ct);

        if (!result.Success) TempData["Error"] = result.Error;
        return RedirectToAction(nameof(Index), new { sessionId = result.SessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResponse(SaveResponseViewModel form, CancellationToken ct)
    {
        var msg = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == form.MessageId, ct);
        if (msg is null) { TempData["Error"] = "Message not found."; return RedirectToAction(nameof(Index)); }

        var title = string.IsNullOrWhiteSpace(form.Title) ? "Saved from chat" : form.Title;
        switch (form.Target)
        {
            case "ApprovedCode":
                _db.ApprovedCodeSnippets.Add(new ApprovedCodeSnippet
                {
                    ProjectId = form.ProjectId, Title = title,
                    Language = string.IsNullOrWhiteSpace(form.Language) ? "csharp" : form.Language!,
                    Framework = form.Framework, Content = msg.Content,
                    Explanation = "Saved from chat.", IsReusable = true, ApprovedUtc = DateTime.UtcNow
                });
                await _db.SaveChangesAsync(ct);
                TempData["Message"] = "Saved as approved code.";
                break;

            case "BusinessRule":
                _db.BusinessRules.Add(new BusinessRule
                {
                    ProjectId = form.ProjectId, Title = title, Content = msg.Content,
                    Status = BusinessRuleStatus.NeedsReview, IsApproved = false
                });
                await _db.SaveChangesAsync(ct);
                TempData["Message"] = "Saved as a business rule (needs review).";
                break;

            default: // Knowledge
                var ki = new KnowledgeItem
                {
                    ProjectId = form.ProjectId, Title = title, Content = msg.Content,
                    SourceType = SourceType.GeneratedCode, Status = KnowledgeStatus.NeedsReview, Confidence = 0.5
                };
                _db.KnowledgeItems.Add(ki);
                await _db.SaveChangesAsync(ct);
                int idx = 0;
                foreach (var chunk in _chunking.Chunk(ki.Content, _rag.MaxChunkChars, _rag.ChunkOverlap))
                    _db.KnowledgeChunks.Add(new KnowledgeChunk { KnowledgeItemId = ki.Id, ChunkIndex = idx++, Content = chunk, TokenCount = _chunking.EstimateTokens(chunk) });
                await _db.SaveChangesAsync(ct);
                try { await _indexer.IndexKnowledgeItemAsync(ki.Id, ct); } catch { }
                TempData["Message"] = "Saved as knowledge (needs review).";
                break;
        }
        return RedirectToAction(nameof(Index), new { sessionId = msg.ChatSessionId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Rate(int messageId, string rating, CancellationToken ct)
    {
        var msg = await _db.ChatMessages.FirstOrDefaultAsync(m => m.Id == messageId, ct);
        if (msg != null)
        {
            msg.Rating = rating == "useful" ? MessageRating.Useful : MessageRating.Wrong;
            if (msg.ModelOutputId is int moId)
            {
                var mo = await _db.ModelOutputs.FirstOrDefaultAsync(o => o.Id == moId, ct);
                if (mo != null) mo.Rating = msg.Rating;
            }
            await _db.SaveChangesAsync(ct);
        }
        return RedirectToAction(nameof(Index), new { sessionId = msg?.ChatSessionId });
    }
}
