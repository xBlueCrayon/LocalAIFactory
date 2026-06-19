using LocalAIFactory.Core.Dtos;

namespace LocalAIFactory.Core.Abstractions;

public interface IChunkingService
{
    IReadOnlyList<string> Chunk(string text, int maxChars, int overlap);
    int EstimateTokens(string text);
}

public interface IVectorStore
{
    Task<bool> HealthAsync(CancellationToken ct = default);
    Task EnsureCollectionAsync(int vectorSize, CancellationToken ct = default);
    Task UpsertAsync(string id, float[] vector, IDictionary<string, object> payload, CancellationToken ct = default);
    Task<IReadOnlyList<VectorSearchHit>> SearchAsync(float[] vector, int topK, int? projectId, CancellationToken ct = default);
    // Phase 1.1: remove points by id (used when knowledge is deprecated or deleted). Quiet on failure.
    Task DeleteAsync(IEnumerable<string> ids, CancellationToken ct = default);
}

public interface IKnowledgeSearchService
{
    Task<List<RagContextItem>> SearchAsync(int? projectId, string query, int topK, CancellationToken ct = default);
}

public interface IRagContextBuilder
{
    Task<RetrievedContext> BuildAsync(
        int? projectId,
        string query,
        bool useProjectMemory,
        bool useKnowledgeBase,
        bool useKnowledgeGraph,
        CancellationToken ct = default);
}

// Indexes a knowledge item's chunks into the vector store (used after import and on approval).
public interface IKnowledgeIndexer
{
    Task IndexKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default);
    // Phase 1.1: delete a knowledge item's vectors from the store and clear chunk vector ids.
    Task RemoveKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default);
}
