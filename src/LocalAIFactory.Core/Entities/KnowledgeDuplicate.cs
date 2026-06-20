using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-004: a detected duplicate relationship between two knowledge items (capture only; auto-merge is
// KE-030). Dedicated table — deliberately not overloaded onto KnowledgeRelationship (which is graph-
// node-to-node). References both local Ids (FK/joins) and portable Uids (cross-instance / pack-safe).
public class KnowledgeDuplicate : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();

    // The (later) item that is a duplicate of the canonical item.
    public int KnowledgeItemId { get; set; }
    public Guid KnowledgeItemUid { get; set; }

    // The canonical item it duplicates.
    public int DuplicateOfKnowledgeItemId { get; set; }
    public Guid DuplicateOfUid { get; set; }

    public DuplicateMatchKind MatchKind { get; set; } = DuplicateMatchKind.Exact;
    public DuplicateStatus Status { get; set; } = DuplicateStatus.Candidate;
    public double Confidence { get; set; } = 1.0;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedUtc { get; set; }
}
