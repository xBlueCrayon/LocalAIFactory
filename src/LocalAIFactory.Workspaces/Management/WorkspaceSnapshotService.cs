using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Workspaces.Management;

// Lightweight source control: capture a point-in-time marker (and file count) before AI modifications.
public sealed class WorkspaceSnapshotService : IWorkspaceSnapshotService
{
    private readonly AppDbContext _db;
    public WorkspaceSnapshotService(AppDbContext db) => _db = db;

    public async Task<WorkspaceSnapshot> CreateSnapshotAsync(int workspaceId, string? description, string? user, CancellationToken ct = default)
    {
        var ws = await _db.Workspaces.FirstOrDefaultAsync(w => w.Id == workspaceId, ct)
                 ?? throw new InvalidOperationException("Workspace not found.");
        var fileCount = await _db.WorkspaceFiles.CountAsync(f => f.WorkspaceId == workspaceId, ct);

        var snapshot = new WorkspaceSnapshot
        {
            WorkspaceId = ws.Id,
            ProjectId = ws.ProjectId,
            Description = string.IsNullOrWhiteSpace(description) ? $"Snapshot {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC" : description,
            CreatedBy = string.IsNullOrWhiteSpace(user) ? "local" : user,
            FileCount = fileCount
        };
        _db.WorkspaceSnapshots.Add(snapshot);
        ws.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return snapshot;
    }

    public async Task<IReadOnlyList<WorkspaceSnapshot>> ListAsync(int workspaceId, CancellationToken ct = default)
        => await _db.WorkspaceSnapshots.AsNoTracking()
            .Where(s => s.WorkspaceId == workspaceId)
            .OrderByDescending(s => s.Id)
            .ToListAsync(ct);
}
