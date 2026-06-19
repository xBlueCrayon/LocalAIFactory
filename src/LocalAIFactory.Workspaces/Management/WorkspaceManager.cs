using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Workspaces.Management;

// Creates and lists workspaces. In Phase 1 a workspace records metadata and a file inventory derived
// from the project's imported files; it does not copy files to a separate editable location yet.
public sealed class WorkspaceManager : IWorkspaceManager
{
    private readonly AppDbContext _db;
    public WorkspaceManager(AppDbContext db) => _db = db;

    public async Task<Workspace> CreateWorkspaceAsync(int projectId, string? name, string? description, bool fromOriginalImport, CancellationToken ct = default)
    {
        var nextVersion = (await _db.Workspaces.Where(w => w.ProjectId == projectId)
            .Select(w => (int?)w.Version).MaxAsync(ct) ?? 0) + 1;

        var latestJob = await _db.IngestionJobs
            .Where(j => j.ProjectId == projectId && j.Status == IngestionJobStatus.Completed)
            .OrderByDescending(j => j.Id).FirstOrDefaultAsync(ct);

        var ws = new Workspace
        {
            ProjectId = projectId,
            IngestionJobId = latestJob?.Id,
            Name = string.IsNullOrWhiteSpace(name) ? (fromOriginalImport ? "Original Import" : $"Workspace v{nextVersion}") : name!,
            Description = description,
            RootPath = latestJob?.ExtractedRoot,
            Version = nextVersion,
            Status = WorkspaceStatus.Active,
            IsOriginalImport = fromOriginalImport
        };
        _db.Workspaces.Add(ws);
        await _db.SaveChangesAsync(ct);

        // Seed the file inventory from the project's imported (non-skipped) files.
        var files = await _db.ImportedFiles.AsNoTracking()
            .Where(f => f.ProjectId == projectId && !f.Skipped)
            .Select(f => new { f.RelativePath, f.Sha256, f.FileClass, f.SizeBytes, f.CreatedUtc })
            .ToListAsync(ct);

        foreach (var f in files)
        {
            _db.WorkspaceFiles.Add(new WorkspaceFile
            {
                WorkspaceId = ws.Id,
                RelativePath = f.RelativePath ?? "",
                Hash = f.Sha256,
                FileType = f.FileClass,
                SizeBytes = f.SizeBytes,
                LastModifiedUtc = f.CreatedUtc
            });
        }
        await _db.SaveChangesAsync(ct);
        return ws;
    }

    public async Task<IReadOnlyList<Workspace>> ListAsync(int? projectId, CancellationToken ct = default)
        => await _db.Workspaces.AsNoTracking()
            .Where(w => projectId == null || w.ProjectId == projectId)
            .OrderByDescending(w => w.Id)
            .ToListAsync(ct);

    public async Task<Workspace?> GetAsync(int workspaceId, CancellationToken ct = default)
        => await _db.Workspaces.AsNoTracking().FirstOrDefaultAsync(w => w.Id == workspaceId, ct);

    public async Task<IReadOnlyList<WorkspaceFile>> ListFilesAsync(int workspaceId, CancellationToken ct = default)
        => await _db.WorkspaceFiles.AsNoTracking()
            .Where(f => f.WorkspaceId == workspaceId)
            .OrderBy(f => f.RelativePath)
            .ToListAsync(ct);
}
