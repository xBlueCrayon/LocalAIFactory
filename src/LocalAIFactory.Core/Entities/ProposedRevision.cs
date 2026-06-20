using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// Phase 2 / KE-002. An automated change to a *curated* item is never applied in place; it is recorded
// here as a proposed revision and routed to review. The prioritized review queue UI is KE-014.
public class ProposedRevision
{
    public int Id { get; set; }

    // The curated entity this revision targets, e.g. "KnowledgeItem", "ProjectProfileSection".
    public string TargetEntityType { get; set; } = "";
    public int TargetEntityId { get; set; }

    // Convenience back-link for the common KnowledgeItem case (nullable for other target types).
    public int? OriginalKnowledgeItemId { get; set; }

    public string? ProposedTitle { get; set; }
    public string ProposedContent { get; set; } = "";  // nvarchar(max) — never select in list views.
    public string ChangeReason { get; set; } = "";
    public RevisionSource Source { get; set; } = RevisionSource.Extraction;

    // The revision's own lifecycle state; NeedsReview until a human accepts or rejects it (KE-013/KE-014).
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.NeedsReview;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
