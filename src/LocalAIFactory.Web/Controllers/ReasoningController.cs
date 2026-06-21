using LocalAIFactory.Reasoning;
using LocalAIFactory.Reasoning.LocalModels;
using LocalAIFactory.Reasoning.Safety;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

/// <summary>
/// Exposes the LAF Software Reasoning Engine: code-graph symbol search, impact, tests-for-change,
/// knowledge-for-task, experience search, model-router status, and a SAFE agent dry-run (classification only,
/// never executes). All endpoints are deterministic and degrade gracefully — no Ollama or Qdrant required.
/// </summary>
public sealed class ReasoningController : Controller
{
    private readonly ReasoningGraphProvider _provider;
    public ReasoningController(ReasoningGraphProvider provider) => _provider = provider;

    [HttpGet("/Reasoning")]
    public IActionResult Index()
    {
        var (nodes, edges, knowledge) = _provider.Stats;
        ViewBag.Nodes = nodes; ViewBag.Edges = edges; ViewBag.Knowledge = knowledge;
        ViewBag.Empty = _provider.IsEmpty;
        return View();
    }

    [HttpGet("/api/reasoning/status")]
    public IActionResult Status()
    {
        var (nodes, edges, knowledge) = _provider.Stats;
        return Json(new { nodes, edges, knowledge, empty = _provider.IsEmpty });
    }

    [HttpGet("/api/reasoning/symbol-search")]
    public IActionResult SymbolSearch(string q) =>
        string.IsNullOrWhiteSpace(q) ? Json(System.Array.Empty<object>())
            : Json(_provider.Service.FindSymbol(q).Take(50));

    [HttpGet("/api/reasoning/impact")]
    public IActionResult Impact(string symbol) =>
        string.IsNullOrWhiteSpace(symbol) ? Json(System.Array.Empty<object>())
            : Json(_provider.Service.FindImpact(symbol).Take(100));

    [HttpGet("/api/reasoning/tests-for-change")]
    public IActionResult TestsForChange(string symbol) =>
        string.IsNullOrWhiteSpace(symbol) ? Json(System.Array.Empty<object>())
            : Json(_provider.Service.FindTestsForChange(symbol));

    [HttpGet("/api/reasoning/knowledge-for-task")]
    public IActionResult KnowledgeForTask(string task) =>
        string.IsNullOrWhiteSpace(task) ? Json(System.Array.Empty<object>())
            : Json(_provider.Service.FindKnowledgeForTask(task));

    [HttpGet("/api/reasoning/experience/search")]
    public IActionResult ExperienceSearch(string q) =>
        string.IsNullOrWhiteSpace(q) ? Json(System.Array.Empty<object>())
            : Json(_provider.Service.FindPriorSimilarFix(q));

    [HttpGet("/api/reasoning/model-router/status")]
    public IActionResult ModelRouterStatus()
    {
        // Deterministic: report the configured roster + role mapping. Ollama is optional; no external call here.
        var router = new LocalModelRouter();
        return Json(new
        {
            ollamaRequired = false,
            roster = router.Roster.Select(r => new { model = r.Name, role = r.Role.ToString() }),
            codeReviewer = router.SelectModel(ModelRole.CodeReviewer),
            planner = router.SelectModel(ModelRole.Planner)
        });
    }

    public sealed record DryRunRequest(string Command);

    [HttpPost("/api/reasoning/agent/run-dry")]
    public IActionResult AgentRunDry([FromBody] DryRunRequest request)
    {
        // SAFE: classify the proposed command only; never execute it.
        var risk = new CommandRiskClassifier().Classify(request?.Command ?? "");
        var allowed = risk is CommandRisk.ReadOnly or CommandRisk.SafeValidation;
        return Json(new { command = request?.Command ?? "", risk = risk.ToString(), wouldRunWithoutWorktree = allowed, executed = false });
    }
}
