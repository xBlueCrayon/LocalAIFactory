using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

// KE-012: on-demand structural consolidation. R2-P0B: Admin-only and audited — it re-extracts and mutates the
// derived structural layer for a project.
public sealed class MaintenanceController : SecuredController
{
    private readonly IStructuralConsolidationService _consolidation;

    public MaintenanceController(IStructuralConsolidationService consolidation,
        ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _consolidation = consolidation; }

    // POST /Maintenance/Consolidate?projectId=1   (Admin-only)
    [HttpPost]
    public async Task<IActionResult> Consolidate(int? projectId, CancellationToken ct)
    {
        if (await RequireAdminAsync("consolidate", ct) is { } denied) return denied;
        await AuditAsync(AuditEventType.ConsolidationStarted, "Consolidation started", "Project", projectId?.ToString(), projectId, ct: ct);
        var result = await _consolidation.ConsolidateProjectAsync(projectId, ct);
        await AuditAsync(AuditEventType.ConsolidationCompleted, "Consolidation completed", "Project", projectId?.ToString(), projectId,
            detail: $"symbols={result.LiveSymbols} edges={result.Edges} orphansRemoved={result.OrphanSymbolsRemoved}", ct: ct);
        return Json(result);
    }
}
