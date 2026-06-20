using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class BusinessRulesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IApprovalService _approval;

    public BusinessRulesController(AppDbContext db, IApprovalService approval)
    {
        _db = db; _approval = approval;
    }

    public async Task<IActionResult> Index(int? projectId, BusinessRuleStatus? status, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId; ViewBag.Status = status;
        var q = _db.BusinessRules.AsNoTracking().AsQueryable();
        if (projectId is int pid) q = q.Where(r => r.ProjectId == pid);
        if (status is BusinessRuleStatus s) q = q.Where(r => r.Status == s);
        return View(await q.OrderByDescending(r => r.UpdatedUtc).Take(300).ToListAsync(ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? projectId, string title, string content, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Title and content are required.";
            return RedirectToAction(nameof(Index));
        }
        _db.BusinessRules.Add(new BusinessRule
        {
            ProjectId = projectId, Title = title, Content = content,
            Status = BusinessRuleStatus.NeedsReview, IsApproved = false,
            Tier = PermanenceTier.Curated // human-authored.
        });
        await _db.SaveChangesAsync(ct);
        TempData["Message"] = "Business rule added (needs review).";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        await _approval.ApproveBusinessRuleAsync(id, ct);
        TempData["Message"] = "Business rule approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(string action, int[] ids, int? projectId, BusinessRuleStatus? status, CancellationToken ct)
    {
        int n = action switch
        {
            "approve" => await _approval.BulkApproveAsync("rule", ids, ct),
            "deprecate" => await _approval.BulkDeprecateAsync("rule", ids, ct),
            "delete" => await _approval.BulkDeleteAsync("rule", ids, ct),
            _ => 0
        };
        TempData["Message"] = $"Bulk {action}: {n} rule(s).";
        return RedirectToAction(nameof(Index), new { projectId, status });
    }
}
