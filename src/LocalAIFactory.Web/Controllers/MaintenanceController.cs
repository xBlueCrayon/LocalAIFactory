using LocalAIFactory.Core.Abstractions;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

// KE-012: on-demand structural consolidation. Re-extracts the deterministic layer from raw, prunes orphans,
// and converges the graph for a project. Idempotent — safe to run repeatedly. Touches only derived
// structural data; curated knowledge is never affected.
public sealed class MaintenanceController : Controller
{
    private readonly IStructuralConsolidationService _consolidation;

    public MaintenanceController(IStructuralConsolidationService consolidation) { _consolidation = consolidation; }

    // POST /Maintenance/Consolidate?projectId=1   (projectId omitted = global/unscoped project)
    [HttpPost]
    public async Task<IActionResult> Consolidate(int? projectId, CancellationToken ct)
        => Json(await _consolidation.ConsolidateProjectAsync(projectId, ct));
}
