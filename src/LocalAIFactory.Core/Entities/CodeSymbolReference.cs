using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-009: a deterministic structural reference captured during T-SQL extraction — the lean staging row that
// KE-010 resolves into a graph edge (DependsOn/References). It is NOT knowledge: no version/approval/Uid.
// Parse-once — KE-009 records what a symbol names (a FK target, a table read by a proc/view/trigger) by
// canonical name; KE-010 joins ReferencedKey to a target CodeSymbol.SourceLocusKey-equivalent and emits the
// edge. Fully rebuildable from raw: references for an artifact are deleted and re-inserted on re-extraction.
public class CodeSymbolReference
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }

    // The symbol that makes the reference (the view/proc/function/trigger/FK-bearing table).
    public int FromSymbolId { get; set; }

    // The artifact this reference was extracted from (KE-007). References are reconciled per artifact.
    public int SourceArtifactId { get; set; }

    public CodeReferenceKind ReferenceKind { get; set; }

    // Canonical referenced-object parts (lowercased, de-bracketed). Database is null when unqualified;
    // Schema defaults to "dbo" when the reference omits it. Column is set for FK/column references.
    public string? ReferencedDatabase { get; set; }
    public string ReferencedSchema { get; set; } = "dbo";
    public string ReferencedObject { get; set; } = "";
    public string? ReferencedColumn { get; set; }

    // Canonical "[db.]schema.object" join key (kind-agnostic — a name may resolve to a table or a view).
    // KE-010 matches this against the object-level identity of candidate target symbols.
    public string ReferencedKey { get; set; } = "";

    // Groups a file's references for provenance/debugging (mirrors CodeSymbol.FileLocusKey).
    public string FileLocusKey { get; set; } = "";

    // R2-ACC-CAP1 (C#↔SQL bridge): detection certainty for SQL-string references (null for deterministic
    // SQL/C# references whose confidence the resolver sets), and a short evidence snippet of the SQL string.
    public double? Confidence { get; set; }
    public string? Evidence { get; set; }

    public DateTime ExtractedUtc { get; set; } = DateTime.UtcNow;
}
