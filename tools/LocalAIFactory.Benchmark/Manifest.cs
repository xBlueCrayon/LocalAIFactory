using System.Text.Json;
using System.Text.Json.Serialization;

namespace LocalAIFactory.Benchmark;

// Pinned, reproducible benchmark definitions. SHAs make every run deterministic against a fixed snapshot of
// each repository — the basis of regression detection.
public sealed class Manifest
{
    [JsonPropertyName("repos")] public List<RepoSpec> Repos { get; set; } = new();

    public static Manifest Load(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<Manifest>(json, Opts)
               ?? throw new InvalidOperationException("Empty manifest.");
    }

    public static readonly JsonSerializerOptions Opts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}

public sealed class RepoSpec
{
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string Type { get; set; } = "git";   // "git" | "sqlfiles" | "localfixture"
    public string Bucket { get; set; } = "";
    public string? GitUrl { get; set; }
    public string? Sha { get; set; }
    public string[]? SrcDirs { get; set; }
    public string[]? FileUrls { get; set; }
    // R2-ACC-CAP2: suite tiering + governance metadata. Tier drives --suite filtering; the rest is informational
    // (surfaced in the report) and keeps approved/pinned governance explicit. LocalDir is the committed synthetic
    // fixture folder under benchmarks/fixtures (used only by type "localfixture").
    public string Tier { get; set; } = "standard";   // "smoke" | "standard" | "extended"
    public string[]? Tags { get; set; }
    public string? License { get; set; }
    public string? ExpectedCapability { get; set; }
    public string? ExpectedGap { get; set; }
    public bool Approved { get; set; } = true;
    public string? LocalDir { get; set; }
    public List<PovCheck> ProofOfVision { get; set; } = new();
}

public sealed class PovCheck
{
    public string Question { get; set; } = "";
    public string Mode { get; set; } = "";   // "find" | "dependents" | "dependencies" | "impact"
    public string Target { get; set; } = "";
    public string[]? MustContain { get; set; }
    public int MinCount { get; set; }
}
