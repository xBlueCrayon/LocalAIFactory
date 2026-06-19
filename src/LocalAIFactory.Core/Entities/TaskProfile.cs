using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// Data-driven model routing. One profile per TaskType. Single-model operation is the default:
// every profile may point PrimaryModelId at the same ModelConfiguration with validation/comparison disabled.
public class TaskProfile
{
    public int Id { get; set; }
    public TaskType TaskType { get; set; }
    public string Name { get; set; } = "";

    public int? PrimaryModelId { get; set; }
    public ModelConfiguration? PrimaryModel { get; set; }

    public bool ValidationEnabled { get; set; }
    public int? ValidationModelId { get; set; }
    public ModelConfiguration? ValidationModel { get; set; }

    public bool ComparisonEnabled { get; set; }
    public int? ComparisonModelId { get; set; }
    public ModelConfiguration? ComparisonModel { get; set; }

    public bool UseKnowledgeBase { get; set; } = true;
    public bool UseProjectMemory { get; set; } = true;
    public bool UseKnowledgeGraph { get; set; } = true;

    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public int ContextWindowHint { get; set; } = 8192;

    public bool LocalOnly { get; set; }
    public bool RequireApprovalBeforeCloudUse { get; set; } = true;

    public bool IsEnabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
