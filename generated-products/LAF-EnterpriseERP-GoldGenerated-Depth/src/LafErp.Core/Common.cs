namespace LafErp.Core;

/// <summary>
/// Document lifecycle, modelled clean-room after the common ERP "draft / submitted / cancelled"
/// pattern (conceptually similar to Frappe's docstatus 0/1/2). Submitted financial/stock documents
/// are immutable and must be reversed or cancelled rather than silently edited.
/// </summary>
public enum DocStatus
{
    Draft = 0,
    Submitted = 1,
    Cancelled = 2
}

/// <summary>Accounting root classification for a chart-of-accounts node.</summary>
public enum RootType
{
    Asset,
    Liability,
    Equity,
    Income,
    Expense
}

public enum PartyType
{
    Customer,
    Supplier
}

/// <summary>Direction of a stock ledger movement.</summary>
public enum StockDirection
{
    In,
    Out
}

public enum WorkflowAction
{
    Submit,
    Approve,
    Reject,
    Cancel
}

public enum LeadStatus { Open, Working, Qualified, Converted, Lost }
public enum OpportunityStage { Prospecting, Qualification, Proposal, Negotiation, Won, Lost }
public enum TicketStatus { Open, InProgress, Escalated, Resolved, Closed }
public enum TicketPriority { Low, Medium, High, Urgent }
public enum ProjectStatus { Open, OnHold, Completed, Cancelled }
public enum TaskStatus { Open, InProgress, Blocked, Completed, Cancelled }
public enum AssetStatus { Draft, InUse, UnderMaintenance, Scrapped, Sold }

/// <summary>Base for all persisted entities: surrogate key + audit timestamps + soft delete.</summary>
public abstract class EntityBase
{
    public int Id { get; set; }
    public DateTime CreatedUtc { get; set; }
    public DateTime? UpdatedUtc { get; set; }
    public string CreatedBy { get; set; } = "system";
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}

/// <summary>Base for documents that flow through the submit/approve workflow.</summary>
public abstract class DocumentBase : EntityBase
{
    public string DocNo { get; set; } = string.Empty;
    public DocStatus Status { get; set; } = DocStatus.Draft;
    public DateTime PostingDate { get; set; }
    /// <summary>Optimistic-concurrency token.</summary>
    public byte[]? RowVersion { get; set; }
}

/// <summary>Domain rule violation — distinct from infrastructure errors.</summary>
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
}
