using LocalAIFactory.CodeBlocks;
using LocalAIFactory.PythonBridge;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

/// <summary>
/// Reasoning V2 surface: building-block search/compose, Python worker status, and a SAFE agent plan (compose-only,
/// never executes). Deterministic and dependency-light — no Ollama, no Python, no network required to respond.
/// </summary>
public sealed class ReasoningV2Controller : Controller
{
    private static readonly CodeBlockCatalog Catalog = CodeBlockCatalog.Default();

    [HttpGet("/Reasoning/Blocks")]
    public IActionResult Blocks()
    {
        ViewBag.BlockCount = Catalog.Count;
        return View();
    }

    [HttpGet("/api/reasoning/blocks/search")]
    public IActionResult BlocksSearch(string q) =>
        string.IsNullOrWhiteSpace(q)
            ? Json(Catalog.All.Select(b => new { b.BlockId, b.Name, b.Purpose }))
            : Json(Catalog.FindByRequirement(q).Select(m => new { m.Block.BlockId, m.Block.Name, m.Score }));

    public sealed record ComposeRequest(string Requirement);

    [HttpPost("/api/reasoning/blocks/compose")]
    public IActionResult BlocksCompose([FromBody] ComposeRequest request)
    {
        var plan = new BlockComposer(Catalog).Compose(request?.Requirement ?? "");
        return Json(new
        {
            requirement = plan.Requirement,
            blocks = plan.Blocks.Select(b => new { b.Block.BlockId, b.Block.Name, b.Reason }),
            missingBlocks = plan.MissingBlocks,
            files = plan.Files, tests = plan.Tests, playwright = plan.PlaywrightProofs,
            securityRisks = plan.SecurityRisks, migrationRisks = plan.MigrationRisks,
            knowledgeUsed = plan.KnowledgeUsed, generatorTemplates = plan.GeneratorTemplates,
            confidence = plan.Confidence
        });
    }

    /// <summary>SAFE agent plan: composes a feature plan from blocks. It does NOT run a patch — that requires the isolated runner.</summary>
    [HttpPost("/api/reasoning/agent/plan")]
    public IActionResult AgentPlan([FromBody] ComposeRequest request)
    {
        var plan = new BlockComposer(Catalog).Compose(request?.Requirement ?? "");
        return Json(new
        {
            requirement = plan.Requirement,
            wouldEditFiles = plan.Files,
            wouldAddTests = plan.Tests,
            missingBuildingBlocks = plan.MissingBlocks,
            risks = plan.SecurityRisks.Concat(plan.MigrationRisks),
            confidence = plan.Confidence,
            executed = false,
            note = "Plan only. Applying a patch requires the isolated worktree runner with build/test gates."
        });
    }

    [HttpGet("/api/reasoning/python/status")]
    public IActionResult PythonStatus()
    {
        var runner = new SafePythonWorkerRunner();
        return Json(new
        {
            available = runner.IsAvailable,
            approvedEntrypoints = runner.ApprovedEntrypoints,
            note = "Python is optional; the platform degrades when it is absent."
        });
    }
}
