using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Rag.Retrieval;

// KE-011: deterministic structural retrieval over the KE-008/009/010 substrate. Queries CodeSymbols +
// CodeEdges + ParentSymbolId containment directly (not the vCodeGraph view) so it is provider-agnostic and
// unit-testable on the in-memory provider — and so the request path never depends on a SQL view. Runs on
// MSSQL alone: no vectors, no model. Every hit carries provenance (Uid + artifact path + line span) for
// citation. Each query writes a capture-only RetrievalEvent (best-effort).
public sealed class StructuralRetrievalService : IStructuralRetrievalService
{
    private readonly AppDbContext _db;

    public StructuralRetrievalService(AppDbContext db) { _db = db; }

    private static bool IsMember(CodeSymbolKind k) => k is
        CodeSymbolKind.Column or CodeSymbolKind.Constraint or CodeSymbolKind.ForeignKey or CodeSymbolKind.Index
        or CodeSymbolKind.Field or CodeSymbolKind.Property or CodeSymbolKind.Method or CodeSymbolKind.Constructor
        or CodeSymbolKind.Event;

    public async Task<IReadOnlyList<SymbolHit>> FindByIdentifierAsync(int? projectId, string identifier, int max = 25, CancellationToken ct = default)
    {
        var norm = (identifier ?? "").Trim().ToLowerInvariant();
        if (norm.Length == 0) return Array.Empty<SymbolHit>();

        var exact = await _db.CodeSymbols
            .Where(s => s.ProjectId == projectId && s.NormalizedKey == norm)
            .Take(max).ToListAsync(ct);

        var syms = exact;
        if (syms.Count == 0) // fall back to contains-match (suffix identifiers, partials)
            syms = await _db.CodeSymbols
                .Where(s => s.ProjectId == projectId && s.NormalizedKey.Contains(norm))
                .OrderBy(s => s.NormalizedKey.Length).Take(max).ToListAsync(ct);

        var hits = await BuildHitsAsync(syms, ct);
        await LogAsync(projectId, identifier, "lexical", hits.Count, ct);
        return hits;
    }

    public async Task<IReadOnlyList<GraphNeighbor>> DependentsOfAsync(int? projectId, string identifier, CancellationToken ct = default)
    {
        var targets = await ResolveAsync(projectId, identifier, ct);
        if (targets.Count == 0) { await LogAsync(projectId, identifier, "dependents", 0, ct); return Array.Empty<GraphNeighbor>(); }
        var targetIds = targets.Select(t => t.Id).ToHashSet();

        // Incoming reference edges: things that reference the target(s).
        var edges = await _db.CodeEdges
            .Where(e => e.ProjectId == projectId && targetIds.Contains(e.ToSymbolId))
            .Select(e => new { e.FromSymbolId, e.RelationType, e.Confidence, e.Evidence })
            .ToListAsync(ct);

        var fromSyms = await _db.CodeSymbols.Where(s => edges.Select(x => x.FromSymbolId).Contains(s.Id)).ToListAsync(ct);
        var hitById = (await BuildHitsAsync(fromSyms, ct)).ToDictionary(h => h.Id);

        var result = edges
            .Where(e => hitById.ContainsKey(e.FromSymbolId))
            .Select(e => new GraphNeighbor(hitById[e.FromSymbolId], e.RelationType, "reference", "incoming", e.Confidence, e.Evidence))
            .ToList();
        await LogAsync(projectId, identifier, "dependents", result.Count, ct);
        return result;
    }

