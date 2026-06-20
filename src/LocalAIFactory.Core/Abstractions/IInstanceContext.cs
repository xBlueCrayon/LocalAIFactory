namespace LocalAIFactory.Core.Abstractions;

// KE-003: the stable identity of THIS LocalAIFactory deployment, persisted once in SystemSettings.
// Stamped onto provenance so locally-produced knowledge is distinguishable from imported (pack) knowledge.
public interface IInstanceContext
{
    Task<Guid> GetInstanceIdAsync(CancellationToken ct = default);
}
