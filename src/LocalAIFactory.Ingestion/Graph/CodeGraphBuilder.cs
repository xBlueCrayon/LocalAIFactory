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
    // SQL object kinds a reference resolves to (matched by fully-qualified NormalizedKey).
    private static readonly CodeSymbolKind[] TargetKinds =
    {
        CodeSymbolKind.Table, CodeSymbolKind.View, CodeSymbolKind.StoredProcedure, CodeSymbolKind.SqlFunction
    };

    // C# type kinds a reference resolves to (matched by simple name, with disambiguation — KE-008.x).
    private static readonly CodeSymbolKind[] CSharpTypeKinds =
    {
        CodeSymbolKind.Class, CodeSymbolKind.Interface, CodeSymbolKind.Struct, CodeSymbolKind.Record,
        CodeSymbolKind.Enum, CodeSymbolKind.Delegate
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

        // C# simple-name resolution map: lower(simple name) -> { FullName -> (canonical id, locus) }. Partials
        // (same FullName across files) merge to one canonical (lowest id) — resolution-time merge, no identity
        // change. A simple name with multiple distinct FullNames is ambiguous (disambiguated by namespace).
        var csTypes = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && CSharpTypeKinds.Contains(s.Kind))
            .Select(s => new { s.Id, s.Name, s.FullName, s.SourceLocusKey })
            .ToListAsync(ct);
        var csByName = new Dictionary<string, Dictionary<string, (int Id, string Locus)>>(StringComparer.OrdinalIgnoreCase);
        foreach (var t in csTypes)
        {
            var key = t.Name.ToLowerInvariant();
            if (!csByName.TryGetValue(key, out var byFull)) csByName[key] = byFull = new(StringComparer.Ordinal);
            if (!byFull.TryGetValue(t.FullName, out var cur) || t.Id < cur.Id) byFull[t.FullName] = (t.Id, t.SourceLocusKey);
        }

        // References in scope (whole project, or one artifact for incremental).
        var refQuery = _db.CodeSymbolReferences.Where(r => r.ProjectId == projectId);
        if (artifactId is int aid) refQuery = refQuery.Where(r => r.SourceArtifactId == aid);
        var refs = await refQuery.ToListAsync(ct);

        // From-symbol details (locus + language) for dispatch.
        var fromIds = refs.Select(r => r.FromSymbolId).Distinct().ToList();
        var fromSyms = await _db.CodeSymbols.Where(s => fromIds.Contains(s.Id))
            .Select(s => new { s.Id, s.SourceLocusKey, s.DetectedLanguage }).ToListAsync(ct);
        var fromById = fromSyms.ToDictionary(x => x.Id, x => (x.SourceLocusKey, x.DetectedLanguage));

        // Existing edges in the same scope, keyed for convergence.
        var edgeQuery = _db.CodeEdges.Where(e => e.ProjectId == projectId);
        if (artifactId is int aid2) edgeQuery = edgeQuery.Where(e => e.SourceArtifactId == aid2);
        var existing = await edgeQuery.ToListAsync(ct);
        var byKey = existing.ToDictionary(e => e.EdgeKey);

        var seen = new HashSet<string>();
        int unresolved = 0;
        foreach (var r in refs)
        {
            if (!fromById.TryGetValue(r.FromSymbolId, out var from)) { unresolved++; continue; }

            // Resolve target + confidence, dispatched by the referencing symbol's language.
            (int Id, string Locus)? target;
            double confidence;
            if (string.Equals(from.DetectedLanguage, "csharp", StringComparison.OrdinalIgnoreCase))
            {
                if (r.ReferenceKind == CodeReferenceKind.SqlObjectAccess)
                {
                    // R2-ACC-CAP1: C#→SQL bridge — resolve the canonical "schema.object" key to a SQL symbol.
                    target = targetByKey.TryGetValue(r.ReferencedKey, out var sj) ? sj : null;
                    confidence = r.Confidence ?? 0.7;
                }
                else
                    (target, confidence) = ResolveCSharp(r, csByName);
            }
            else
            { target = targetByKey.TryGetValue(r.ReferencedKey, out var sq) ? sq : null; confidence = 1.0; }

            if (target is null) { unresolved++; continue; }
            if (target.Value.Id == r.FromSymbolId) continue; // self-reference: ignore

            var rel = MapRelation(r.ReferenceKind);
            var edgeKey = EdgeKey(projectId, from.SourceLocusKey, target.Value.Locus, rel);
            if (!seen.Add(edgeKey)) continue; // collapse duplicate references

            if (byKey.TryGetValue(edgeKey, out var edge))
            {
                edge.FromSymbolId = r.FromSymbolId;
                edge.ToSymbolId = target.Value.Id;
                edge.RelationType = rel;
                edge.SourceArtifactId = r.SourceArtifactId;
                edge.Confidence = confidence;
                edge.Evidence = r.Evidence; // R2-ACC-CAP1
            }
            else
            {
                _db.CodeEdges.Add(new CodeEdge
                {
                    ProjectId = projectId,
                    FromSymbolId = r.FromSymbolId,
                    ToSymbolId = target.Value.Id,
                    RelationType = rel,
                    SourceArtifactId = r.SourceArtifactId,
                    EdgeKey = edgeKey,
                    Confidence = confidence,
                    Evidence = r.Evidence, // R2-ACC-CAP1
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

    // KE-008.x: resolve a C# reference (simple name) to a type symbol, with confidence tiers. Unique name in
    // corpus -> high; ambiguous but disambiguated by the owner's namespace -> medium; otherwise unresolved.
    private static ((int Id, string Locus)? target, double confidence) ResolveCSharp(
        CodeSymbolReference r, Dictionary<string, Dictionary<string, (int Id, string Locus)>> csByName)
    {
        if (!csByName.TryGetValue(r.ReferencedKey, out var byFull) || byFull.Count == 0) return (null, 0);

        double baseConf = r.ReferenceKind is CodeReferenceKind.BaseType or CodeReferenceKind.InterfaceImplementation ? 0.95 : 0.9;
        if (byFull.Count == 1) return (byFull.Values.First(), baseConf); // unique simple name -> high confidence

        var nsHint = r.ReferencedSchema ?? "";
        var sameNs = byFull.Where(kv => NamespaceOf(kv.Key) == nsHint).Select(kv => kv.Value).ToList();
        if (sameNs.Count == 1) return (sameNs[0], 0.7);          // disambiguated by namespace -> medium
        return (null, 0);                                        // still ambiguous -> unresolved, never fabricated
    }

    private static string NamespaceOf(string fullName)
    {
        var i = fullName.LastIndexOf('.');
        return i < 0 ? "" : fullName.Substring(0, i).ToLowerInvariant();
    }

    private static RelationType MapRelation(CodeReferenceKind kind) => kind switch
    {
        CodeReferenceKind.ProcedureReference => RelationType.DependsOn, // EXEC of another procedure
        CodeReferenceKind.BaseType => RelationType.Inherits,
        CodeReferenceKind.InterfaceImplementation => RelationType.Implements,
        CodeReferenceKind.ConstructorParameterType => RelationType.DependsOn, // DI dependency
        CodeReferenceKind.SqlObjectAccess => RelationType.AccessesSql, // R2-ACC-CAP1: C#→SQL bridge
        _ => RelationType.References
    };

    // Convergence key from stable SourceLocusKeys (never DB ids) so edges survive re-import with their Uid.
    private static string EdgeKey(int? projectId, string fromLocus, string toLocus, RelationType rel)
    {
        var canonical = $"v1|proj:{projectId ?? 0}|from:{fromLocus}|to:{toLocus}|rel:{(int)rel}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonical)));
    }
}
