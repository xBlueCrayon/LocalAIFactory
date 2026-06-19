using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Rag.Health;

// Singleton holder for the latest service-health snapshot.
// The initial value is derived purely from configuration (no I/O), so the dashboard shows
// "Disabled" immediately for switched-off services even before the first background probe runs.
public sealed class ServiceHealthCache : IServiceHealthCache
{
    private volatile ServiceHealthSnapshot _current;

    public ServiceHealthCache(IOptions<QdrantOptions> qdrant, IOptions<OllamaOptions> ollama, IOptions<RagOptions> rag)
    {
        var q = qdrant.Value;
        var o = ollama.Value;
        var r = rag.Value;

        bool qdrantUsable = q.Enabled && r.UseVectorSearch;
        bool embeddingsPossible = qdrantUsable
            && (string.Equals(r.EmbeddingProvider, "OpenAI", StringComparison.OrdinalIgnoreCase) || o.Enabled);

        EnvironmentMode mode = !o.Enabled
            ? EnvironmentMode.Minimal
            : (qdrantUsable ? EnvironmentMode.FullAi : EnvironmentMode.Standard);

        _current = new ServiceHealthSnapshot
        {
            Qdrant = qdrantUsable ? ServiceState.Unknown : ServiceState.Disabled,
            Ollama = o.Enabled ? ServiceState.Unknown : ServiceState.Disabled,
            Embeddings = embeddingsPossible ? ServiceState.Unknown : ServiceState.Disabled,
            Mode = mode,
            ChatAvailable = true, // optimistic until the first probe; corrected within ~1s
            LastCheckedUtc = null
        };
    }

    public ServiceHealthSnapshot Current => _current;

    public void Set(ServiceHealthSnapshot snapshot) => _current = snapshot;
}
