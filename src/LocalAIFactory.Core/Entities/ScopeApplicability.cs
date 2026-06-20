using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-005: an AppliesTo link — a constraint-scoped knowledge item (Standards/Regulatory) declares the
// project/component it governs. A constraint with NO links applies globally; with links it applies only
// to the linked targets. Loose references (like ProposedRevision/KnowledgeDuplicate), portable via Uids.
// (Populating these for typed standards/regulations is KE-023; KE-005 only builds the mechanism.)
public class ScopeApplicability
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();

    public int ConstraintKnowledgeItemId { get; set; }
    public Guid ConstraintUid { get; set; }

    public ScopeTargetKind TargetKind { get; set; } = ScopeTargetKind.Project;
    public int TargetId { get; set; }
    public Guid? TargetUid { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
