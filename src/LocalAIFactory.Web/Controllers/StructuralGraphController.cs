using LocalAIFactory.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

// KE-011: thin read-only JSON surface over deterministic structural retrieval. Answers the Proof-of-Vision
// questions directly from MSSQL (no vectors, no model). Backs the future Pilot Impact/Retrieval screens and
// the benchmark validation harness. Cited: every result carries Uid + artifact path + line span.
public sealed class StructuralGraphController : Controller
{
    private readonly IStructuralRetrievalService _retrieval;

    public StructuralGraphController(IStructuralRetrievalService retrieval) { _retrieval = retrieval; }

    // GET /StructuralGraph/Find?projectId=1&q=dbo.Customer  — exact-identifier lexical lookup.
    [HttpGet]
    public async Task<IActionResult> Find(int? projectId, string? q, CancellationToken ct)
        => Json(await _retrieval.FindByIdentifierAsync(projectId, q ?? "", 25, ct));

    // GET /StructuralGraph/Dependents?projectId=1&q=dbo.Customer — "what references / depends on X".
    [HttpGet]
    public async Task<IActionResult> Dependents(int? projectId, string? q, CancellationToken ct)
        => Json(await _retrieval.DependentsOfAsync(projectId, q ?? "", ct));

    // GET /StructuralGraph/Dependencies?projectId=1&q=dbo.usp_GetBalance — "what X references".
    [HttpGet]
    public async Task<IActionResult> Dependencies(int? projectId, string? q, CancellationToken ct)
        => Json(await _retrieval.DependenciesOfAsync(projectId, q ?? "", ct));

    // GET /StructuralGraph/Impact?projectId=1&q=dbo.Customer&depth=4 — transitive blast radius.
    [HttpGet]
    public async Task<IActionResult> Impact(int? projectId, string? q, int depth, CancellationToken ct)
        => Json(await _retrieval.ImpactOfAsync(projectId, q ?? "", depth <= 0 ? 4 : depth, 250, ct));
}