    public async Task<IReadOnlyList<GraphNeighbor>> DependenciesOfAsync(int? projectId, string identifier, CancellationToken ct = default)
    {
        var targets = await ResolveAsync(projectId, identifier, ct);
        if (targets.Count == 0) { await LogAsync(projectId, identifier, "dependencies", 0, ct); return Array.Empty<GraphNeighbor>(); }
        var targetIds = targets.Select(t => t.Id).ToHashSet();

        var outgoing = new List<GraphNeighbor>();

        // Outgoing reference edges: what the target references.
        var edges = await _db.CodeEdges
            .Where(e => e.ProjectId == projectId && targetIds.Contains(e.FromSymbolId))
            .Select(e => new { e.ToSymbolId, e.RelationType, e.Confidence, e.Evidence })
            .ToListAsync(ct);
        var toSyms = await _db.CodeSymbols.Where(s => edges.Select(x => x.ToSymbolId).Contains(s.Id)).ToListAsync(ct);
        var hitById = (await BuildHitsAsync(toSyms, ct)).ToDictionary(h => h.Id);
        outgoing.AddRange(edges.Where(e => hitById.ContainsKey(e.ToSymbolId))
            .Select(e => new GraphNeighbor(hitById[e.ToSymbolId], e.RelationType, "reference", "outgoing", e.Confidence, e.Evidence)));

        // Containment: the target's parent (PartOf).
        var parentIds = targets.Where(t => t.ParentSymbolId is int).Select(t => t.ParentSymbolId!.Value).ToHashSet();
        if (parentIds.Count > 0)
        {
            var parents = await _db.CodeSymbols.Where(s => parentIds.Contains(s.Id)).ToListAsync(ct);
            foreach (var ph in await BuildHitsAsync(parents, ct))
                outgoing.Add(new GraphNeighbor(ph, RelationType.PartOf, "containment", "outgoing"));
        }

        await LogAsync(projectId, identifier, "dependencies", outgoing.Count, ct);
        return outgoing;
    }

    public async Task<ImpactResult?> ImpactOfAsync(int? projectId, string identifier, int maxDepth = 4, int maxNodes = 250, CancellationToken ct = default)
    {
        var targets = await ResolveAsync(projectId, identifier, ct);
        if (targets.Count == 0) { await LogAsync(projectId, identifier, "impact", 0, ct); return null; }

        // Preload the project's edges and lightweight nodes once, then BFS in memory (bounded, deterministic).
        var edges = await _db.CodeEdges.Where(e => e.ProjectId == projectId)
            .Select(e => new { e.FromSymbolId, e.ToSymbolId, e.RelationType, e.Confidence }).ToListAsync(ct);
        var nodes = await _db.CodeSymbols.Where(s => s.ProjectId == projectId)
            .Select(s => new { s.Id, s.FullName, s.Kind, s.ParentSymbolId }).ToListAsync(ct);
        var nodeById = nodes.ToDictionary(n => n.Id);

        // Incoming adjacency: target -> [(dependent, relation, confidence)].
        var incoming = new Dictionary<int, List<(int From, RelationType Rel, double Conf)>>();
        foreach (var e in edges)
        {
            if (!incoming.TryGetValue(e.ToSymbolId, out var list)) incoming[e.ToSymbolId] = list = new();
            list.Add((e.FromSymbolId, e.RelationType, e.Confidence));
        }

        // Seed: the target(s), plus the owning object of any member target (a column pulls in its table).
        var seed = new HashSet<int>(targets.Select(t => t.Id));
        foreach (var t in targets)
            if (IsMember(t.Kind) && t.ParentSymbolId is int pid) seed.Add(pid);

        var visited = new HashSet<int>(seed);
        var found = new List<(int Id, RelationType Rel, int Depth, string Via, double Conf)>();
        var frontier = new HashSet<int>(seed);
        int depth = 1, maxReached = 0; bool truncated = false;

        while (frontier.Count > 0 && depth <= maxDepth && !truncated)
        {
            var next = new HashSet<int>();
            foreach (var toId in frontier)
            {
                if (!incoming.TryGetValue(toId, out var deps)) continue;
                var viaName = nodeById.TryGetValue(toId, out var vn) ? vn.FullName : "";
                foreach (var (fromId, rel, conf) in deps)
                {
                    if (visited.Contains(fromId)) continue;
                    visited.Add(fromId);
                    found.Add((fromId, rel, depth, viaName, conf));
                    next.Add(fromId);
                    // member dependents (a FK/constraint) flow up to their owning table so impact propagates.
                    if (nodeById.TryGetValue(fromId, out var fn) && IsMember(fn.Kind) && fn.ParentSymbolId is int p && !visited.Contains(p))
                        next.Add(p);
                    if (visited.Count >= maxNodes) { truncated = true; break; }
                }
                if (truncated) break;
            }
            maxReached = depth;
            frontier = next;
            depth++;
        }
        if (frontier.Count > 0) truncated = true;

        var hitById = (await BuildHitsAsync(
            await _db.CodeSymbols.Where(s => found.Select(f => f.Id).Contains(s.Id)).ToListAsync(ct), ct))
            .ToDictionary(h => h.Id);
        var targetHit = (await BuildHitsAsync(new List<CodeSymbol> { targets[0] }, ct)).First();

        var impactNodes = found.Where(f => hitById.ContainsKey(f.Id))
            .Select(f => new ImpactNode(hitById[f.Id], f.Rel, f.Depth, f.Via, f.Conf)).ToList();

        await LogAsync(projectId, identifier, "impact", impactNodes.Count, ct);
        return new ImpactResult(
            targetHit,
            impactNodes.Where(n => n.Depth == 1).ToList(),
            impactNodes.Where(n => n.Depth > 1).ToList(),
            maxReached, truncated);
    }

