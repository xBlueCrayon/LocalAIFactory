using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-011: a retrieved symbol with its provenance — everything needed to cite the answer (Uid, defining
// artifact path, line span). Code and schema symbols share this shape.
public sealed record SymbolHit(
    int Id,
    Guid Uid,
    string FullName,
    string Name,
    CodeSymbolKind Kind,
    string? DetectedLanguage,
    int? ProjectId,
    string? Signature,
    int StartLine,
    int EndLine,
    int SourceArtifactId,
    string? ArtifactPath,
    string? ParentFullName);

// KE-011: one edge in an answer — the related symbol, the relationship, where the edge came from
// (containment vs reference), and its direction relative to the queried object.
public sealed record GraphNeighbor(
    SymbolHit Symbol,
    RelationType RelationType,
    string EdgeSource,   // "containment" | "reference"
    string Direction);   // "incoming" (depends on the query) | "outgoing" (the query depends on it)

// KE-011: a node in an impact/blast-radius result, with the hop distance and the edge that reached it.
public sealed record ImpactNode(SymbolHit Symbol, RelationType RelationType, int Depth, string ViaFullName);

// KE-011: the full blast radius of a change to a target, split into direct (1 hop) and transitive (>1).
public sealed record ImpactResult(
    SymbolHit Target,
    IReadOnlyList<ImpactNode> Direct,
    IReadOnlyList<ImpactNode> Transitive,
    int MaxDepthReached,
    bool Truncated);

// KE-011: deterministic, MSSQL-only structural retrieval over the KE-008/009/010 substrate (CodeSymbol +
// CodeEdge + ParentSymbolId containment). No vectors, no model required — both are optional accelerators.
// Every result is provenance-tagged for citation. Each call logs a capture-only RetrievalEvent.
public interface IStructuralRetrievalService
{
    // Exact-identifier lexical lookup (NormalizedKey); falls back to contains-match when no exact hit.
    Task<IReadOnlyList<SymbolHit>> FindByIdentifierAsync(int? projectId, string identifier, int max = 25, CancellationToken ct = default);

    // "What references / depends on X" — incoming reference edges (and, for a member like a column, the
    // dependents of its owning object too). The answer to "what breaks if X changes" at one hop.
    Task<IReadOnlyList<GraphNeighbor>> DependentsOfAsync(int? projectId, string identifier, CancellationToken ct = default);

    // "What does X reference / depend on" — outgoing reference edges plus containment children.
    Task<IReadOnlyList<GraphNeighbor>> DependenciesOfAsync(int? projectId, string identifier, CancellationToken ct = default);

    // Transitive blast radius of changing X — bounded BFS over incoming reference edges.
    Task<ImpactResult?> ImpactOfAsync(int? projectId, string identifier, int maxDepth = 4, int maxNodes = 250, CancellationToken ct = default);
}
