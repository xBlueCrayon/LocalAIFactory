using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class ImportedFile
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? IngestionJobId { get; set; }
    public string FileName { get; set; } = "";
    public string? RelativePath { get; set; }
    public string Extension { get; set; } = "";
    public string? ContentType { get; set; }
    public FileClass FileClass { get; set; } = FileClass.Unknown;
    public long SizeBytes { get; set; }
    public string? Sha256 { get; set; }
    public string? RawText { get; set; }
    public string? StoredPath { get; set; }
    public bool Skipped { get; set; }
    public string? SkipReason { get; set; }
    public ImportStatus Status { get; set; } = ImportStatus.Pending;
    public int? KnowledgeItemId { get; set; }
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
