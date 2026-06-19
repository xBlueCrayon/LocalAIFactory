using LocalAIFactory.Core.Entities;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class ProjectsController : Controller
{
    private readonly AppDbContext _db;
    public ProjectsController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken ct)
        => View(await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct));

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string code, string? description, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(code))
        {
            TempData["Error"] = "Name and code are required.";
            return RedirectToAction(nameof(Index));
        }
        if (await _db.Projects.AnyAsync(p => p.Code == code, ct))
        {
            TempData["Error"] = "A project with that code already exists.";
            return RedirectToAction(nameof(Index));
        }
        _db.Projects.Add(new Project { Name = name, Code = code, Description = description });
        await _db.SaveChangesAsync(ct);
        TempData["Message"] = "Project created.";
        return RedirectToAction(nameof(Index));
    }
}
