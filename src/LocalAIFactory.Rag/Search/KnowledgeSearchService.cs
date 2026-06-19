using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Options;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Rag.Search;

// Vector-first search with automatic keyword fallback. Never throws to callers.
public sealed class KnowledgeSearchService : IKnowledgeSearchService
{
    private readonly IEmbeddingService _embedding;
    private readonly IVectorStore _vectorStore;
    private readonly KeywordSearchService _keyword;
    private readonly RagOptions _rag;

    public KnowledgeSearchService(
        IEmbeddingService embedding, IVectorStore vectorStore,
        KeywordSearchService keyword, IOptions<RagOptions> rag)
    {
        _embedding = embedding; _vectorStore = vectorStore; _keyword = keyword; _rag = rag.Value;
    }

    public async Task<List<RagContextItem>> SearchAsync(int? projectId, string query, int topK, CancellationToken ct = default)
    {
        if (_rag.UseVectorSearch)
        {
            try
            {
                if (await _vectorStore.HealthAsync(ct))
                {
                    var emb = await _embedding.EmbedAsync(query, ct);
                    if (emb.Success && emb.Vector is { Length: > 0 })
                    {
                        await _vectorStore.EnsureCollectionAsync(_embedding.VectorSize, ct);
                        var hits = await _vectorStore.SearchAsync(emb.Vector, topK, projectId, ct);
                        var items = hits.Select(Map).Where(i => !string.IsNullOrWhiteSpace(i.Content)).ToList();
                        if (items.Count > 0) return items;
                    }
                }
            }
            catch
            {
                // ignore and fall back to keyword search
            }
        }

        return await _keyword.SearchAsync(projectId, query, topK, ct);
    }

    private static RagContextItem Map(VectorSearchHit hit)
    {
        string title = hit.Payload.TryGetValue("title", out var t) ? t?.ToString() ?? "" : "";
        string content = hit.Payload.TryGetValue("content", out var c) ? c?.ToString() ?? "" : "";
        bool approved = hit.Payload.TryGetValue("isApproved", out var a) && a is bool b && b;
        return new RagContextItem
        {
            Kind = "Knowledge",
            Source = "vector",
            Title = title,
            Content = content,
            IsApproved = approved,
            Score = hit.Score
        };
    }
}
