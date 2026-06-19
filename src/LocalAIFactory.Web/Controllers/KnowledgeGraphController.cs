using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class KnowledgeGraphController : Controller
{
    private readonly AppDbContext _db;
    private readonly IApprovalService _approval;
    public KnowledgeGraphController(AppDbContext db, IApprovalService approval) { _db = db; _approval = approval; }

    public async Task<IActionResult> Index(int? projectId, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId;
        var entities = await _db.KnowledgeEntities.AsNoTracking()
            .Where(e => projectId == null || e.ProjectId == projectId)
            .OrderByDescending(e => e.Id).Take(200).ToListAsync(ct);
        var rels = await _db.KnowledgeRelationships.AsNoTracking()
            .Where(r => projectId == null || r.ProjectId == projectId)
            .OrderByDescending(r => r.Id).Take(300).ToListAsync(ct);
        ViewBag.EntityNames = entities.ToDictionary(e => e.Id, e => e.Name);
        ViewBag.Relationships = rels;
        return View(entities);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveEntity(int id, int? projectId, CancellationToken ct)
    {
        var e = await _db.KnowledgeEntities.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e != null) { e.Status = KnowledgeStatus.Approved; await _db.SaveChangesAsync(ct); }
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveRelationship(int id, int? projectId, CancellationToken ct)
    {
        var r = await _db.KnowledgeRelationships.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r != null) { r.Status = KnowledgeStatus.Approved; await _db.SaveChangesAsync(ct); }
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkEntities(string action, int[] ids, int? projectId, CancellationToken ct)
    {
        int n = action switch
        {
            "approve" => await _approval.BulkApproveAsync("entity", ids, ct),
            "deprecate" => await _approval.BulkDeprecateAsync("entity", ids, ct),
            "delete" => await _approval.BulkDeleteAsync("entity", ids, ct),
            _ => 0
        };
        TempData["Message"] = $"Bulk {action}: {n} entity(ies).";
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> BulkRelationships(string action, int[] ids, int? projectId, CancellationToken ct)
    {
        int n = action switch
        {
            "approve" => await _approval.BulkApproveAsync("relationship", ids, ct),
            "deprecate" => await _approval.BulkDeprecateAsync("relationship", ids, ct),
            "delete" => await _approval.BulkDeleteAsync("relationship", ids, ct),
            _ => 0
        };
        TempData["Message"] = $"Bulk {action}: {n} relationship(s).";
        return RedirectToAction(nameof(Index), new { projectId });
    }
}
