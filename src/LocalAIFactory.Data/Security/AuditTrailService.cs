using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Data.Security;

// R2-P0B: append-only security audit trail (AuditEvent). Records who/what/when/which-project, including denied
// access. Distinct from the Phase-1 AuditService/AuditLog. In the request path the caller wraps it so auditing
// can never break a query; failures are surfaced (logged), never silent.
public sealed class AuditTrailService : IAuditTrailService
{
    private readonly AppDbContext _db;
    public AuditTrailService(AppDbContext db) { _db = db; }

    public async Task WriteAsync(UserAccount? actor, string? ip, AuditEventType type, string action,
        string? targetType = null, string? targetId = null, int? projectId = null, string? detail = null,
        CancellationToken ct = default)
    {
        _db.AuditEvents.Add(new AuditEvent
        {
            UserAccountId = actor?.Id,
            WindowsIdentity = actor?.WindowsIdentity,
            EventType = type,
            Action = action.Length > 200 ? action[..200] : action,
            TargetType = targetType,
            TargetId = targetId is { Length: > 400 } ? targetId[..400] : targetId,
            ProjectId = projectId,
            Detail = detail is { Length: > 2000 } ? detail[..2000] : detail,
            IpAddress = ip
        });
        await _db.SaveChangesAsync(ct);
    }
}
