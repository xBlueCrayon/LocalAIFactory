using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-007: the immutable SourceArtifact — the permanent raw record of an imported file. Never edited
// after import; everything derived (knowledge items, provenance, future code symbols) traces back to it.
public class ImportedFile : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // KE-007 portable identity.
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? IngestionJobId { get; set; }
    public string FileName { get; set; } = "";
    public string? RelativePath { get; set; }
    public string Extension { get; set; } = "";
    public string? ContentType { get; set; }
    public FileClass FileClass { get; set; } = FileClass.Unknown;
    public ArtifactSourceSystem SourceSystem { get; set; } = ArtifactSourceSystem.Upload; // KE-007
    public string? DetectedLanguage { get; set; } // KE-007: e.g. "csharp", "sql", "markdown" — feeds KE-008/009.
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? RawText { get; set; }
    public string? StoredPath { get; set; }
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    // R2-P0A: honest per-file extraction outcome (set by the structural stores / pipeline). Feeds the gap report.
    public ExtractionStatus ExtractionStatus { get; set; } = ExtractionStatus.NotAttempted;
    public string? ExtractionNote { get; set; } // e.g. a parse-error message — surfaced in the gap report.
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public int? KnowledgeItemId { get; set; }
    // KE-004: when a file at the same path is re-imported with changed content, the new artifact points
    // back to the prior one (minimal raw-artifact versioning; full lineage is KE-007's SourceArtifact).
    public int? SupersedesImportedFileId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class ImportedConversation
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public ConversationSource Source { get; set; } = ConversationSource.ChatGpt;
    public string Title { get; set; } = "";
    public string? RawJson { get; set; }
    public int MessageCount { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.NeedsReview;
    public int? KnowledgeItemId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ImportedConversationMessage> Messages { get; set; } = new List<ImportedConversationMessage>();
}

public class ImportedConversationMessage
{
    public int Id { get; set; }
    public int ImportedConversationId { get; set; }
    public ImportedConversation? ImportedConversation { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
    public int OrderIndex { get; set; }
}
