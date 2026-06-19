using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Rag.Embeddings;

public sealed class EmbeddingService : IEmbeddingService
{
    private readonly IEnumerable<IEmbeddingProvider> _providers;
    private readonly AppDbContext _db;
    private readonly IApiKeyProtector _protector;
    private readonly OllamaOptions _ollama;
    private readonly QdrantOptions _qdrant;
    private readonly RagOptions _rag;

    public EmbeddingService(
        IEnumerable<IEmbeddingProvider> providers, AppDbContext db, IApiKeyProtector protector,
        IOptions<OllamaOptions> ollama, IOptions<QdrantOptions> qdrant, IOptions<RagOptions> rag)
    {
        _providers = providers; _db = db; _protector = protector;
        _ollama = ollama.Value; _qdrant = qdrant.Value; _rag = rag.Value;
    }

    // Phase 1.2: embeddings are only usable when vector search is on, there is a vector store to hold
    // them (Qdrant enabled), and a provider path exists (Ollama enabled, or OpenAI selected). This keeps
    // the indexer from making pointless embed/Qdrant calls in Minimal Mode.
    public bool IsConfigured =>
        _rag.UseVectorSearch && _qdrant.Enabled
        && (string.Equals(_rag.EmbeddingProvider, "OpenAI", StringComparison.OrdinalIgnoreCase) || _ollama.Enabled);

    public int VectorSize => _qdrant.VectorSize;

    public async Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default)
    {
        if (!IsConfigured) return EmbeddingResult.Fail("Embeddings are disabled (Minimal Mode or vector search off).");

        var useOpenAi = string.Equals(_rag.EmbeddingProvider, "OpenAI", StringComparison.OrdinalIgnoreCase);
        if (useOpenAi)
        {
            var cfg = await _db.ModelConfigurations
                .Where(m => m.Provider == ModelProvider.OpenAi && m.IsEnabled)
                .OrderByDescending(m => m.IsDefault).FirstOrDefaultAsync(ct);
            if (cfg is not null)
            {
                var prov = _providers.FirstOrDefault(p => p.Provider == ModelProvider.OpenAi);
                if (prov is not null)
                {
                    var key = _protector.Unprotect(cfg.ApiKeyEncrypted);
                    var modelName = string.IsNullOrWhiteSpace(cfg.EmbeddingModel) ? "text-embedding-3-small" : cfg.EmbeddingModel!;
                    return await prov.EmbedAsync(cfg.BaseUrl, key, modelName, text, ct);
                }
            }
        }

        var ollama = _providers.FirstOrDefault(p => p.Provider == ModelProvider.Ollama);
        if (ollama is null) return EmbeddingResult.Fail("No embedding provider registered.");
        return await ollama.EmbedAsync(_ollama.BaseUrl, null, _ollama.EmbeddingModel, text, ct);
    }
}
