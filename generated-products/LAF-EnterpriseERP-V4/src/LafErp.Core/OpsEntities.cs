namespace LafErp.Core;

public class Lead : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Company { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public LeadStatus Status { get; set; } = LeadStatus.Open;
    public string? Source { get; set; }
    public int? ConvertedCustomerId { get; set; }
}

public class Opportunity : EntityBase
{
    public int? LeadId { get; set; }
    public int? CustomerId { get; set; }
    public string Title { get; set; } = string.Empty;
    public OpportunityStage Stage { get; set; } = OpportunityStage.Prospecting;
    public decimal EstimatedValue { get; set; }
    public int ProbabilityPercent { get; set; }
}

public class Project : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public ProjectStatus Status { get; set; } = ProjectStatus.Open;
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal PercentComplete { get; set; }
    public List<ProjectTask> Tasks { get; set; } = new();
}

public class ProjectTask : EntityBase
{
    public int ProjectId { get; set; }
    public string Subject { get; set; } = string.Empty;
    public LafErp.Core.TaskStatus Status { get; set; } = LafErp.Core.TaskStatus.Open;
    public string? AssignedTo { get; set; }
    public DateTime? DueDate { get; set; }
    public bool RequiresApproval { get; set; }
    public bool IsApproved { get; set; }
}

public class SupportTicket : EntityBase
{
    public string Subject { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public TicketStatus Status { get; set; } = TicketStatus.Open;
    public TicketPriority Priority { get; set; } = TicketPriority.Medium;
    public string? AssignedTo { get; set; }
    public DateTime? EscalatedUtc { get; set; }
    public string? Resolution { get; set; }
}

public class Asset : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public int CompanyId { get; set; }
    public int? ItemId { get; set; }
    public decimal PurchaseValue { get; set; }
    public DateTime? PurchaseDate { get; set; }
    public AssetStatus Status { get; set; } = AssetStatus.Draft;
    public string? Location { get; set; }
    public DateTime? NextMaintenanceDate { get; set; }
}
