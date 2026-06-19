using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;

namespace LocalAIFactory.Core.Abstractions;

// Phase 1: schema + service surface for workspace management. Real on-disk editing is enabled in Phase 2.

public interface IWorkspaceManager
{
    Task<Workspace> CreateWorkspaceAsync(int projectId, string? name, string? description, bool fromOriginalImport, CancellationToken ct = default);
    Task<IReadOnlyList<Workspace>> ListAsync(int? projectId, CancellationToken ct = default);
    Task<Workspace?> GetAsync(int workspaceId, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceFile>> ListFilesAsync(int workspaceId, CancellationToken ct = default);
}

public interface IWorkspaceSnapshotService
{
    Task<WorkspaceSnapshot> CreateSnapshotAsync(int workspaceId, string? description, string? user, CancellationToken ct = default);
    Task<IReadOnlyList<WorkspaceSnapshot>> ListAsync(int workspaceId, CancellationToken ct = default);
}

public interface IWorkspaceModificationService
{
    // Phase 1: records a proposed change only; it does NOT write to disk.
    Task<WorkspaceChange> ProposeChangeAsync(
        int workspaceId, string relativePath, string? newContent, string? reason,
        string? prompt, string? modelUsed, CancellationToken ct = default);

    // Phase 1: guarded placeholder. Throws until on-disk editing is enabled in Phase 2.
    Task ApplyChangeAsync(int changeId, CancellationToken ct = default);

    Task<IReadOnlyList<WorkspaceChange>> ListChangesAsync(int workspaceId, CancellationToken ct = default);
    Task<WorkspaceChange?> GetChangeAsync(int changeId, CancellationToken ct = default);
}

public interface IDiffService
{
    DiffResult Diff(string? previous, string? current);
}
