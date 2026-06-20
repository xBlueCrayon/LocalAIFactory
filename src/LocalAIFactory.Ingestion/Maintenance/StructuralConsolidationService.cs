using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Ingestion.Maintenance;

// KE-012: project-scoped consolidation of the deterministic structural layer. Idempotent and rebuildable
// from raw. Strategy: stamp a watermark, re-extract every live artifact from raw (per-artifact convergent
// upsert via KE-008/009 — matched symbols keep their Uid, removed-within-file symbols are reconciled), then
// PRUNE any symbol not re-touched this pass — that is the cross-file / deleted-artifact orphan that
// per-artifact extraction cannot see. Finally rebuild the reference graph convergently (KE-010). Touches
// only CodeSymbol / CodeSymbolReference / CodeEdge; curated knowledge is never read or written.
public sealed class StructuralConsolidationService : IStructuralConsolidationService
{
    private readonly AppDbContext _db;
    private readonly ICodeSymbolStore _symbols;
    private readonly ISchemaSymbolStore _schema;
    private readonly ICodeGraphBuilder _graph;

    public StructuralConsolidationService(
        AppDbContext db, ICodeSymbolStore symbols, ISchemaSymbolStore schema, ICodeGraphBuilder graph)
    {
        _db = db; _symbols = symbols; _schema = schema; _graph = graph;
    }

    // Unambiguous "not produced this pass" marker — independent of wall-clock resolution (Windows UtcNow is
    // coarse, so a timestamp watermark could miss symbols re-touched within the same tick). Any live re-
    // extraction overwrites this with a real UtcNow; whatever is still at the sentinel afterward is an orphan.
    private static readonly DateTime Sentinel = DateTime.MinValue;

    public async Task<ConsolidationResult> ConsolidateProjectAsync(int? projectId, CancellationToken ct = default)
    {
        // 0. Reset the produced-marker on every project symbol (portable load-and-set; works on any provider).
        //    A crashed run leaves sentinels that the next successful run overwrites — ExtractedUtc is
        //    informational, not load-bearing, so this is safe and re-runnable.
        var existingForReset = await _db.CodeSymbols.Where(s => s.ProjectId == projectId).ToListAsync(ct);
        foreach (var s in existingForReset) s.ExtractedUtc = Sentinel;
        if (existingForReset.Count > 0) await _db.SaveChangesAsync(ct);

        // 1. Re-extract every live artifact from raw (rebuildable-from-raw). Per-artifact failures are isolated.
        var artifacts = await _db.ImportedFiles
            .Where(f => f.ProjectId == projectId && !f.Skipped && f.RawText != null
                        && (f.DetectedLanguage == "csharp" || f.DetectedLanguage == "sql"))
            .Select(f => new { f.Id, f.DetectedLanguage })
            .ToListAsync(ct);

        foreach (var a in artifacts)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                if (a.DetectedLanguage == "csharp") await _symbols.UpsertForArtifactAsync(a.Id, ct);
                else await _schema.UpsertForArtifactAsync(a.Id, ct);
            }
            catch { /* one bad artifact must not abort consolidation */ }
        }

        // 2. Identify orphans: symbols still at the sentinel were not produced from any live artifact this
        //    pass (cross-file removal, deleted/superseded artifact, or object dropped from an edited file).
        var orphanIds = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && s.ExtractedUtc == Sentinel)
            .Select(s => s.Id).ToListAsync(ct);

        int orphanEdges = 0, orphanRefs = 0;
        if (orphanIds.Count > 0)
        {
            // Order matters (Restrict FKs): edges and references first, then re-parent, then the symbols.
            var deadEdges = await _db.CodeEdges
                .Where(e => orphanIds.Contains(e.FromSymbolId) || orphanIds.Contains(e.ToSymbolId)).ToListAsync(ct);
            orphanEdges = deadEdges.Count;
            _db.CodeEdges.RemoveRange(deadEdges);

            var deadRefs = await _db.CodeSymbolReferences
                .Where(r => orphanIds.Contains(r.FromSymbolId)).ToListAsync(ct);
            orphanRefs = deadRefs.Count;
            _db.CodeSymbolReferences.RemoveRange(deadRefs);

            // Defensive: detach any live child whose parent is being removed (orphan subtrees are normally
            // self-contained, but never leave a dangling self-FK).
            var reparent = await _db.CodeSymbols
                .Where(s => s.ProjectId == projectId && s.ParentSymbolId != null && orphanIds.Contains(s.ParentSymbolId!.Value)
                            && s.ExtractedUtc != Sentinel)
                .ToListAsync(ct);
            foreach (var s in reparent) s.ParentSymbolId = null;
            await _db.SaveChangesAsync(ct);

            var orphans = await _db.CodeSymbols.Where(s => orphanIds.Contains(s.Id)).ToListAsync(ct);
            _db.CodeSymbols.RemoveRange(orphans);
            await _db.SaveChangesAsync(ct);
        }

        // 3. Convergent reference-graph rebuild over the surviving symbols (KE-010). Removes any edge whose
        //    target vanished; preserves Uids of edges that persist.
        var graph = await _graph.RebuildForProjectAsync(projectId, ct);

        var liveSymbols = await _db.CodeSymbols.CountAsync(s => s.ProjectId == projectId, ct);
        return new ConsolidationResult(
            artifacts.Count, liveSymbols, orphanIds.Count, graph.Edges, orphanEdges, orphanRefs);
    }
}