    // ---- helpers ----

    private async Task<List<CodeSymbol>> ResolveAsync(int? projectId, string identifier, CancellationToken ct)
    {
        var norm = (identifier ?? "").Trim().ToLowerInvariant();
        if (norm.Length == 0) return new();
        var exact = await _db.CodeSymbols.Where(s => s.ProjectId == projectId && s.NormalizedKey == norm).ToListAsync(ct);
        if (exact.Count > 0) return exact;
        // unique contains-match fallback (e.g. unqualified name)
        return await _db.CodeSymbols.Where(s => s.ProjectId == projectId && s.NormalizedKey.Contains(norm))
            .OrderBy(s => s.NormalizedKey.Length).Take(10).ToListAsync(ct);
    }

    private async Task<List<SymbolHit>> BuildHitsAsync(List<CodeSymbol> syms, CancellationToken ct)
    {
        if (syms.Count == 0) return new();
        var artIds = syms.Select(s => s.SourceArtifactId).Distinct().ToList();
        var paths = await _db.ImportedFiles.Where(f => artIds.Contains(f.Id))
            .Select(f => new { f.Id, f.RelativePath }).ToListAsync(ct);
        var pathById = paths.ToDictionary(p => p.Id, p => p.RelativePath);

        var parentIds = syms.Where(s => s.ParentSymbolId is int).Select(s => s.ParentSymbolId!.Value).Distinct().ToList();
        var parents = await _db.CodeSymbols.Where(s => parentIds.Contains(s.Id))
            .Select(s => new { s.Id, s.FullName }).ToListAsync(ct);
        var parentById = parents.ToDictionary(p => p.Id, p => p.FullName);

        return syms.Select(s => new SymbolHit(
            s.Id, s.Uid, s.FullName, s.Name, s.Kind, s.DetectedLanguage, s.ProjectId, s.Signature,
            s.StartLine, s.EndLine, s.SourceArtifactId,
            pathById.TryGetValue(s.SourceArtifactId, out var path) ? path : null,
            s.ParentSymbolId is int pid && parentById.TryGetValue(pid, out var pf) ? pf : null)).ToList();
    }

    private async Task LogAsync(int? projectId, string query, string mode, int resultCount, CancellationToken ct)
    {
        try
        {
            _db.Set<RetrievalEvent>().Add(new RetrievalEvent
            {
                ProjectId = projectId,
                Query = query.Length > 400 ? query.Substring(0, 400) : query,
                Mode = mode,
                ResultCount = resultCount
            });
            await _db.SaveChangesAsync(ct);
        }
        catch { /* capture-only: never fail the query */ }
    }
}
