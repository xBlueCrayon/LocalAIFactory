namespace LocalAIFactory.Core.Abstractions;

// KE-012: outcome of a project-scoped consolidation pass — what converged and what was pruned. Surfaced for
// the convergence/validation report.
public sealed record ConsolidationResult(
    int Artifacts,                 // live artifacts re-extracted from raw
    int LiveSymbols,               // symbols present after consolidation
    int OrphanSymbolsRemoved,      // symbols no longer produced from any raw artifact (cross-file/deleted)
    int Edges,                     // reference edges present after consolidation
    int OrphanEdgesRemoved,        // edges touching a removed symbol
    int OrphanReferencesRemoved);  // staged references owned by a removed symbol

// KE-012: the thin, idempotent maintenance job that closes M2. It re-extracts the deterministic structural
// layer from raw (the immutable SourceArtifacts), reconciles symbols/references/edges in place, and prunes
// orphans — the cross-file/deleted-artifact deletion that per-artifact extraction (KE-008/009) deliberately
// defers. Rebuildable from raw; convergent (re-running changes nothing once stable); Uid-preserving so
// Knowledge Packs stay valid. Propose-never-overwrite: it touches ONLY the derived structural tables
// (CodeSymbol / CodeSymbolReference / CodeEdge) and never curated knowledge.
public interface IStructuralConsolidationService
{
    Task<ConsolidationResult> ConsolidateProjectAsync(int? projectId, CancellationToken ct = default);
}
