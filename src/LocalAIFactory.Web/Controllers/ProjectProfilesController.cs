using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class ProjectProfilesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IApprovalService _approval;
    private readonly IProjectProfileService _profiles;

    public ProjectProfilesController(AppDbContext db, IApprovalService approval, IProjectProfileService profiles)
    {
        _db = db; _approval = approval; _profiles = profiles;
    }

    public async Task<IActionResult> Index(int? projectId, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId;
        ViewBag.ProjectNames = await _db.Projects.ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var profiles = await _db.ProjectProfiles.AsNoTracking()
            .Where(p => projectId == null || p.ProjectId == projectId)
            .OrderByDescending(p => p.UpdatedUtc).ToListAsync(ct);

        var ids = profiles.Select(p => p.Id).ToList();
        var counts = await _db.ProjectProfileSections.AsNoTracking()
            .Where(s => ids.Contains(s.ProjectProfileId))
            .GroupBy(s => s.ProjectProfileId)
            .Select(g => new { Id = g.Key, Total = g.Count(), Approved = g.Count(x => x.Status == KnowledgeStatus.Approved) })
            .ToListAsync(ct);
        ViewBag.SectionTotals = counts.ToDictionary(c => c.Id, c => c.Total);
        ViewBag.SectionApproved = counts.ToDictionary(c => c.Id, c => c.Approved);
        return View(profiles);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var profile = await _db.ProjectProfiles.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null) return RedirectToAction(nameof(Index));
        ViewBag.ProjectName = await _db.Projects.Where(p => p.Id == profile.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct);
        ViewBag.Sections = await _db.ProjectProfileSections.AsNoTracking()
            .Where(s => s.ProjectProfileId == id)
            .OrderBy(s => s.OrderIndex).ToListAsync(ct);
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveSection(int sectionId, int profileId, CancellationToken ct)
    {
        await _approval.ApproveProjectProfileSectionAsync(sectionId, ct);
        TempData["Message"] = "Section approved.";
        return RedirectToAction(nameof(Details), new { id = profileId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(string action, int[] ids, int profileId, CancellationToken ct)
    {
        int n = action switch
        {
            "approve" => await _approval.BulkApproveAsync("section", ids, ct),
            "deprecate" => await _approval.BulkDeprecateAsync("section", ids, ct),
            "delete" => await _approval.BulkDeleteAsync("section", ids, ct),
            _ => 0
        };
        TempData["Message"] = $"Bulk {action}: {n} section(s).";
        return RedirectToAction(nameof(Details), new { id = profileId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(int projectId, CancellationToken ct)
    {
        try
        {
            await _profiles.GenerateAsync(projectId, null, ct);
            TempData["Message"] = "Profile generation completed (sections need review).";
        }
        catch (Exception ex) { TempData["Error"] = "Profile generation failed: " + ex.Message; }
        return RedirectToAction(nameof(Index), new { projectId });
    }
}
