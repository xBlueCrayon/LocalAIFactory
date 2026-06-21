using LocalAIFactory.Reasoning.CodeGraph;
using LocalAIFactory.Reasoning.Experience;
using LocalAIFactory.Reasoning.Retrieval;

namespace LocalAIFactory.Reasoning;

/// <summary>
/// Lazily builds and caches a code graph + knowledge index + experience memory rooted at a content root, and
/// exposes a ready-to-use reasoning service. Build is deferred to first use and cached, so it never blocks app
/// startup or a request beyond the first; when no source tree is present it degrades to an empty graph (no hang,
/// no external service). Thread-safe.
/// </summary>
public sealed class ReasoningGraphProvider
{
    private readonly string _root;
    private readonly Lazy<SoftwareReasoningService> _service;
    private readonly Lazy<(int nodes, int edges, int knowledge)> _stats;
    public ExperienceMemory Experience { get; } = new();

    public ReasoningGraphProvider(string contentRoot)
    {
        _root = contentRoot;
        _service = new Lazy<SoftwareReasoningService>(Build, isThreadSafe: true);
        _stats = new Lazy<(int, int, int)>(() =>
        {
            var s = _service.Value; // triggers build
            return (_lastGraph!.Nodes.Count, _lastGraph.Edges.Count, _lastKnowledge!.Count);
        }, isThreadSafe: true);
    }

    private CodeGraphModel? _lastGraph;
    private KnowledgeIndex? _lastKnowledge;

    public ISoftwareReasoningService Service => _service.Value;
    public (int Nodes, int Edges, int Knowledge) Stats => _stats.Value;
    public bool IsEmpty => Stats.Nodes == 0;

    private SoftwareReasoningService Build()
    {
        var builder = new CodeGraphBuilder();
        var files = new List<(string, string)>();
        foreach (var sub in new[] { "src", "tools", "generated-products" })
        {
            var dir = Path.Combine(_root, sub);
            if (!Directory.Exists(dir)) continue;
            foreach (var f in CodeGraphBuilder.EnumerateCsFiles(dir, 8000))
            {
                try { files.Add((f.Replace('\\', '/'), File.ReadAllText(f))); } catch { /* skip unreadable */ }
            }
        }
        _lastGraph = builder.Build(files);
        _lastKnowledge = TryLoadKnowledge();
        return new SoftwareReasoningService(_lastGraph, _lastKnowledge, Experience);
    }

    private KnowledgeIndex TryLoadKnowledge()
    {
        var packs = Path.Combine(_root, "knowledge-packs");
        return Directory.Exists(packs) ? KnowledgeIndex.LoadFromPacks(packs) : new KnowledgeIndex();
    }
}
