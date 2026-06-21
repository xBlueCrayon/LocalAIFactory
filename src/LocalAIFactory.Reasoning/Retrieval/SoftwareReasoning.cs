using System.Text.Json;
using LocalAIFactory.Reasoning.CodeGraph;
using LocalAIFactory.Reasoning.Experience;

namespace LocalAIFactory.Reasoning.Retrieval;

public sealed record SymbolHit(string Name, string FullName, string Kind, string? FilePath, int StartLine, IReadOnlyList<string> Roles);
public sealed record KnowledgeHit(string Uid, string Title, string Category, string Pack, double Score);
public sealed record ReasoningContext(
    string Task,
    IReadOnlyList<SymbolHit> Symbols,
    IReadOnlyList<SymbolHit> Impact,
    IReadOnlyList<SymbolHit> Tests,
    IReadOnlyList<KnowledgeHit> Knowledge,
    IReadOnlyList<ExperienceEntry> PriorFixes);

/// <summary>
/// Deterministic software-reasoning retrieval over the code graph, the knowledge index and experience
/// memory. No model and no external service are required; an optional model only enriches explanations.
/// </summary>
public interface ISoftwareReasoningService
{
    IReadOnlyList<SymbolHit> FindSymbol(string name);
    IReadOnlyList<SymbolHit> FindImpact(string symbolName, int maxDepth = 3);
    IReadOnlyList<SymbolHit> FindTestsForChange(string symbolName);
    IReadOnlyList<KnowledgeHit> FindKnowledgeForTask(string task, int top = 10);
    string? FindGeneratorTemplateForFile(string generatedFilePath);
    IReadOnlyList<ExperienceEntry> FindPriorSimilarFix(string symptom, int top = 5);
    ReasoningContext BuildReasoningContext(string task);
}

public sealed class SoftwareReasoningService : ISoftwareReasoningService
{
    private readonly CodeGraph.CodeGraphModel _graph;
    private readonly KnowledgeIndex _knowledge;
    private readonly IExperienceMemory _experience;

    public SoftwareReasoningService(CodeGraph.CodeGraphModel graph, KnowledgeIndex knowledge, IExperienceMemory experience)
    { _graph = graph; _knowledge = knowledge; _experience = experience; }

    public IReadOnlyList<SymbolHit> FindSymbol(string name)
    {
        var exact = _graph.FindByName(name);
        var hits = (exact.Count > 0 ? exact : _graph.Search(name)).Select(Map).ToList();
        return hits;
    }

    public IReadOnlyList<SymbolHit> FindImpact(string symbolName, int maxDepth = 3)
    {
        var seeds = _graph.FindByName(symbolName);
        var impact = new Dictionary<string, CodeNode>(StringComparer.Ordinal);
        foreach (var s in seeds)
            foreach (var n in _graph.ImpactOf(s.Id, maxDepth))
                impact[n.Id] = n;
        return impact.Values.Select(Map).ToList();
    }

    public IReadOnlyList<SymbolHit> FindTestsForChange(string symbolName)
    {
        var seeds = _graph.FindByName(symbolName);
        var tests = new Dictionary<string, CodeNode>(StringComparer.Ordinal);
        foreach (var s in seeds)
            foreach (var refr in _graph.ReferencersOf(s.Id))
                if (refr.Roles.Contains("test")) tests[refr.Id] = refr;
        // also any test whose impact reaches the seed
        foreach (var s in seeds)
            foreach (var n in _graph.ImpactOf(s.Id, 4))
                if (n.Roles.Contains("test")) tests[n.Id] = n;
        return tests.Values.Select(Map).ToList();
    }

    public IReadOnlyList<KnowledgeHit> FindKnowledgeForTask(string task, int top = 10) =>
        _knowledge.Search(task, top);

    public string? FindGeneratorTemplateForFile(string generatedFilePath)
    {
        // Generated ERP files mirror the template tree under templates/erp-core/. Map a product-relative
        // path to its template source deterministically.
        var p = generatedFilePath.Replace('\\', '/');
        var idx = p.IndexOf("/src/", StringComparison.OrdinalIgnoreCase);
        var tail = idx >= 0 ? p[(idx + 1)..] : p.TrimStart('/');
        if (!tail.StartsWith("src/")) return null;
        return $"tools/LocalAIFactory.Generator/templates/erp-core/{tail}";
    }

