using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// R2-P0B: a pilot user, identified by their Windows account. No passwords stored — Windows/IIS authenticates;
// this row holds only the app-level role and lifecycle. New users are deny-by-default (Viewer, no project access).
public class UserAccount : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();
    public string WindowsIdentity { get; set; } = ""; // DOMAIN\user (unique, case-insensitive)
    public string? Sid { get; set; }
    public string DisplayName { get; set; } = "";
    public UserRole Role { get; set; } = UserRole.Viewer;
    public bool Enabled { get; set; } = true;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenUtc { get; set; } = DateTime.UtcNow;
}

// R2-P0B: an explicit grant of a user to a project. Absence of a row denies (Admins bypass). Granting/revoking
// is Admin-only and audited.
public class ProjectAccess
{
    public int Id { get; set; }
    public int UserAccountId { get; set; }
    public int ProjectId { get; set; }
    public AccessLevel AccessLevel { get; set; } = AccessLevel.Read;
    public int? GrantedByUserId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

// R2-P0B: an append-only audit record. Never updated or deleted. Answers who did what, when, to which project,
// and whether access was denied.
public class AuditEvent : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();
    public int? UserAccountId { get; set; }
    public string? WindowsIdentity { get; set; }
    public AuditEventType EventType { get; set; }
    public string Action { get; set; } = "";
    public string? TargetType { get; set; }
    public string? TargetId { get; set; }
    public int? ProjectId { get; set; }
    public string? Detail { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
