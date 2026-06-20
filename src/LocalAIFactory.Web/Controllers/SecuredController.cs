using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

// R2-P0B: base for access-controlled controllers. Enforcement is SERVER-SIDE — controllers call RequireAdmin()
// / RequireProjectAsync() at the top of each action; hiding UI is never sufficient. Denials are audited.
public abstract class SecuredController : Controller
{
    protected readonly ICurrentUserService Me;
    protected readonly IAccessControlService Access;
    protected readonly IAuditTrailService Audit;

    protected SecuredController(ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
    { Me = me; Access = access; Audit = audit; }

    protected UserAccount? CurrentUser => Me.User;

    // Returns a 403 result if the user is not an enabled Admin; otherwise null (proceed). Audits the denial.
    protected async Task<IActionResult?> RequireAdminAsync(string action, CancellationToken ct = default)
    {
        if (Me.IsAdmin) return null;
        await SafeAudit(AuditEventType.AuthDenied, $"DENIED admin action: {action}", ct: ct);
        return Denied();
    }

    // Returns a 403 result if the user cannot access the project; otherwise null (proceed). Audits the denial.
    protected async Task<IActionResult?> RequireProjectAsync(int projectId, string action, CancellationToken ct = default)
    {
        if (Me.User is { } u && await Access.CanAccessProjectAsync(u, projectId, ct)) return null;
        await SafeAudit(AuditEventType.AuthDenied, $"DENIED project access: {action}", projectId: projectId, ct: ct);
        return Denied();
    }

    protected IActionResult Denied() { Response.StatusCode = 403; return View("~/Views/Shared/AccessDenied.cshtml"); }

    protected Task AuditAsync(AuditEventType type, string action, string? targetType = null, string? targetId = null,
        int? projectId = null, string? detail = null, CancellationToken ct = default)
        => SafeAudit(type, action, targetType, targetId, projectId, detail, ct);

    private async Task SafeAudit(AuditEventType type, string action, string? targetType = null, string? targetId = null,
        int? projectId = null, string? detail = null, CancellationToken ct = default)
    {
        try { await Audit.WriteAsync(Me.User, Me.IpAddress, type, action, targetType, targetId, projectId, detail, ct); }
        catch { /* auditing must never break the request */ }
    }
}
