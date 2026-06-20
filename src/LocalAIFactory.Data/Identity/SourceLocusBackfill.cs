using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Identity;

// KE-004 one-time, idempotent backfill. Assigns the per-file source locus to pre-existing machine-
// extracted knowledge items that are linked to an imported file, so future re-extraction converges on
// them. Items with no file link (e.g. human-authored) keep a null locus and are never auto-converged.
// Bounded batches: only items still missing a locus are selected, so each pass makes progress.
public static class SourceLocusBackfill
{
    public static async Task RunAsync(AppDbContext db, CancellationToken ct = default)
    {
        const int batchSize = 1000;
        while (true)
        {
            var batch = await (from f in db.ImportedFiles
                               join k in db.KnowledgeItems on f.KnowledgeItemId equals k.Id
                               where k.SourceLocusKey == null && f.RelativePath != null
                               orderby k.Id
                               select new { k.Id, f.ProjectId, f.RelativePath })
                              .Take(batchSize)
                              .ToListAsync(ct);
            if (batch.Count == 0) break;

            // One artifact per item (the first seen); an item linked to several artifacts uses any path —
            // the locus is path-derived and all versions of a file share the same RelativePath.
            var byItem = batch.GroupBy(b => b.Id).ToDictionary(g => g.Key, g => g.First());
            var ids = byItem.Keys.ToList();
            var items = await db.KnowledgeItems.Where(k => ids.Contains(k.Id)).ToListAsync(ct);
            foreach (var item in items)
            {
                var r = byItem[item.Id];
                item.SourceLocusKey = SourceLocus.FileKey(r.ProjectId, r.RelativePath);
            }
            await db.SaveChangesAsync(ct);
        }
    }
}
