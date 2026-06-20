using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Ingestion.Graph;

// KE-010: resolves deterministic references (KE-009 CodeSymbolReference) into structural CodeEdges. The
// referenced object's canonical key (ReferencedKey) is matched against CodeSymbol.NormalizedKey for object-
// level target kinds (table/view/proc/function). Resolved -> an edge; unresolved -> counted, never fabricated.
// Convergence is keyed on EdgeKey (built from stable SourceLocusKeys, not DB ids) so edges keep their Uid
// across rebuild and re-import. Pure MSSQL/EF — no external services, no second parse.
public sealed class CodeGraphBuilder : ICodeGraphBuilder
{
    // Object-level kinds a reference can resolve to (a FROM/JOIN/FK/EXEC names one of these).
    private static readonly CodeSymbolKind[] TargetKinds =
    {
        CodeSymbolKind.Table, CodeSymbolKind.View, CodeSymbolKind.StoredProcedure, CodeSymbolKind.SqlFunction
    };

    private readonly AppDbContext _db;

    public CodeGraphBuilder(AppDbContext db) { _db = db; }

    public Task<GraphRebuildResult> RebuildForProjectAsync(int? projectId, CancellationToken ct = default)
        => RebuildAsync(projectId, artifactId: null, ct);

    public async Task<GraphRebuildResult> RebuildForArtifactAsync(int sourceArtifactId, CancellationToken ct = default)
    {
        var art = await _db.ImportedFiles.FirstOrDefaultAsync(f => f.Id == sourceArtifactId, ct);
        if (art is null) return new GraphRebuildResult(0, 0);
        return await RebuildAsync(art.ProjectId, sourceArtifactId, ct);
    }

    private async Task<GraphRebuildResult> RebuildAsync(int? projectId, int? artifactId, CancellationToken ct)
    {
        // Resolution map: canonical object key -> (symbol id, locus). Project-wide so cross-file targets bind.
        var targets = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && TargetKinds.Contains(s.Kind))
            .Select(s => new { s.NormalizedKey, s.Id, s.SourceLocusKey })
            .ToListAsync(ct);
        var targetByKey = new Dictionary<string, (int Id, string Locus)>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in targets)
            targetByKey.TryAdd(t.NormalizedKey, (t.Id, t.SourceLocusKey)); // first wins on rare FullName collision

        // References in scope (whole project, or one artifact for incremental).
        var refQuery = _db.CodeSymbolReferences.Where(r => r.ProjectId == projectId);
        if (artifactId is int aid) refQuery = refQuery.Where(r => r.SourceArtifactId == aid);
        var refs = await refQuery.ToListAsync(ct);

        // Locus of each referencing (From) symbol.
        var fromIds = refs.Select(r => r.FromSymbolId).Distinct().ToList();
        var fromLocus = await _db.CodeSymbols.Where(s => fromIds.Contains(s.Id))
            .Select(s => new { s.Id, s.SourceLocusKey }).ToListAsync(ct);
        var locusById = fromLocus.ToDictionary(x => x.Id, x => x.SourceLocusKey);

        // Existing edges in the same scope, keyed for convergence.
        var edgeQuery = _db.CodeEdges.Where(e => e.ProjectId == projectId);
        if (artifactId is int aid2) edgeQuery = edgeQuery.Where(e => e.SourceArtifactId == aid2);
        var existing = await edgeQuery.ToListAsync(ct);
        var byKey = existing.ToDictionary(e => e.EdgeKey);

        var seen = new HashSet<string>();
        int unresolved = 0;
        foreach (var r in refs)
        {
            if (!targetByKey.TryGetValue(r.ReferencedKey, out var target)) { unresolved++; continue; }
            if (!locusById.TryGetValue(r.FromSymbolId, out var fromLoc)) { unresolved++; continue; }
            if (target.Id == r.FromSymbolId) continue; // self-reference: ignore

            var rel = MapRelation(r.ReferenceKind);
            var edgeKey = EdgeKey(projectId, fromLoc, target.Locus, rel);
            if (!seen.Add(edgeKey)) continue; // collapse duplicate references (e.g. a table read twice)

            if (byKey.TryGetValue(edgeKey, out var edge))
            {
                edge.FromSymbolId = r.FromSymbolId;
                edge.ToSymbolId = target.Id;
                edge.RelationType = rel;
                edge.SourceArtifactId = r.SourceArtifactId;
                edge.Confidence = 1.0;
            }
            else
            {
                _db.CodeEdges.Add(new CodeEdge
                {
                    ProjectId = projectId,
                    FromSymbolId = r.FromSymbolId,
                    ToSymbolId = target.Id,
                    RelationType = rel,
                    SourceArtifactId = r.SourceArtifactId,
                    EdgeKey = edgeKey,
                    Confidence = 1.0,
                    Status = KnowledgeStatus.Approved,
                    Tier = PermanenceTier.Derived
                });
            }
        }

        // Remove edges no longer produced in this scope.
        foreach (var e in existing)
            if (!seen.Contains(e.EdgeKey)) _db.CodeEdges.Remove(e);

        await _db.SaveChangesAsync(ct);
        return new GraphRebuildResult(seen.Count, unresolved);
    }

    private static RelationType MapRelation(CodeReferenceKind kind) => kind switch
    {
        CodeReferenceKind.ProcedureReference => RelationType.DependsOn, // EXEC of another procedure
        _ => RelationType.References
    };

    // Convergence key from stable SourceLocusKeys (never DB ids) so edges survive re-import with their Uid.
    private static string EdgeKey(int? projectId, string fromLocus, string toLocus, RelationType rel)
    {
        var canonical = $"v1|proj:{projectId ?? 0}|from:{fromLocus}|to:{toLocus}|rel:{(int)rel}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
