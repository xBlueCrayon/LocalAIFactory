using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// R2-P0B: server-side access control over MSSQL. All gating decisions live here so they are enforced
// consistently (not just hidden in the UI) and are unit-testable without a web host.
public interface IAccessControlService
{
    // Resolve the user for a Windows identity, auto-provisioning a deny-by-default Viewer on first sight.
    // The configured bootstrap-admin identity is provisioned as Admin. Updates LastSeenUtc.
    Task<UserAccount> ResolveUserAsync(string windowsIdentity, string? sid, string? displayName, string? bootstrapAdmin, CancellationToken ct = default);

    // Project access (Admin bypasses; otherwise requires an explicit ProjectAccess row). Disabled users get nothing.
    Task<bool> CanAccessProjectAsync(UserAccount user, int projectId, CancellationToken ct = default);
    Task<HashSet<int>> AccessibleProjectIdsAsync(UserAccount user, CancellationToken ct = default);

    // Coarse role gates (server-side). Disabled users are denied everything.
    static bool CanImport(UserAccount? u) => u is { Enabled: true, Role: UserRole.Admin };
    static bool CanManage(UserAccount? u) => u is { Enabled: true, Role: UserRole.Admin };
    static bool CanQuery(UserAccount? u) => u is { Enabled: true };
}

// R2-P0B: append-only security audit trail (distinct from the Phase-1 IAuditService/AuditLog). Actor + IP are
// supplied by the caller (from the resolved user + request).
public interface IAuditTrailService
{
    Task WriteAsync(UserAccount? actor, string? ip, AuditEventType type, string action,
        string? targetType = null, string? targetId = null, int? projectId = null, string? detail = null,
        CancellationToken ct = default);
}

// R2-P0B: the current request's authenticated user (resolved from the Windows principal). Web-layer concern.
public interface ICurrentUserService
{
    UserAccount? User { get; }
    string? IpAddress { get; }
    bool IsAdmin => User is { Enabled: true, Role: UserRole.Admin };
    bool IsAuthenticated => User is { Enabled: true };
}
