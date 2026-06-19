using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Dtos;

public sealed class ResolvedTaskProfile
{
    public TaskType TaskType { get; set; }
    public ModelConfiguration? PrimaryModel { get; set; }
    public ModelConfiguration? ValidationModel { get; set; }
    public ModelConfiguration? ComparisonModel { get; set; }
    public bool ValidationEnabled { get; set; }
    public bool ComparisonEnabled { get; set; }
    public bool UseKnowledgeBase { get; set; } = true;
    public bool UseProjectMemory { get; set; } = true;
    public bool UseKnowledgeGraph { get; set; } = true;
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public int ContextWindowHint { get; set; } = 8192;
    public bool LocalOnly { get; set; }
    public bool RequireApprovalBeforeCloudUse { get; set; } = true;
    public string? ResolutionNote { get; set; }

    public bool HasPrimary => PrimaryModel is not null;
}

public sealed class ModelExecutionRequest
{
    public TaskType TaskType { get; set; } = TaskType.Chat;
    public int? ProjectId { get; set; }
    public int? ChatSessionId { get; set; }
    public int? OverrideModelId { get; set; }
    public string UserPrompt { get; set; } = "";
    public string? SystemPromptOverride { get; set; }
    public List<ChatMessageDto> History { get; set; } = new();
    public bool AllowCloud { get; set; }
}

public sealed class ModelExecutionResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int PromptRunId { get; set; }
    public int? PrimaryOutputId { get; set; }
    public string PrimaryOutput { get; set; } = "";
    public string? PrimaryModelName { get; set; }
    public string? ValidationOutput { get; set; }
    public string? Notes { get; set; }
    public RetrievedContext Context { get; set; } = new();
}

public sealed class IngestionProgress
{
    public int JobId { get; set; }
    public IngestionJobStatus Status { get; set; }
    public IngestionPhase Phase { get; set; }
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public int ChunkCount { get; set; }
    public int EmbeddedCount { get; set; }
    public string? Error { get; set; }
}
