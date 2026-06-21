using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Reasoning.CodeGraph;

/// <summary>Edge relationship kinds in the reasoning code graph (a superset over the extractor's reference kinds).</summary>
public enum CodeEdgeKind
{
    Contains, Defines, Calls, References, Implements, Inherits,
    UsesEntity, UsesDbSet, UsesSqlObject, ExposesRoute, RendersView, PostsToAction,
    TestCovers, GeneratedFromTemplate, DependsOnKnowledge
}

/// <summary>A node in the in-memory code graph: a file, type, or member, tagged with semantic roles.</summary>
public sealed class CodeNode
{
    public required string Id { get; init; }            // stable key: "{kind}:{fullName}"
    public required CodeSymbolKind Kind { get; init; }
    public required string Name { get; init; }
    public required string FullName { get; init; }
    public string? FilePath { get; set; }
    public string? Signature { get; init; }
    public int StartLine { get; init; }
    public int EndLine { get; init; }
    public bool IsPublic { get; init; }
    /// <summary>Semantic roles inferred from naming/structure: controller, service, dbcontext, entity, test, apiroute, view.</summary>
    public HashSet<string> Roles { get; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record CodeEdge(string FromId, string ToId, CodeEdgeKind Kind, string? Detail = null);

/// <summary>
/// A queryable in-memory code graph. Built deterministically from the Roslyn symbol extractor; no DB, no
/// external services. Indexed for symbol lookup, neighbourhood traversal, and incoming-reference (impact) queries.
/// </summary>
public sealed class CodeGraphModel
{
    private readonly List<CodeNode> _nodes = new();
    private readonly List<CodeEdge> _edges = new();
    private readonly Dictionary<string, CodeNode> _byId = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<CodeNode>> _byName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<CodeEdge>> _outByFrom = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<CodeEdge>> _inByTo = new(StringComparer.Ordinal);

    public IReadOnlyList<CodeNode> Nodes => _nodes;
    public IReadOnlyList<CodeEdge> Edges => _edges;

    public CodeNode AddNode(CodeNode n)
    {
        if (_byId.TryGetValue(n.Id, out var existing)) return existing; // idempotent on Id
        _nodes.Add(n);
        _byId[n.Id] = n;
        if (!_byName.TryGetValue(n.Name, out var list)) _byName[n.Name] = list = new();
        list.Add(n);
        return n;
    }

    public void AddEdge(CodeEdge e)
    {
        if (!_byId.ContainsKey(e.FromId) || !_byId.ContainsKey(e.ToId)) return; // never dangle
        _edges.Add(e);
        if (!_outByFrom.TryGetValue(e.FromId, out var o)) _outByFrom[e.FromId] = o = new();
        o.Add(e);
        if (!_inByTo.TryGetValue(e.ToId, out var i)) _inByTo[e.ToId] = i = new();
        i.Add(e);
    }

    public CodeNode? GetById(string id) => _byId.TryGetValue(id, out var n) ? n : null;

    /// <summary>Find symbols whose simple name matches (case-insensitive). Exact name match.</summary>
    public IReadOnlyList<CodeNode> FindByName(string name) =>
        _byName.TryGetValue(name, out var list) ? list : Array.Empty<CodeNode>();

    /// <summary>Fuzzy/keyword find: name or full name contains the term.</summary>
    public IReadOnlyList<CodeNode> Search(string term) =>
        _nodes.Where(n => n.Name.Contains(term, StringComparison.OrdinalIgnoreCase)
                       || n.FullName.Contains(term, StringComparison.OrdinalIgnoreCase)).ToList();

    public IReadOnlyList<CodeNode> WithRole(string role) =>
        _nodes.Where(n => n.Roles.Contains(role)).ToList();

    public IReadOnlyList<CodeEdge> OutgoingFrom(string id) =>
        _outByFrom.TryGetValue(id, out var e) ? e : Array.Empty<CodeEdge>();

    public IReadOnlyList<CodeEdge> IncomingTo(string id) =>
        _inByTo.TryGetValue(id, out var e) ? e : Array.Empty<CodeEdge>();

    /// <summary>Direct referencers of a node (incoming References/Uses*/Implements/Inherits/Calls edges).</summary>
    public IReadOnlyList<CodeNode> ReferencersOf(string id) =>
        IncomingTo(id).Select(e => _byId[e.FromId]).Distinct().ToList();

    /// <summary>Transitive impact set: every node that (transitively) references the seed, up to maxDepth hops.</summary>
    public IReadOnlyList<CodeNode> ImpactOf(string id, int maxDepth = 3)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var frontier = new Queue<(string id, int depth)>();
        frontier.Enqueue((id, 0));
        while (frontier.Count > 0)
        {
            var (cur, depth) = frontier.Dequeue();
            if (depth >= maxDepth) continue;
            foreach (var e in IncomingTo(cur))
            {
                // containment is structural, not impact; follow real dependency edges only
                if (e.Kind == CodeEdgeKind.Contains) continue;
                if (seen.Add(e.FromId)) frontier.Enqueue((e.FromId, depth + 1));
            }
        }
        return seen.Select(s => _byId[s]).ToList();
    }
}