    public IReadOnlyList<ExperienceEntry> FindPriorSimilarFix(string symptom, int top = 5) =>
        _experience.FindSimilar(symptom, top);

    public ReasoningContext BuildReasoningContext(string task)
    {
        // Extract candidate symbol names (Capitalised tokens) from the task to seed the graph queries.
        var tokens = task.Split(new[] { ' ', ',', '.', '?', '!', '(', ')', '"', '\'', ':' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2 && char.IsUpper(t[0])).Distinct().ToList();
        var symbols = tokens.SelectMany(_graph.FindByName).DistinctBy(n => n.Id).Select(Map).ToList();
        var impact = tokens.SelectMany(t => FindImpact(t)).DistinctBy(s => s.FullName).Take(50).ToList();
        var tests = tokens.SelectMany(FindTestsForChange).DistinctBy(s => s.FullName).Take(30).ToList();
        var knowledge = FindKnowledgeForTask(task);
        var prior = FindPriorSimilarFix(task);
        return new ReasoningContext(task, symbols, impact, tests, knowledge, prior);
    }

    private static SymbolHit Map(CodeNode n) =>
        new(n.Name, n.FullName, n.Kind.ToString(), n.FilePath, n.StartLine, n.Roles.ToList());
}

/// <summary>Deterministic keyword index over installed knowledge-pack category items (no embeddings required).</summary>
public sealed class KnowledgeIndex
{
    public sealed record Item(string Uid, string Title, string Category, string Pack, string Text);
    private readonly List<Item> _items = new();
    public int Count => _items.Count;

    public void Add(Item item) => _items.Add(item);

    /// <summary>Load every pack's category JSON under a knowledge-packs root. Best-effort and resilient.</summary>
    public static KnowledgeIndex LoadFromPacks(string knowledgePacksRoot)
    {
        var idx = new KnowledgeIndex();
        if (!Directory.Exists(knowledgePacksRoot)) return idx;
        foreach (var packDir in Directory.EnumerateDirectories(knowledgePacksRoot))
        {
            var packName = Path.GetFileName(packDir);
            foreach (var json in Directory.EnumerateFiles(packDir, "*.json"))
            {
                if (Path.GetFileName(json) is "manifest.json" or "source-registry.json") continue;
                try
                {
                    using var doc = JsonDocument.Parse(File.ReadAllText(json));
                    if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array) continue;
                    var category = doc.RootElement.TryGetProperty("category", out var c) ? c.GetString() ?? "" : "";
                    foreach (var it in items.EnumerateArray())
                    {
                        var uid = Str(it, "uid");
                        var title = Str(it, "title");
                        var text = string.Join(" ", new[] { title, Str(it, "description"), Str(it, "applicability"), string.Join(" ", Tags(it)) });
                        idx.Add(new Item(uid, title, category, packName, text));
                    }
                }
                catch { /* skip malformed pack file */ }
            }
        }
        return idx;
    }

    public IReadOnlyList<KnowledgeHit> Search(string query, int top = 10)
    {
        var terms = query.ToLowerInvariant().Split(new[] { ' ', ',', '.', '?', '!', '-', '_', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2).Distinct().ToArray();
        if (terms.Length == 0) return Array.Empty<KnowledgeHit>();
        return _items.Select(i =>
            {
                var text = i.Text.ToLowerInvariant();
                double score = terms.Count(t => text.Contains(t));
                return (i, score);
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(top)
            .Select(x => new KnowledgeHit(x.i.Uid, x.i.Title, x.i.Category, x.i.Pack, x.score / terms.Length))
            .ToList();
    }

    private static string Str(JsonElement e, string prop) => e.TryGetProperty(prop, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() ?? "" : "";
    private static IEnumerable<string> Tags(JsonElement e) =>
        e.TryGetProperty("tags", out var t) && t.ValueKind == JsonValueKind.Array ? t.EnumerateArray().Select(x => x.GetString() ?? "") : Array.Empty<string>();
}
