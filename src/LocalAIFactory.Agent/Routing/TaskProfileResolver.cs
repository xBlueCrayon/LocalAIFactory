using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Agent.Routing;

// Turns a TaskType into a concrete plan. The fallback chain guarantees single-model operation:
// profile primary -> system default model -> any enabled model.
public sealed class TaskProfileResolver : ITaskProfileResolver
{
    private readonly AppDbContext _db;
    public TaskProfileResolver(AppDbContext db) => _db = db;

    public async Task<ResolvedTaskProfile> ResolveAsync(TaskType taskType, CancellationToken ct = default)
    {
        var profile = await _db.TaskProfiles
            .Include(p => p.PrimaryModel)
            .Include(p => p.ValidationModel)
            .Include(p => p.ComparisonModel)
            .FirstOrDefaultAsync(p => p.TaskType == taskType && p.IsEnabled, ct);

        var resolved = new ResolvedTaskProfile { TaskType = taskType };
        string? note = null;

        if (profile is not null)
        {
            resolved.UseKnowledgeBase = profile.UseKnowledgeBase;
            resolved.UseProjectMemory = profile.UseProjectMemory;
            resolved.UseKnowledgeGraph = profile.UseKnowledgeGraph;
            resolved.Temperature = profile.Temperature;
            resolved.MaxTokens = profile.MaxTokens;
            resolved.ContextWindowHint = profile.ContextWindowHint;
            resolved.LocalOnly = profile.LocalOnly;
            resolved.RequireApprovalBeforeCloudUse = profile.RequireApprovalBeforeCloudUse;
        }
        else
        {
            note = $"No task profile for {taskType}; using default routing.";
        }

        // Resolve primary model with fallback chain.
        ModelConfiguration? primary = (profile?.PrimaryModel is { IsEnabled: true }) ? profile.PrimaryModel : null;
        if (primary is null)
        {
            primary = await _db.ModelConfigurations.Where(m => m.IsEnabled)
                .OrderByDescending(m => m.IsDefault).FirstOrDefaultAsync(ct);
            if (primary is not null)
                note = (note is null ? "" : note + " ") + "Primary model not set/enabled; fell back to default enabled model.";
        }
        resolved.PrimaryModel = primary;

        if (profile is { ValidationEnabled: true } && profile.ValidationModel is { IsEnabled: true })
        {
            resolved.ValidationEnabled = true;
            resolved.ValidationModel = profile.ValidationModel;
        }
        if (profile is { ComparisonEnabled: true } && profile.ComparisonModel is { IsEnabled: true })
        {
            resolved.ComparisonEnabled = true;
            resolved.ComparisonModel = profile.ComparisonModel;
        }

        if (primary is null)
            note = (note is null ? "" : note + " ") + "No enabled model configured.";

        resolved.ResolutionNote = note;
        return resolved;
    }
}
