using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// Substrate that supports both single-model operation and future multi-model comparison.
public class PromptRun
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? ChatSessionId { get; set; }
    public TaskType TaskType { get; set; } = TaskType.Chat;
    public string? UserPrompt { get; set; }
    public string? RetrievedContextJson { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ModelOutput> Outputs { get; set; } = new List<ModelOutput>();
}

public class ModelOutput
{
    public int Id { get; set; }
    public int PromptRunId { get; set; }
    public PromptRun? PromptRun { get; set; }
    public int? ModelConfigurationId { get; set; }
    public ModelConfiguration? ModelConfiguration { get; set; }
    public ModelOutputKind Kind { get; set; } = ModelOutputKind.Primary;
    public string? Content { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }
    public int? LatencyMs { get; set; }
    public bool IsApproved { get; set; }
    public MessageRating Rating { get; set; } = MessageRating.None;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class ExtractedCodeBlock
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? ImportedConversationId { get; set; }
    public int? SourceKnowledgeItemId { get; set; }
    public string? Language { get; set; }
    public string? Content { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.NeedsReview;
    public int? PromotedToApprovedCodeSnippetId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
