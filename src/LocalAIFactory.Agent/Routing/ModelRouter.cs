using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Agent.Routing;

public sealed class ModelRouter : IModelRouter
{
    private readonly IEnumerable<IChatModelProvider> _providers;
    private readonly AppDbContext _db;

    public ModelRouter(IEnumerable<IChatModelProvider> providers, AppDbContext db)
    {
        _providers = providers; _db = db;
    }

    public IChatModelProvider Resolve(ModelProvider provider)
    {
        var p = _providers.FirstOrDefault(x => x.Provider == provider);
        if (p is null) throw new InvalidOperationException($"No chat provider registered for {provider}.");
        return p;
    }

    public async Task<ModelConfiguration?> GetActiveAsync(int? modelConfigurationId, CancellationToken ct = default)
    {
        if (modelConfigurationId is int id)
        {
            var m = await _db.ModelConfigurations.FirstOrDefaultAsync(x => x.Id == id, ct);
            if (m is not null) return m;
        }
        return await _db.ModelConfigurations
            .Where(x => x.IsEnabled)
            .OrderByDescending(x => x.IsDefault)
            .FirstOrDefaultAsync(ct);
    }
}
