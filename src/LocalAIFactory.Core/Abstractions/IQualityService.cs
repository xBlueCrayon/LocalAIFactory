using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-006: computes, persists, recomputes, and demotes the QualityBand. Pure EF/MSSQL. Strictly bounded —
// it does NOT influence retrieval ranking, queue ordering, approval ordering, or vector weighting.
public interface IQualityService
{
    // Recompute and persist the band for one item from current MSSQL signals. Returns the new band.
    Task<QualityBand> RecomputeAsync(int knowledgeItemId, CancellationToken ct = default);

    // Batch recompute (backfill / future KE-031 consolidation seam). Returns the number of items processed.
    Task<int> RecomputeAllAsync(int? projectId, CancellationToken ct = default);

    // Anti-drift demotion: a contradiction (KE-025) or failed outcome (KE-028) moves the item to
    // NeedsReview and floors its band. The signal sources call this once they exist.
    Task DemoteAsync(int knowledgeItemId, DemotionReason reason, CancellationToken ct = default);
}
