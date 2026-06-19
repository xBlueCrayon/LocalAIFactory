using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Workspaces.Management;

// Phase 1: records proposed changes and their history. Applying changes to disk is intentionally
// disabled here and will be enabled in Phase 2 (autonomous editing) without changing this surface.
public sealed class WorkspaceModificationService : IWorkspaceModificationService
{
    private readonly AppDbContext _db;
    public WorkspaceModificationService(AppDbContext db) => _db = db;

    public async Task<WorkspaceChange> ProposeChangeAsync(
        int workspaceId, string relativePath, string? newContent, string? reason,
        string? prompt, string? modelUsed, CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct)
                 ?? throw new InvalidOperationException("Workspace not found.");

        // Best-effort previous content: read from the recorded RootPath if present (read-only).
        string? previous = null;
        if (!string.IsNullOrWhiteSpace(ws.RootPath))
        {
            try
            {
                var full = Path.Combine(ws.RootPath!, relativePath);
                if (File.Exists(full)) previous = await File.ReadAllTextAsync(full, ct);
            }
            catch { /* ignore */ }
        }

        var change = new WorkspaceChange
        {
            WorkspaceId = ws.Id,
            RelativePath = relativePath,
            PreviousContent = previous,
            NewContent = newContent,
            Reason = reason,
            Prompt = prompt,
            ModelUsed = modelUsed,
            Status = WorkspaceChangeStatus.Proposed
        };
        _db.WorkspaceChanges.Add(change);
        ws.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return change;
    }

    public Task ApplyChangeAsync(int changeId, CancellationToken ct = default)
        => throw new NotSupportedException(
            "Applying changes to disk is disabled in Phase 1. Enable workspace editing in Phase 2 to apply proposed changes.");

    public async Task<IReadOnlyList<WorkspaceChange>> ListChangesAsync(int workspaceId, CancellationToken ct = default)
        => await _db.WorkspaceChanges.AsNoTracking()
            .Where(c => c.WorkspaceId == workspaceId)
            .OrderByDescending(c => c.Id)
            .ToListAsync(ct);

    public async Task<WorkspaceChange?> GetChangeAsync(int changeId, CancellationToken ct = default)
        => await _db.WorkspaceChanges.AsNoTracking().FirstOrDefaultAsync(c => c.Id == changeId, ct);
}
