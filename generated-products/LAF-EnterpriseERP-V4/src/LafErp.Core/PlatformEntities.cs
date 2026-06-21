namespace LafErp.Core;

// ---------------- Workflow ----------------

/// <summary>
/// Workflow definition for a document type: a set of states, allowed transitions, and an optional
/// amount threshold above which a higher-privilege approval role is required.
/// </summary>
public class WorkflowDefinition : EntityBase
{
    public string DocType { get; set; } = string.Empty; // e.g. "SalesOrder"
    public string Name { get; set; } = string.Empty;
    public bool MakerCannotApprove { get; set; } = true;
    public decimal ApprovalThreshold { get; set; } = 0m; // amount above which ApproverRole is required
    public string SubmitRole { get; set; } = string.Empty;
    public string ApproverRole { get; set; } = string.Empty;
    public List<WorkflowTransition> Transitions { get; set; } = new();
}

public class WorkflowTransition : EntityBase
{
    public int WorkflowDefinitionId { get; set; }
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public WorkflowAction Action { get; set; }
    public string AllowedRole { get; set; } = string.Empty;
}

/// <summary>Live workflow state for one document instance.</summary>
public class WorkflowInstance : EntityBase
{
    public int WorkflowDefinitionId { get; set; }
    public string DocType { get; set; } = string.Empty;
    public int DocumentId { get; set; }
    public string CurrentState { get; set; } = "Draft";
    public string SubmittedBy { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public List<WorkflowApproval> Approvals { get; set; } = new();
}

public class WorkflowApproval : EntityBase
{
    public int WorkflowInstanceId { get; set; }
    public WorkflowAction Action { get; set; }
    public string ActedBy { get; set; } = string.Empty;
    public string FromState { get; set; } = string.Empty;
    public string ToState { get; set; } = string.Empty;
    public string? Reason { get; set; }
    public DateTime ActedUtc { get; set; }
}

// ---------------- Audit & Security ----------------

public class AuditEvent : EntityBase
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string Action { get; set; } = string.Empty; // Create/Update/Submit/Approve/Reject/Cancel
    public string PerformedBy { get; set; } = string.Empty;
    public string? Details { get; set; }
    public DateTime EventUtc { get; set; }
}

public class AppUser : EntityBase
{
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public List<AppUserRole> Roles { get; set; } = new();
}

public class AppRole : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class AppUserRole : EntityBase
{
    public int AppUserId { get; set; }
    public int AppRoleId { get; set; }
    public AppRole? Role { get; set; }
}

/// <summary>Role-based permission for a doctype + action (clean-room role permission matrix).</summary>
public class RolePermission : EntityBase
{
    public string RoleName { get; set; } = string.Empty;
    public string DocType { get; set; } = string.Empty;
    public bool CanRead { get; set; }
    public bool CanCreate { get; set; }
    public bool CanWrite { get; set; }
    public bool CanSubmit { get; set; }
    public bool CanApprove { get; set; }
    public bool CanCancel { get; set; }
}

// ---------------- Import / Reporting ----------------

public class ImportBatch : EntityBase
{
    public string DocType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int ImportedRows { get; set; }
    public int FailedRows { get; set; }
    public string? Errors { get; set; }
}

public class ReportDefinition : EntityBase
{
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string ReportType { get; set; } = "Query"; // Query/Script/Print/Dashboard
    public string? Description { get; set; }
}
