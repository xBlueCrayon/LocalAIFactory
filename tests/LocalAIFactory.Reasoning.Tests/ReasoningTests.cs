using LocalAIFactory.Reasoning.CodeGraph;
using LocalAIFactory.Reasoning.Experience;
using LocalAIFactory.Reasoning.Retrieval;
using Xunit;

namespace LocalAIFactory.Reasoning.Tests;

public class ReasoningTests
{
    private static SoftwareReasoningService Build(out ExperienceMemory mem, out KnowledgeIndex ki)
    {
        var graph = new CodeGraphBuilder().Build(SampleCode.Files());
        ki = new KnowledgeIndex();
        ki.Add(new KnowledgeIndex.Item("u1", "Failed-login lockout after N attempts", "ERP Auth Hardening", "erp-gold-auth-hardening-v1", "lockout failed login password policy authentication AppUser"));
        ki.Add(new KnowledgeIndex.Item("u2", "BOM-driven production order with material issue", "ERP Manufacturing Depth", "erp-gold-manufacturing-depth-v1", "manufacturing bom production order material issue stock costing"));
        mem = new ExperienceMemory();
        mem.Add(new ExperienceEntry { Type = ExperienceType.BuildFailure, Title = "Stale SQLite db missing columns", Symptoms = "EnsureCreated did not add AppUser columns 500 error", RootCause = "stale laferp.db", Fix = "delete db", ReusableLesson = "EnsureCreated never migrates a stale file" });
        return new SoftwareReasoningService(graph, ki, mem);
    }

    [Fact] public void FindSymbol_returns_exact_match()
    {
        var s = Build(out _, out _);
        Assert.Contains(s.FindSymbol("UserAuthService"), h => h.FullName.EndsWith("UserAuthService"));
    }

    [Fact] public void FindSymbol_falls_back_to_substring()
        => Assert.NotEmpty(Build(out _, out _).FindSymbol("Auth"));

    [Fact] public void FindImpact_of_appuser_reaches_dependents()
    {
        var s = Build(out _, out _);
        var impact = s.FindImpact("ErpDbContext").Select(h => h.Name).ToList();
        Assert.Contains("UserAuthService", impact);
        Assert.Contains("AccountController", impact);
    }

    [Fact] public void FindTestsForChange_returns_covering_tests()
    {
        var s = Build(out _, out _);
        Assert.Contains(s.FindTestsForChange("UserAuthService"), h => h.Name == "AuthTests");
    }

    [Fact] public void FindKnowledgeForTask_ranks_relevant_pack_item()
    {
        var s = Build(out _, out _);
        var hits = s.FindKnowledgeForTask("how does login lockout work for authentication");
        Assert.Equal("u1", hits.First().Uid);
    }

    [Fact] public void FindKnowledgeForTask_finds_manufacturing()
        => Assert.Contains(Build(out _, out _).FindKnowledgeForTask("production order bom material"), h => h.Uid == "u2");

    [Fact] public void FindGeneratorTemplateForFile_maps_to_template_path()
    {
        var s = Build(out _, out _);
        var tmpl = s.FindGeneratorTemplateForFile("generated-products/LAF-EnterpriseERP-Gold/src/LafErp.Web/Controllers/AccountController.cs");
        Assert.Equal("tools/LocalAIFactory.Generator/templates/erp-core/src/LafErp.Web/Controllers/AccountController.cs", tmpl);
    }

    [Fact] public void FindPriorSimilarFix_matches_symptom_keywords()
    {
        var s = Build(out _, out _);
        Assert.Contains(s.FindPriorSimilarFix("500 error stale db missing columns"), e => e.Title.Contains("Stale SQLite"));
    }

    [Fact] public void BuildReasoningContext_aggregates_all_dimensions()
    {
        var s = Build(out _, out _);
        var ctx = s.BuildReasoningContext("What code touches UserAuthService and ErpDbContext for login lockout?");
        Assert.NotEmpty(ctx.Symbols);
        Assert.NotEmpty(ctx.Impact);
        Assert.NotEmpty(ctx.Knowledge);
    }

    [Fact] public void Unknown_symbol_returns_empty_not_throw()
        => Assert.Empty(Build(out _, out _).FindImpact("NoSuchTypeXyz"));

    [Fact] public void KnowledgeIndex_empty_query_returns_empty()
        => Assert.Empty(Build(out _, out var ki).FindKnowledgeForTask("  "));

    [Fact] public void KnowledgeIndex_loads_from_real_packs_directory_if_present()
    {
        // Resolve repo root from the test assembly location; skip silently if not running in the repo.
        var dir = AppContext.BaseDirectory;
        string? root = null;
        for (var d = new DirectoryInfo(dir); d != null; d = d.Parent)
            if (Directory.Exists(Path.Combine(d.FullName, "knowledge-packs"))) { root = d.FullName; break; }
        if (root is null) return; // environment without the repo tree
        var ki = KnowledgeIndex.LoadFromPacks(Path.Combine(root, "knowledge-packs"));
        Assert.True(ki.Count > 0);
    }
}
