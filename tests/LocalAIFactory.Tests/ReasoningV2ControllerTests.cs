using System.Collections;
using LocalAIFactory.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LocalAIFactory.Tests;

public class ReasoningV2ControllerTests
{
    private static ReasoningV2Controller Ctrl() => new();

    [Fact] public void Blocks_page_returns_a_view() => Assert.IsType<ViewResult>(Ctrl().Blocks());

    [Fact] public void Blocks_search_returns_matches()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().BlocksSearch("secure login"));
        Assert.NotEmpty(((IEnumerable)json.Value!).Cast<object>());
    }

    [Fact] public void Compose_returns_a_plan_with_blocks_for_a_known_requirement()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().BlocksCompose(new ReasoningV2Controller.ComposeRequest("build a secure login with lockout")));
        var blocks = json.Value!.GetType().GetProperty("blocks")!.GetValue(json.Value);
        Assert.NotEmpty(((IEnumerable)blocks!).Cast<object>());
    }

    [Fact] public void Compose_flags_missing_block_for_uncovered_capability()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().BlocksCompose(new ReasoningV2Controller.ComposeRequest("build an odoo inventory connector")));
        var missing = (IEnumerable)json.Value!.GetType().GetProperty("missingBlocks")!.GetValue(json.Value)!;
        Assert.Contains("odoo-inventory-connector", missing.Cast<string>());
    }

    [Fact] public void Agent_plan_is_compose_only_and_never_executes()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().AgentPlan(new ReasoningV2Controller.ComposeRequest("build a report endpoint")));
        var executed = (bool)json.Value!.GetType().GetProperty("executed")!.GetValue(json.Value)!;
        Assert.False(executed);
    }

    [Fact] public void Python_status_reports_optional_and_lists_approved_entrypoints()
    {
        var json = Assert.IsType<JsonResult>(Ctrl().PythonStatus());
        var approved = (IEnumerable)json.Value!.GetType().GetProperty("approvedEntrypoints")!.GetValue(json.Value)!;
        Assert.Contains("code-mine", approved.Cast<string>());
    }
}
