using System.IO;
using LocalAIFactory.Reasoning;
using LocalAIFactory.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LocalAIFactory.Tests;

/// <summary>Builds the reasoning graph over the real repo once and shares it across the controller tests.</summary>
public sealed class ReasoningProviderFixture
{
    public ReasoningGraphProvider Provider { get; }
    public bool InRepo { get; }
    public ReasoningProviderFixture()
    {
        string? root = null;
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
            if (File.Exists(Path.Combine(d.FullName, "LocalAIFactory.sln"))) { root = d.FullName; break; }
        InRepo = root != null;
        Provider = new ReasoningGraphProvider(root ?? AppContext.BaseDirectory);
    }
}

public sealed class ReasoningControllerTests : IClassFixture<ReasoningProviderFixture>
{
    private readonly ReasoningProviderFixture _fx;
    private ReasoningController Ctrl() => new(_fx.Provider);
    public ReasoningControllerTests(ReasoningProviderFixture fx) => _fx = fx;

    [Fact] public void Index_returns_a_view()
        => Assert.IsType<ViewResult>(Ctrl().Index());

    [Fact] public void Status_reports_graph_stats()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().Status());
        Assert.NotNull(json.Value);
        if (_fx.InRepo)
        {
            var nodes = (int)json.Value!.GetType().GetProperty("nodes")!.GetValue(json.Value)!;
            Assert.True(nodes > 0);
        }
    }

    [Fact] public void Symbol_search_finds_a_real_type()
    {
        if (!_fx.InRepo) return;
        var json = Assert.IsType<JsonResult>(Ctrl().SymbolSearch("UserAuthService"));
        var list = ((System.Collections.IEnumerable)json.Value!).Cast<object>().ToList();
        Assert.NotEmpty(list);
    }

    [Fact] public void Symbol_search_empty_query_returns_empty()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().SymbolSearch(""));
        Assert.Empty(((System.Collections.IEnumerable)json.Value!).Cast<object>());
    }

    [Fact] public void Impact_endpoint_returns_json()
        => Assert.IsType<JsonResult>(Ctrl().Impact("StockLedgerEntry"));

    [Fact] public void Tests_for_change_endpoint_returns_json()
        => Assert.IsType<JsonResult>(Ctrl().TestsForChange("UserAuthService"));

    [Fact] public void Knowledge_for_task_returns_json()
        => Assert.IsType<JsonResult>(Ctrl().KnowledgeForTask("authentication lockout"));

    [Fact] public void Experience_search_returns_json()
        => Assert.IsType<JsonResult>(Ctrl().ExperienceSearch("build failure"));

    [Fact] public void Model_router_status_reports_ollama_optional()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().ModelRouterStatus());
        var req = (bool)json.Value!.GetType().GetProperty("ollamaRequired")!.GetValue(json.Value)!;
        Assert.False(req);
    }

    [Fact] public void Agent_dry_run_blocks_forbidden_and_never_executes()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().AgentRunDry(new ReasoningController.DryRunRequest("git push")));
        var risk = (string)json.Value!.GetType().GetProperty("risk")!.GetValue(json.Value)!;
        var executed = (bool)json.Value!.GetType().GetProperty("executed")!.GetValue(json.Value)!;
        Assert.Equal("Forbidden", risk);
        Assert.False(executed);
    }

    [Fact] public void Agent_dry_run_allows_readonly_command_without_executing()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().AgentRunDry(new ReasoningController.DryRunRequest("git status")));
        var executed = (bool)json.Value!.GetType().GetProperty("executed")!.GetValue(json.Value)!;
        Assert.False(executed);
    }
}
