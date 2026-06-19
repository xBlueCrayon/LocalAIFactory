using System.Globalization;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Rag.Indexing;

// Embeds a knowledge item's chunks and upserts them to the vector store. Safe no-op when embeddings
// or the vector store are unavailable (search will fall back to keyword).
public sealed class KnowledgeIndexer : IKnowledgeIndexer
{
    private readonly AppDbContext _db;
    private readonly IEmbeddingService _embedding;
    private readonly IVectorStore _vectorStore;

    public KnowledgeIndexer(AppDbContext db, IEmbeddingService embedding, IVectorStore vectorStore)
    {
        _db = db; _embedding = embedding; _vectorStore = vectorStore;
    }

    public async Task IndexKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default)
    {
        var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == knowledgeItemId, ct);
        if (item is null) return;

        // Phase 1.2: skip all vector/embedding work when embeddings are disabled (Minimal Mode).
        if (!_embedding.IsConfigured) return;
        if (!await SafeHealthy(ct)) return;
        await SafeEnsure(ct);

        var chunks = await _db.KnowledgeChunks
            .Where(c => c.KnowledgeItemId == knowledgeItemId)
            .OrderBy(c => c.ChunkIndex)
            .ToListAsync(ct);

        foreach (var chunk in chunks)
        {
            var emb = await _embedding.EmbedAsync(chunk.Content, ct);
            if (!emb.Success || emb.Vector is not { Length: > 0 }) continue;

            var pointId = DeterministicGuid(knowledgeItemId, chunk.ChunkIndex).ToString();
            var payload = new Dictionary<string, object>
            {
                ["knowledgeItemId"] = item.Id,
                ["projectId"] = item.ProjectId ?? 0,
                ["isApproved"] = item.IsApproved,
                ["title"] = item.Title,
                ["content"] = chunk.Content
            };
            try
            {
                await _vectorStore.UpsertAsync(pointId, emb.Vector, payload, ct);
                chunk.VectorId = pointId;
                chunk.EmbeddingModel = "configured";
            }
            catch { /* leave VectorId null; keyword search still works */ }
        }
        await _db.SaveChangesAsync(ct);
    }

    public async Task RemoveKnowledgeItemAsync(int knowledgeItemId, CancellationToken ct = default)
    {
        var chunks = await _db.KnowledgeChunks
            .Where(c => c.KnowledgeItemId == knowledgeItemId && c.VectorId != null)
            .ToListAsync(ct);
        var ids = chunks.Where(c => !string.IsNullOrEmpty(c.VectorId)).Select(c => c.VectorId!).ToList();
        if (ids.Count > 0)
        {
            try { await _vectorStore.DeleteAsync(ids, ct); } catch { /* keyword search unaffected */ }
            foreach (var c in chunks) c.VectorId = null;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<bool> SafeHealthy(CancellationToken ct)
    {
        try { return await _vectorStore.HealthAsync(ct); } catch { return false; }
    }

    private async Task SafeEnsure(CancellationToken ct)
    {
        try { await _vectorStore.EnsureCollectionAsync(_embedding.VectorSize, ct); } catch { }
    }

    private static Guid DeterministicGuid(int knowledgeItemId, int chunkIndex)
    {
        var key = string.Create(CultureInfo.InvariantCulture, $"ki-{knowledgeItemId}-{chunkIndex}");
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }
}
