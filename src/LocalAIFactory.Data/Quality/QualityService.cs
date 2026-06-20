using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Quality;

// KE-006: gathers MSSQL signals, computes the band via the evaluator, and persists it. Pure EF/MSSQL —
// works with no model and no vector store. Strictly bounded: compute / persist / recompute / demote only.
public sealed class QualityService : IQualityService
{
    private readonly AppDbContext _db;
    private readonly IQualityEvaluator _evaluator;

    public QualityService(AppDbContext db, IQualityEvaluator evaluator)
    {
        _db = db; _evaluator = evaluator;
    }

    public async Task<QualityBand> RecomputeAsync(int knowledgeItemId, CancellationToken ct = default)
    {
        var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == knowledgeItemId, ct);
        if (item is null) return QualityBand.Provisional;

        var band = _evaluator.ComputeBand(await BuildContextAsync(item, ct));
        if (item.QualityBand != band)
        {
            item.QualityBand = band; // note: do NOT bump UpdatedUtc — that would reset the currency clock.
            await _db.SaveChangesAsync(ct);
        }
        return band;
    }

    public async Task<int> RecomputeAllAsync(int? projectId, CancellationToken ct = default)
    {
        const int batchSize = 500;
        int lastId = 0, processed = 0;
        while (true)
        {
            var ids = await _db.KnowledgeItems
                .Where(k => (projectId == null || k.ProjectId == projectId) && k.Id > lastId)
                .OrderBy(k => k.Id).Select(k => k.Id).Take(batchSize).ToListAsync(ct);
            if (ids.Count == 0) break;
            foreach (var id in ids) { await RecomputeAsync(id, ct); processed++; }
            lastId = ids[^1];
            if (ids.Count < batchSize) break;
        }
        return processed;
    }

    public async Task DemoteAsync(int knowledgeItemId, DemotionReason reason, CancellationToken ct = default)
    {
        var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == knowledgeItemId, ct);
        if (item is null) return;
        // Anti-drift: a contradiction or failed outcome moves the item to review and floors its band.
        item.Status = KnowledgeStatus.NeedsReview;
        item.IsApproved = false;
        item.QualityBand = QualityBand.Provisional;
        item.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
    }

    private async Task<QualityContext> BuildContextAsync(KnowledgeItem item, CancellationToken ct)
    {
        // Primary corroboration signal: exact-content duplicate links involving this item (KE-004).
        var corroboration = await _db.KnowledgeDuplicates.CountAsync(
            d => (d.DuplicateOfKnowledgeItemId == item.Id || d.KnowledgeItemId == item.Id)
                 && d.MatchKind == DuplicateMatchKind.Exact, ct);

        // Secondary corroboration signal: distinct source artifacts in the provenance chain (KE-003).
        var distinctSources = await _db.ProvenanceEvents
            .Where(p => p.KnowledgeItemId == item.Id && p.SourceArtifactId != null)
            .Select(p => p.SourceArtifactId).Distinct().CountAsync(ct);

        var initialMethod = await _db.ProvenanceEvents
            .Where(p => p.KnowledgeItemId == item.Id).OrderBy(p => p.Id)
            .Select(p => p.Method).FirstOrDefaultAsync(ct);

        // Future signal sources (KE-025 Contradicts edges, KE-028 OutcomeEvents). When they land, replace
        // these with the real queries — the evaluator already consumes the flags, so no redesign is needed.
        const bool hasContradiction = false;
        const bool hasFailedOutcome = false;

        return new QualityContext(item.Tier, initialMethod, corroboration, distinctSources,
            item.Status, item.Scope, item.UpdatedUtc, DateTime.UtcNow, hasContradiction, hasFailedOutcome);
    }
}
