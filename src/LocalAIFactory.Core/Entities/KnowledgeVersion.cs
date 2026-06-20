using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-003: an immutable snapshot of a knowledge item at one point in its history. Once written, a
// version never changes; supersession appends a new version rather than mutating an old one.
public class KnowledgeVersion
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();   // portable identity of the version itself.

    public int KnowledgeItemId { get; set; }
    public KnowledgeItem? KnowledgeItem { get; set; }
    public Guid KnowledgeItemUid { get; set; }               // portable parent reference (pack-safe).

    public int VersionNumber { get; set; }
    public string ContentSnapshot { get; set; } = "";        // nvarchar(max) — never select in list views.
    public string ContentHash { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Summary { get; set; }
    public string ChangeReason { get; set; } = "";
    public ProvenanceMethod Method { get; set; } = ProvenanceMethod.Human;
    public string Actor { get; set; } = "";
    public PermanenceTier TierAtVersion { get; set; } = PermanenceTier.Derived;
    public KnowledgeStatus StatusAtVersion { get; set; } = KnowledgeStatus.Draft;
    public Guid? PreviousVersionUid { get; set; }            // lineage to the prior version.
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
