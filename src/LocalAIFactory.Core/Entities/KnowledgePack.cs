using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// R2-ACC-B1: the database anchor for an installed Knowledge Pack — a portable, versioned bundle of baseline
// professional knowledge shipped with the application. The pack's items are normal KnowledgeItem rows stamped
// with KnowledgePackId (so they flow through existing search/approval/permanence/versioning), and this row is
// the install record that makes the pack identifiable, idempotent to re-install, and visible in the UI.
public class KnowledgePack : IPortableEntity
{
    public int Id { get; set; }

    // Portable identity. Set from the manifest's packUid so the same pack reconciles across instances (E9).
    public Guid Uid { get; set; } = Guid.CreateVersion7();

    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Description { get; set; } = "";
    public string License { get; set; } = "";

    public DateTime InstalledUtc { get; set; } = DateTime.UtcNow;
    public int ItemCount { get; set; }

    // SHA-256 over the pack's manifest + category files. Lets a re-install detect "nothing changed" instantly
    // and skip work — the fast idempotency path.
    public string SourceManifestHash { get; set; } = "";

    public KnowledgePackStatus Status { get; set; } = KnowledgePackStatus.Installed;
}
