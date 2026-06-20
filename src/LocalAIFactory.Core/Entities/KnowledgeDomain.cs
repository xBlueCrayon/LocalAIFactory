namespace LocalAIFactory.Core.Entities;

// KE-005: a user-managed, editable, additive domain vocabulary (e.g. BDM, MCIB). Code is the STABLE,
// unique, human-meaningful key used to reconcile domains across instances and Knowledge Packs (E9);
// Uid is the portable surrogate identity. Starter domains are seeded; the taxonomy is owned by the user.
public class KnowledgeDomain : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();
    public string Code { get; set; } = "";   // unique, stable, uppercase — primary pack/sync reconciliation key.
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
