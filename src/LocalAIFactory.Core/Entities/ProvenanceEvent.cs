using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-003: append-only record of why/how a knowledge item came to be or changed. Distinct from AuditLog
// (which logs operational actions); provenance is the knowledge-lineage trail used for explainability,
// cross-instance reconciliation, and (future) Knowledge Pack origin tracking. Never mutated or deleted.
public class ProvenanceEvent
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();

    public int KnowledgeItemId { get; set; }
    public KnowledgeItem? KnowledgeItem { get; set; }
    public Guid KnowledgeItemUid { get; set; }

    public int? SourceArtifactId { get; set; }               // -> ImportedFile (formalized in KE-007).
    public ProvenanceMethod Method { get; set; } = ProvenanceMethod.Human;
    public string? ExtractorOrModelId { get; set; }
    public string Actor { get; set; } = "";
    public string Reason { get; set; } = "";

    // Portability / pack origin. OriginInstanceId stamps the deployment that produced the event;
    // OriginPackUid links an item that arrived from a future Knowledge Pack (E9). Both nullable.
    public Guid? OriginInstanceId { get; set; }
    public Guid? OriginPackUid { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
