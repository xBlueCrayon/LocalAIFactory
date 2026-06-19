using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class AgentTask
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? ChatSessionId { get; set; }
    public string Title { get; set; } = "";
    public string? Goal { get; set; }
    public AgentTaskStatus Status { get; set; } = AgentTaskStatus.Pending;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedUtc { get; set; }

    public ICollection<AgentStep> Steps { get; set; } = new List<AgentStep>();
}

public class AgentStep
{
    public int Id { get; set; }
    public int AgentTaskId { get; set; }
    public AgentTask? AgentTask { get; set; }
    public int StepIndex { get; set; }
    public AgentStepKind Kind { get; set; }
    public string? Input { get; set; }
    public string? Output { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
