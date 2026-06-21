using System.Text.Json;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-POC-ENTERPRISE: guards the proof-of-capability artifacts. Every test protects a real deliverable
// (scorecard validity, scenario completeness, evidence docs, source registry, pack uid uniqueness, searchable
// terms, smoke test). These run from the test bin dir, so they locate the repo root by walking up to the .sln.
public class PocReadinessTests
{
    private static string RepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 10 && dir is not null; i++, dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "LocalAIFactory.sln"))) return dir.FullName;
        throw new InvalidOperationException("Could not locate repo root (LocalAIFactory.sln).");
    }

    private static string Path2(params string[] parts) => Path.Combine(new[] { RepoRoot() }.Concat(parts).ToArray());

    // ---- 1-4. readiness scorecard: valid, required areas, scores 0..100, proofRequiredFor100 present ----
    [Fact]
    public void Readiness_scorecard_is_valid_with_22_areas_scored_0_to_100_and_proof_paths()
    {
        var path = Path2("docs", "readiness-scorecard.json");
        Assert.True(File.Exists(path), "readiness-scorecard.json missing");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var areas = doc.RootElement.GetProperty("areas");
        Assert.True(areas.GetArrayLength() >= 22, "expected >= 22 readiness areas");
        var ids = new HashSet<int>();
        foreach (var a in areas.EnumerateArray())
        {
            ids.Add(a.GetProperty("id").GetInt32());
            Assert.False(string.IsNullOrWhiteSpace(a.GetProperty("name").GetString()), "area name required");
            var score = a.GetProperty("currentScore").GetInt32();
            Assert.InRange(score, 0, 100);
            Assert.False(string.IsNullOrWhiteSpace(a.GetProperty("proofRequiredFor100").GetString()), "proofRequiredFor100 required");
            Assert.True(a.TryGetProperty("criteria", out _), "criteria required");
            // WP9 fields: targetScore (0..100, >= currentScore), status, pathTo100, nextActions
            var target = a.GetProperty("targetScore").GetInt32();
            Assert.InRange(target, 0, 100);
            Assert.True(target >= score, $"targetScore {target} should be >= currentScore {score}");
            Assert.False(string.IsNullOrWhiteSpace(a.GetProperty("status").GetString()), "status required");
            Assert.False(string.IsNullOrWhiteSpace(a.GetProperty("pathTo100").GetString()), "pathTo100 required");
            Assert.True(a.GetProperty("nextActions").ValueKind == JsonValueKind.Array, "nextActions array required");
        }
        // ids 1..22 all present
        for (int i = 1; i <= 22; i++) Assert.Contains(i, ids);
    }

    // ---- 5-7. enterprise scenarios exist; each has the four required files ----
    [Fact]
    public void Enterprise_scenarios_exist_with_required_files()
    {
        var dir = Path2("enterprise-scenarios");
        Assert.True(Directory.Exists(dir), "enterprise-scenarios folder missing");
        Assert.True(File.Exists(Path.Combine(dir, "README.md")), "scenarios README missing");
        var folders = Directory.GetDirectories(dir);
        Assert.True(folders.Length >= 14, $"expected >= 14 scenario folders, found {folders.Length}");
        foreach (var f in folders)
        {
            // Two valid shapes: canonical advisory scenarios (4 markdown files), or industrial capability
            // fixtures (README + a validation script that runs the benchmark proof). Either is acceptable.
            bool canonical = new[] { "scenario.md", "expected-capabilities.md", "acceptance-criteria.md", "test-questions.md" }
                .All(r => File.Exists(Path.Combine(f, r)));
            bool industrial = File.Exists(Path.Combine(f, "README.md")) && File.Exists(Path.Combine(f, "validation-script.ps1"));
            Assert.True(canonical || industrial, $"{Path.GetFileName(f)} has neither the canonical 4 files nor (README.md + validation-script.ps1)");
        }
    }

    // ---- 8-10,14. required evidence docs + scripts exist ----
    [Theory]
    [InlineData("docs/POC-Evidence-Pack.md")]
    [InlineData("docs/Readiness-Maturity-Model.md")]
    [InlineData("docs/Enterprise-Readiness-Scorecard.md")]
    [InlineData("docs/High-End-Enterprise-Solution-Comparison.md")]
    [InlineData("docs/Enterprise-Solution-Evaluation-Rubric.md")]
    [InlineData("docs/POC-Demo-Script.md")]
    [InlineData("docs/Public-Material-Learning-Governance.md")]
    [InlineData("docs/Repository-Cleanliness-Audit.md")]
    [InlineData("docs/Local-POC-Environment-Verification.md")]
    [InlineData("docs/LocalDB-POC-Evidence.md")]
    [InlineData("docs/HTTP-POC-Evidence.md")]
    [InlineData("docs/Ollama-Local-AI-POC-Evidence.md")]
    [InlineData("deploy/docs/hardware-sizing-guide.md")]
    [InlineData("benchmarks/repo-candidates.json")]
    [InlineData("scripts/poc/verify-poc.ps1")]
    [InlineData("scripts/poc/ui-smoke-test.ps1")]
    [InlineData("scripts/benchmark/run-enterprise-reasoning-benchmark.ps1")]
    public void Required_poc_artifact_exists(string rel)
    {
        var path = Path2(rel.Split('/'));
        Assert.True(File.Exists(path), $"missing {rel}");
        Assert.True(new FileInfo(path).Length > 200, $"{rel} looks empty/stub");
    }

    [Fact]
    public void Benchmark_repo_candidates_json_is_valid()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path2("benchmarks", "repo-candidates.json")));
        Assert.True(doc.RootElement.GetProperty("candidates").GetArrayLength() >= 10, "expected >= 10 candidate repos");
    }

    // ---- 11. source registry remains valid (parses, has sources, required metadata) ----
    [Fact]
    public void Source_registry_is_valid()
    {
        var path = Path2("knowledge-packs", "professional-base-v1", "source-registry.json");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var sources = doc.RootElement.GetProperty("sources");
        Assert.True(sources.GetArrayLength() >= 15, "expected a substantial source registry");
        var uids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in sources.EnumerateArray())
        {
            var uid = s.GetProperty("sourceUid").GetString();
            Assert.False(string.IsNullOrWhiteSpace(uid));
            Assert.True(uids.Add(uid!), $"duplicate sourceUid {uid}");
            foreach (var f in new[] { "title", "sourceType", "publisher", "allowedUse", "reliabilityLevel", "limitationNote" })
                Assert.False(string.IsNullOrWhiteSpace(s.GetProperty(f).GetString()), $"source {uid} missing {f}");
        }
    }

    // ---- 12. knowledge pack item uids remain globally unique + valid GUIDs ----
    [Fact]
    public void Knowledge_pack_uids_are_unique_and_valid()
    {
        var packDir = Path2("knowledge-packs", "professional-base-v1");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(packDir, "manifest.json")));
        var uids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int total = 0;
        foreach (var file in manifest.RootElement.GetProperty("files").EnumerateArray())
        {
            using var cat = JsonDocument.Parse(File.ReadAllText(Path.Combine(packDir, file.GetString()!)));
            foreach (var item in cat.RootElement.GetProperty("items").EnumerateArray())
            {
                total++;
                var uid = item.GetProperty("uid").GetString();
                Assert.True(Guid.TryParse(uid, out _), $"invalid uid {uid} in {file}");
                Assert.True(uids.Add(uid!), $"duplicate uid {uid}");
            }
        }
        Assert.True(total >= 300, $"expected the full pack (>=300 items), found {total}");
    }

    // ---- 13. the required Base Knowledge search terms exist in the pack content (data-level guarantee) ----
    [Theory]
    [InlineData("OCR")]
    [InlineData("Mauritius")]
    [InlineData("IFRS")]
    [InlineData("direct debit")]
    [InlineData("VB6")]
    [InlineData("signature")]
    [InlineData("Qdrant")]
    [InlineData("PDF")]
    public void Required_search_term_is_present_in_pack_content(string term)
    {
        var packDir = Path2("knowledge-packs", "professional-base-v1");
        using var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(packDir, "manifest.json")));
        bool found = false;
        foreach (var file in manifest.RootElement.GetProperty("files").EnumerateArray())
        {
            var text = File.ReadAllText(Path.Combine(packDir, file.GetString()!));
            if (text.Contains(term, StringComparison.OrdinalIgnoreCase)) { found = true; break; }
        }
        Assert.True(found, $"required search term '{term}' not present in any pack file");
    }
}
