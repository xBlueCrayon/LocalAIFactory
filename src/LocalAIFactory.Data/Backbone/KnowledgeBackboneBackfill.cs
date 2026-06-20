using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Backbone;

// KE-003 one-time, idempotent backfill. Gives every pre-existing knowledge item a content hash, a v1
// version snapshot, and an initial provenance event. (Uid backfill for all tables is done in the
// migration via NEWID, before the unique indexes are created.) Safe to run on every startup: it only
// processes items that have not been stamped yet (ContentHash == ""), in bounded batches.
public static class KnowledgeBackboneBackfill
{
    public static async Task RunAsync(AppDbContext db, IContentHasher hasher, IInstanceContext instance,
        CancellationToken ct = default)
    {
        var instanceId = await instance.GetInstanceIdAsync(ct); // also creates the InstanceId if missing.
        const int batchSize = 500;

        while (true)
        {
            var items = await db.KnowledgeItems
                .Where(k => k.ContentHash == "")
                .OrderBy(k => k.Id)
                .Take(batchSize)
                .ToListAsync(ct);
            if (items.Count == 0) break;

            foreach (var item in items)
            {
                item.ContentHash = hasher.Compute(item.Content);
                if (item.VersionNumber < 1) item.VersionNumber = 1;
                var method = item.Tier == PermanenceTier.Curated ? ProvenanceMethod.Human : ProvenanceMethod.Deterministic;

                db.KnowledgeVersions.Add(new KnowledgeVersion
                {
                    KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, VersionNumber = 1,
                    ContentSnapshot = item.Content, ContentHash = item.ContentHash, Title = item.Title,
                    Summary = item.Summary, ChangeReason = "Backfill: initial version", Method = method,
                    Actor = "backfill", TierAtVersion = item.Tier, StatusAtVersion = item.Status
                });
                db.ProvenanceEvents.Add(new ProvenanceEvent
                {
                    KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, Method = method, Actor = "backfill",
                    Reason = "Backfill: initial provenance", OriginInstanceId = instanceId
                });
            }

            await db.SaveChangesAsync(ct);
            if (items.Count < batchSize) break;
        }
    }
}
