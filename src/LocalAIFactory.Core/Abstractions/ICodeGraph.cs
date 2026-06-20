namespace LocalAIFactory.Core.Abstractions;

// KE-010: outcome of a structural graph rebuild — edges materialized and references that could not be
// resolved to a symbol in the corpus (external/system objects, dynamic SQL, cross-project). Unresolved is
// surfaced for the validation report — never silently dropped.
public sealed record GraphRebuildResult(int Edges, int Unresolved);

// KE-010: builds the deterministic structural graph by resolving CodeSymbolReferences (KE-009) into CodeEdges.
// Convergent and rebuildable from raw: matched edges keep their Uid (Knowledge Pack stability), new edges
// insert, edges no longer produced are removed. Containment edges are NOT built here — they are derived from
// CodeSymbol.ParentSymbolId by the vCodeGraph view. Pure MSSQL/EF; no external services.
public interface ICodeGraphBuilder
{
    // Convergent full rebuild of a project's reference edges. Idempotent.
    Task<GraphRebuildResult> RebuildForProjectAsync(int? projectId, CancellationToken ct = default);

    // Incremental rebuild of just the edges sourced from one artifact's references. Resolution targets are
    // still project-wide, so a reference into a table defined in another file resolves correctly.
    Task<GraphRebuildResult> RebuildForArtifactAsync(int sourceArtifactId, CancellationToken ct = default);
}
