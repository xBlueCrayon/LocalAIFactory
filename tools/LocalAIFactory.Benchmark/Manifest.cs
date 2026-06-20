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
    public string Type { get; set; } = "git";   // "git" | "sqlfiles"
    public string Bucket { get; set; } = "";
    public string? GitUrl { get; set; }
    public string? Sha { get; set; }
    public string[]? SrcDirs { get; set; }
    public string[]? FileUrls { get; set; }
    public List<PovCheck> ProofOfVision { get; set; } = new();
}

public sealed class PovCheck
{
    public string Question { get; set; } = "";
    public string Mode { get; set; } = "";   // "find" | "dependents" | "impact"
    public string Target { get; set; } = "";
    public string[]? MustContain { get; set; }
    public int MinCount { get; set; }
}
