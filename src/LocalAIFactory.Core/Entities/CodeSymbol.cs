using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-008: a deterministic, structural code symbol (namespace/type/member) extracted from a source
// artifact by syntax-only parsing. Lean by design — it carries identity, location and syntactic
// attributes, but NO version/provenance/approval chain. Symbols are regenerable: re-extraction
// converges on the stable SourceLocusKey and keeps the same Uid, so graph edges and pack references
// (KE-010/KE-011) survive re-import. Everything traces back to its SourceArtifact via SourceArtifactId.
public class CodeSymbol : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // portable identity, stable across re-extraction.
    public int? ProjectId { get; set; }

    // The artifact this symbol was last extracted from (KE-007). Updated on convergent re-extraction.
    public int SourceArtifactId { get; set; }

    // KE-004 file-level locus — groups every symbol of one logical file so re-extraction can reconcile
    // the whole file's symbol set (insert new, keep matched, delete removed) in one pass.
    public string FileLocusKey { get; set; } = "";

    // KE-008 per-symbol convergence key (hashed canonical "...|type:symbol|sym:{FullName}|sig:{hash}").
    // Stable identity of the symbol within its file; the join target for graph/pack references.
    public string SourceLocusKey { get; set; } = "";

    // Containment: member -> type -> namespace. Null for top-level namespaces.
    public int? ParentSymbolId { get; set; }

    public CodeSymbolKind Kind { get; set; }
    public string Name { get; set; } = "";
    public string FullName { get; set; } = "";   // namespace-qualified (e.g. "Acme.Banking.Account.Deposit").
    public string? Signature { get; set; }        // parameter/return shape; disambiguates overloads.
    public CodeAccess Access { get; set; } = CodeAccess.Internal;
    public bool IsPublic { get; set; }

    public int StartOffset { get; set; }
    public int EndOffset { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }

    // Syntactic cyclomatic complexity (decision-point count). Deterministic; no semantic model. 0 for
    // symbols without a body (namespaces, types, fields, events, abstract members).
    public int ComplexitySignal { get; set; }

    public string? DetectedLanguage { get; set; } // e.g. "csharp"; the extractor that produced this symbol.
    public string SymbolHash { get; set; } = "";  // change-detection digest (location + signature + attributes).
    public DateTime ExtractedUtc { get; set; } = DateTime.UtcNow;
}
