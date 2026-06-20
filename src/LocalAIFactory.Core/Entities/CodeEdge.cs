using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-010: a deterministic structural graph edge between two CodeSymbols (reference/dependency). A sibling of
// KnowledgeRelationship — NOT a child of it: KnowledgeRelationship endpoints are KnowledgeEntities (the
// heuristic/curated semantic layer), whereas CodeEdge endpoints are CodeSymbols (object-scoped, Uid-stable),
// auto-active and high-confidence. Containment (PartOf) is NOT stored here — it is derived from
// CodeSymbol.ParentSymbolId and exposed alongside these rows by the vCodeGraph view.
//
// Resolved from CodeSymbolReference (KE-009): the reference's owner becomes From, its resolved target becomes
// To. Convergence is keyed on EdgeKey (locus-based, not DB ids) so a rebuild/re-import keeps the same Uid —
// the basis of Knowledge Pack stability. Rebuildable from raw: edges for an artifact are reconciled in place.
public class CodeEdge : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // portable identity; stable across rebuilds.
    public int? ProjectId { get; set; }

    public int FromSymbolId { get; set; } // the referencing symbol (view/proc/function/trigger/FK-bearing table)
    public int ToSymbolId { get; set; }   // the resolved referenced symbol (table/view/proc/...)

    public RelationType RelationType { get; set; } = RelationType.References;
    public double Confidence { get; set; } = 1.0;                       // deterministic: high by construction
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.Approved; // structural edges are auto-active
    public PermanenceTier Tier { get; set; } = PermanenceTier.Derived;

    // The artifact whose reference produced this edge (KE-009 CodeSymbolReference.SourceArtifactId). Scopes
    // incremental rebuild: re-extracting an artifact reconciles only the edges it sources.
    public int SourceArtifactId { get; set; }

    // Convergence key = hash(projectId | fromLocus | toLocus | relationType). Built from stable
    // SourceLocusKeys (never ephemeral CodeSymbol.Id) so the edge survives re-import with its Uid.
    public string EdgeKey { get; set; } = "";

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

// KE-010: read-only projection of the unified structural graph (vCodeGraph view): containment edges derived
// from CodeSymbol.ParentSymbolId UNION reference edges from CodeEdge. KE-011 traverses this one shape so code
// and schema are a single graph. Keyless — backed by a SQL view, never a table.
public class CodeGraphEdge
{
    public int? ProjectId { get; set; }
    public int FromSymbolId { get; set; }
    public int ToSymbolId { get; set; }
    public RelationType RelationType { get; set; }
    public double Confidence { get; set; }
    public string EdgeSource { get; set; } = ""; // "containment" | "reference"
}
