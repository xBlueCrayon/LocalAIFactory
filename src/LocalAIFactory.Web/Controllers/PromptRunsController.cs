using LocalAIFactory.Core.Entities;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class PromptRunsController : Controller
{
    private readonly AppDbContext _db;
    public PromptRunsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? projectId, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId;
        ViewBag.ProjectNames = await _db.Projects.ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var runs = await _db.PromptRuns.AsNoTracking()
            .Where(r => projectId == null || r.ProjectId == projectId)
            .OrderByDescending(r => r.Id).Take(200).ToListAsync(ct);

        var ids = runs.Select(r => r.Id).ToList();
        var outCounts = await _db.ModelOutputs.AsNoTracking()
            .Where(o => ids.Contains(o.PromptRunId))
            .GroupBy(o => o.PromptRunId)
            .Select(g => new { Id = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        ViewBag.OutputCounts = outCounts.ToDictionary(c => c.Id, c => c.Count);
        return View(runs);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var run = await _db.PromptRuns.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id, ct);
        if (run is null) return RedirectToAction(nameof(Index));

        var outputs = await _db.ModelOutputs.AsNoTracking()
            .Where(o => o.PromptRunId == id)
            .OrderBy(o => o.Kind).ThenBy(o => o.Id).ToListAsync(ct);
        ViewBag.Outputs = outputs;

        var modelIds = outputs.Where(o => o.ModelConfigurationId != null).Select(o => o.ModelConfigurationId!.Value).Distinct().ToList();
        ViewBag.ModelNames = await _db.ModelConfigurations.AsNoTracking()
            .Where(m => modelIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.Name, ct);
        ViewBag.ProjectName = await _db.Projects.Where(p => p.Id == run.ProjectId).Select(p => p.Name).FirstOrDefaultAsync(ct);
        return View(run);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApproveOutput(int outputId, int runId, CancellationToken ct)
    {
        var o = await _db.ModelOutputs.FirstOrDefaultAsync(x => x.Id == outputId, ct);
        if (o != null) { o.IsApproved = true; await _db.SaveChangesAsync(ct); TempData["Message"] = "Output marked approved."; }
        return RedirectToAction(nameof(Details), new { id = runId });
    }
}
