using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Backbone;

// KE-003: reads (or creates once) this deployment's stable InstanceId from SystemSettings. The value
// is process-cached after first read; it never changes for the life of a database.
public sealed class InstanceContext : IInstanceContext
{
    public const string SettingKey = "InstanceId";
    private static Guid _cached;
    private readonly AppDbContext _db;

    public InstanceContext(AppDbContext db) => _db = db;

    public async Task<Guid> GetInstanceIdAsync(CancellationToken ct = default)
    {
        if (_cached != Guid.Empty) return _cached;

        var setting = await _db.SystemSettings.FirstOrDefaultAsync(s => s.Key == SettingKey, ct);
        if (setting is null)
        {
            var id = Guid.CreateVersion7();
            _db.SystemSettings.Add(new SystemSetting { Key = SettingKey, Value = id.ToString() });
            await _db.SaveChangesAsync(ct);
            return _cached = id;
        }
        return _cached = Guid.TryParse(setting.Value, out var parsed) ? parsed : Guid.Empty;
    }
}
