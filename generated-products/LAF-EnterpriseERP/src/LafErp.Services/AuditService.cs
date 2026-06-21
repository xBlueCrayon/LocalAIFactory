using LafErp.Core;
using LafErp.Data;

namespace LafErp.Services;

/// <summary>Append-only audit trail. Every state-changing domain action records one event.</summary>
public class AuditService
{
    private readonly ErpDbContext _db;
    private readonly ICurrentUser _user;

    public AuditService(ErpDbContext db, ICurrentUser user)
    {
        _db = db;
        _user = user;
    }

    public AuditEvent Record(string entityType, int entityId, string action, string? details = null)
    {
        var ev = new AuditEvent
        {
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PerformedBy = _user.Username,
            Details = details,
            EventUtc = DateTime.UtcNow,
            CreatedBy = _user.Username
        };
        _db.AuditEvents.Add(ev);
        return ev;
    }
}
