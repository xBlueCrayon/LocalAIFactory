using System.Text.Json;

namespace LocalAIFactory.Reasoning.Experience;

public enum ExperienceType
{
    BuildFailure, TestFailure, PlaywrightFailure, SecurityFinding, BugFix,
    GeneratorImprovement, KnowledgeImprovement, DeploymentIssue, RuntimeError, RegressionPrevented
}

/// <summary>One recorded engineering experience: a symptom, its root cause, the fix, and the reusable lesson.</summary>
public sealed class ExperienceEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("n");
    public required ExperienceType Type { get; init; }
    public required string Title { get; init; }
    public string Source { get; init; } = "";          // sprint/report it came from
    public string Repo { get; init; } = "";
    public List<string> AffectedFiles { get; init; } = new();
    public string Symptoms { get; init; } = "";
    public string RootCause { get; init; } = "";
    public string Fix { get; init; } = "";
    public List<string> TestsAdded { get; init; } = new();
    public string ReusableLesson { get; init; } = "";
    public string Confidence { get; init; } = "medium"; // high|medium|low
    public bool PromotedToKnowledgePack { get; set; }
    public List<string> RelatedKnowledgeIds { get; init; } = new();
    public List<string> RelatedCodeNodes { get; init; } = new();
}

public interface IExperienceMemory
{
    ExperienceEntry Add(ExperienceEntry entry);
    IReadOnlyList<ExperienceEntry> All { get; }
    IReadOnlyList<ExperienceEntry> FindSimilar(string symptom, int top = 5);
    IReadOnlyList<ExperienceEntry> OfType(ExperienceType type);
    /// <summary>Mark an experience as promoted to a knowledge pack. Idempotent — a second promotion is a no-op and returns false.</summary>
    bool PromoteToKnowledge(string experienceId, string knowledgeUid);
    void LinkCodeNode(string experienceId, string nodeId);
}

/// <summary>
/// In-memory experience store with deterministic keyword similarity, idempotent promotion to knowledge, and
/// optional JSON persistence. No DB required to function or to test.
/// </summary>
public sealed class ExperienceMemory : IExperienceMemory
{
    private readonly List<ExperienceEntry> _entries = new();
    public IReadOnlyList<ExperienceEntry> All => _entries;

    public ExperienceEntry Add(ExperienceEntry entry) { _entries.Add(entry); return entry; }

    public IReadOnlyList<ExperienceEntry> OfType(ExperienceType type) => _entries.Where(e => e.Type == type).ToList();

    public IReadOnlyList<ExperienceEntry> FindSimilar(string symptom, int top = 5)
    {
        var terms = symptom.ToLowerInvariant().Split(new[] { ' ', ',', '.', ':', '\n', '\t', '(', ')' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(t => t.Length > 2).Distinct().ToArray();
        if (terms.Length == 0) return Array.Empty<ExperienceEntry>();
        return _entries.Select(e =>
            {
                var hay = (e.Title + " " + e.Symptoms + " " + e.RootCause + " " + e.ReusableLesson).ToLowerInvariant();
                return (e, score: terms.Count(t => hay.Contains(t)));
            })
            .Where(x => x.score > 0)
            .OrderByDescending(x => x.score)
            .Take(top).Select(x => x.e).ToList();
    }

    public bool PromoteToKnowledge(string experienceId, string knowledgeUid)
    {
        var e = _entries.FirstOrDefault(x => x.Id == experienceId);
        if (e is null || e.PromotedToKnowledgePack) return false; // no duplicate promotion
        e.PromotedToKnowledgePack = true;
        e.RelatedKnowledgeIds.Add(knowledgeUid);
        return true;
    }

    public void LinkCodeNode(string experienceId, string nodeId)
    {
        var e = _entries.FirstOrDefault(x => x.Id == experienceId);
        if (e != null && !e.RelatedCodeNodes.Contains(nodeId)) e.RelatedCodeNodes.Add(nodeId);
    }

    public string ToJson() => JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });

    public static ExperienceMemory FromJson(string json)
    {
        var mem = new ExperienceMemory();
        try
        {
            var list = JsonSerializer.Deserialize<List<ExperienceEntry>>(json);
            if (list != null) mem._entries.AddRange(list);
        }
        catch { /* corrupt store -> empty memory */ }
        return mem;
    }
}
