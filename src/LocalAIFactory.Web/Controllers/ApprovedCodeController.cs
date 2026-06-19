using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class ApprovedCodeController : Controller
{
    private readonly AppDbContext _db;
    public ApprovedCodeController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(int? projectId, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId;
        var q = _db.ApprovedCodeSnippets.AsNoTracking().AsQueryable();
        if (projectId is int pid) q = q.Where(s => s.ProjectId == pid);
        return View(await q.OrderByDescending(s => s.UpdatedUtc).Take(300).ToListAsync(ct));
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var snip = await _db.ApprovedCodeSnippets.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
        if (snip is null) return RedirectToAction(nameof(Index));
        return View(snip);
    }
}
