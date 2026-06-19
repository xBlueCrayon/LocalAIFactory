using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;

namespace LocalAIFactory.Data.Security;

public sealed class AuditService : IAuditService
{
    private readonly AppDbContext _db;
    public AuditService(AppDbContext db) => _db = db;

    public async Task LogAsync(string action, string? entityName = null, string? entityId = null, string? details = null, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(new AuditLog
        {
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            UserName = "local",
            CreatedUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }
}
