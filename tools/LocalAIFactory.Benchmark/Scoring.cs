using System.Text.Json.Serialization;

namespace LocalAIFactory.Benchmark;

// Bronze/Silver/Gold scoring across four axes. A repo's overall tier is the LOWEST axis tier (conservative
// and honest — a repo is only Gold if discovery, graph, retrieval and impact are all Gold).
public static class Scoring
{
    public static string Tier(string axis, double v) => axis switch
    {
        "discovery"  => v >= 0.98 ? "Gold" : v >= 0.95 ? "Silver" : v >= 0.90 ? "Bronze" : "None",
        "graph"      => v >= 0.90 ? "Gold" : v >= 0.80 ? "Silver" : v > 0.0 ? "Bronze" : "None",
        "retrieval"  => v >= 0.95 ? "Gold" : v >= 0.85 ? "Silver" : v > 0.0 ? "Bronze" : "None",
        "impact"     => v >= 0.95 ? "Gold" : v >= 0.85 ? "Silver" : v > 0.0 ? "Bronze" : "None",
        _ => "None"
    };

    private static readonly string[] Order = { "None", "Bronze", "Silver", "Gold" };
    public static string Lowest(params string[] tiers)
        => tiers.OrderBy(t => Array.IndexOf(Order, t)).First();
}

// The deterministic, comparable result for one repo — also the golden-snapshot shape (regression unit).
public sealed class RepoResult
{
    public string Name { get; set; } = "";
    public string Bucket { get; set; } = "";
    public string Sha { get; set; } = "";
    public int CandidateFiles { get; set; }
    public int ParsedArtifacts { get; set; }
    public int Symbols { get; set; }
    public int Edges { get; set; }
    public int References { get; set; }
    public int Unresolved { get; set; }
    public Dictionary<string, int> EdgesByType { get; set; } = new();
    public bool Convergent { get; set; }
    public List<PovResult> Pov { get; set; } = new();

    // R2-P0A: honest coverage / gap metrics (every benchmark report must carry these).
    public CoverageBlock? Coverage { get; set; }

    public double DiscoveryCoverage => CandidateFiles == 0 ? 0 : (double)ParsedArtifacts / CandidateFiles;

    // Resolution rate is INFORMATIONAL, not a quality axis: a low value is correct — most references point to
    // external framework/BCL types that are not (and should not be) in the corpus. Reported, never scored.
    public double ResolutionRate => (Edges + Unresolved) == 0 ? 0 : (double)Edges / (Edges + Unresolved);

    // Graph integrity = convergent (idempotent rebuild) AND the graph-dependent Proof-of-Vision questions
    // (dependents/impact) return the expected edges. This measures edge CORRECTNESS, not raw resolution.
    public double GraphAccuracy => Convergent ? PassRate("dependents", "impact") : 0.0;
    public double RetrievalAccuracy => PassRate("find", "dependents");
    public double ImpactAccuracy => PassRate("impact");

    private double PassRate(params string[] modes)
    {
        var items = Pov.Where(p => modes.Contains(p.Mode)).ToList();
        return items.Count == 0 ? 1.0 : (double)items.Count(p => p.Passed) / items.Count;
    }

    [JsonIgnore] public string DiscoveryTier => Scoring.Tier("discovery", DiscoveryCoverage);
    [JsonIgnore] public string GraphTier => Scoring.Tier("graph", GraphAccuracy);
    [JsonIgnore] public string RetrievalTier => Scoring.Tier("retrieval", RetrievalAccuracy);
    [JsonIgnore] public string ImpactTier => Scoring.Tier("impact", ImpactAccuracy);
    [JsonIgnore] public string OverallTier => Scoring.Lowest(DiscoveryTier, GraphTier, RetrievalTier, ImpactTier);
}

public sealed class PovResult
{
    public string Question { get; set; } = "";
    public string Mode { get; set; } = "";
    public string Target { get; set; } = "";
    public int Count { get; set; }
    public bool Passed { get; set; }
    public string? Detail { get; set; }
}

// R2-P0A: the gap report carried by every benchmark — what was and was not understood.
public sealed class CoverageBlock
{
    public int FilesDiscovered { get; set; }
    public int FilesImported { get; set; }
    public int FilesExtracted { get; set; }
    public int FilesNoSymbols { get; set; }
    public int FilesUnsupported { get; set; }
    public int FilesParseError { get; set; }
    public int FilesNonCode { get; set; }
    public int FilesSkipped { get; set; }
    public int ResolvedReferences { get; set; }
    public int UnresolvedReferences { get; set; }
    public List<string> UnsupportedLanguages { get; set; } = new();
}
