using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-008: a single symbol produced by a language extractor, before it is reconciled to a CodeSymbol row.
// ParentFullName is the FullName of the enclosing namespace/type; the store maps it to ParentSymbolId.
public sealed record ExtractedSymbol(
    CodeSymbolKind Kind,
    string Name,
    string FullName,
    string? Signature,
    CodeAccess Access,
    bool IsPublic,
    int StartOffset,
    int EndOffset,
    int StartLine,
    int EndLine,
    int ComplexitySignal,
    string? ParentFullName);

// KE-008.x: a deterministic, syntax-only type reference one symbol makes to another, captured by SIMPLE name
// (C# cannot produce a fully-qualified name without a semantic model). FromFullName is the owning symbol's
// display FullName; ReferencedName is the simple type name (generic arity/args stripped); NamespaceHint is the
// owner's enclosing namespace, used by the resolver to disambiguate same-named types. Resolved to a CodeSymbol
// by KE-010's language-dispatched resolver; unresolved references are counted, never fabricated.
public sealed record ExtractedCodeReference(
    CodeReferenceKind Kind,
    string FromFullName,
    string ReferencedName,
    string? NamespaceHint,
    // R2-ACC-CAP1: bridge metadata. Confidence is the detection certainty for SQL-string references (null =
    // use the resolver default). Evidence is a short snippet of the SQL string that produced the reference.
    double? Confidence = null,
    string? Evidence = null);

// KE-008: the result of extracting one source artifact — symbols plus the references they make. References
// are empty for extractors that do not yet capture them.
public sealed record CodeExtractionResult(
    IReadOnlyList<ExtractedSymbol> Symbols,
    IReadOnlyList<ExtractedCodeReference> References)
{
    public static readonly CodeExtractionResult Empty =
        new(Array.Empty<ExtractedSymbol>(), Array.Empty<ExtractedCodeReference>());
}

// KE-008: a per-language, deterministic, syntax-only symbol extractor. Pluggable by design — VB.NET,
// Razor, etc. implement the same contract and register without any pipeline redesign. Implementations
// MUST be pure (same input -> same output) and MUST NOT resolve dependencies or build a semantic model.
// References (KE-008.x) are by simple name; resolution to symbols is KE-010's concern.
public interface ICodeSymbolExtractor
{
    // The DetectedLanguage value this extractor handles (e.g. "csharp").
    string Language { get; }
    CodeExtractionResult Extract(string content);
}

// KE-008: routes content to the extractor registered for a DetectedLanguage. Unknown/unsupported
// languages return false / Empty — never throw.
public interface ICodeSymbolExtractorRouter
{
    bool CanExtract(string? detectedLanguage);
    CodeExtractionResult Extract(string? detectedLanguage, string content);
}

// KE-008: persists/reconciles symbols for one artifact. Convergent upsert keyed on SourceLocusKey —
// matched symbols keep their Uid, new ones are inserted, removed ones are deleted; parent links are
// resolved after. Incremental at the caller (only changed artifacts are passed).
public interface ICodeSymbolStore
{
    // Extracts and reconciles the symbols of the given source artifact. Returns the number of symbols
    // now stored for that artifact's file. A no-op (returns 0) when the artifact is skipped, has no
    // text, or its language has no registered extractor.
    Task<int> UpsertForArtifactAsync(int sourceArtifactId, CancellationToken ct = default);
}
