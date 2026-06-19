using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// A working copy of an imported project. Phase 1 records metadata and history; Phase 2 will
// perform real on-disk editing, diffing and building against the workspace.
public class Workspace
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? IngestionJobId { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public string? RootPath { get; set; }
    public int Version { get; set; }
    public WorkspaceStatus Status { get; set; } = WorkspaceStatus.Active;
    public bool IsOriginalImport { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<WorkspaceSnapshot> Snapshots { get; set; } = new List<WorkspaceSnapshot>();
    public ICollection<WorkspaceFile> Files { get; set; } = new List<WorkspaceFile>();
    public ICollection<WorkspaceChange> Changes { get; set; } = new List<WorkspaceChange>();
}

// Lightweight source control: a point-in-time marker taken before AI modifications.
public class WorkspaceSnapshot
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public int? ProjectId { get; set; }
    public string? Description { get; set; }
    public string? CreatedBy { get; set; }
    public int FileCount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

// A single proposed/applied file modification. Becomes debugging history and future knowledge.
public class WorkspaceChange
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public int? WorkspaceSnapshotId { get; set; }
    public string RelativePath { get; set; } = "";
    public string? PreviousContent { get; set; }
    public string? NewContent { get; set; }
    public string? Reason { get; set; }
    public string? Prompt { get; set; }
    public string? ModelUsed { get; set; }
    public WorkspaceChangeStatus Status { get; set; } = WorkspaceChangeStatus.Proposed;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

// Tracks files belonging to a workspace.
public class WorkspaceFile
{
    public int Id { get; set; }
    public int WorkspaceId { get; set; }
    public Workspace? Workspace { get; set; }
    public string RelativePath { get; set; } = "";
    public string? Hash { get; set; }
    public FileClass FileType { get; set; } = FileClass.Unknown;
    public long SizeBytes { get; set; }
    public DateTime? LastModifiedUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
