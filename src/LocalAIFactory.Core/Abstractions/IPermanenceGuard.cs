using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// Phase 2 / KE-002. The single chokepoint that enforces propose-never-overwrite: automated processes
// (extraction, consolidation, re-embedding) call this instead of mutating curated knowledge directly.
public interface IPermanenceGuard
{
    // True when an item is human-anchored (created, edited, or approved by a person) and therefore
    // must not be overwritten by any automated process.
    bool IsCurated(PermanenceTier tier);

    // Records an automated change to a curated item as a proposed revision routed to review, instead
    // of overwriting it. Returns the new ProposedRevision id.
    Task<int> ProposeRevisionAsync(
        string targetType, int targetId, int? originalKnowledgeItemId,
        string? proposedTitle, string proposedContent, string changeReason,
        RevisionSource source, CancellationToken ct = default);
}
