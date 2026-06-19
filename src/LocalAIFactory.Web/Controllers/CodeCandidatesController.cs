using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class CodeCandidatesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IApprovalService _approval;

    public CodeCandidatesController(AppDbContext db, IApprovalService approval)
    {
        _db = db; _approval = approval;
    }

    public async Task<IActionResult> Index(int? projectId, KnowledgeStatus? status, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId;
        ViewBag.Status = status;
        ViewBag.ProjectNames = await _db.Projects.ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var q = _db.ExtractedCodeBlocks.AsNoTracking().AsQueryable();
        if (projectId is int pid) q = q.Where(b => b.ProjectId == pid);
        if (status is KnowledgeStatus s) q = q.Where(b => b.Status == s);
        else q = q.Where(b => b.Status != KnowledgeStatus.Approved); // default: outstanding candidates
        var items = await q.OrderByDescending(b => b.Id).Take(300).ToListAsync(ct);
        return View(items);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Promote(int id, string? title, int? projectId, CancellationToken ct)
    {
        var newId = await _approval.PromoteCodeBlockAsync(id, title ?? "", ct);
        TempData["Message"] = newId > 0 ? "Promoted to approved code." : "Could not promote candidate.";
        return RedirectToAction(nameof(Index), new { projectId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(string action, int[] ids, int? projectId, CancellationToken ct)
    {
        int n = action switch
        {
            "promote" => await _approval.BulkPromoteCodeBlocksAsync(ids, ct),
            "approve" => await _approval.BulkPromoteCodeBlocksAsync(ids, ct), // approve == promote for candidates
            "deprecate" => await _approval.BulkDeprecateAsync("code", ids, ct),
            "delete" => await _approval.BulkDeleteAsync("code", ids, ct),
            _ => 0
        };
        TempData["Message"] = $"Bulk {action}: {n} candidate(s).";
        return RedirectToAction(nameof(Index), new { projectId });
    }
}
