using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

// Phase 1 surface for workspace management. Creating workspaces/snapshots and recording proposed
// changes works now; applying changes to disk is enabled in Phase 2.
public class WorkspacesController : Controller
{
    private readonly AppDbContext _db;
    private readonly IWorkspaceManager _manager;
    private readonly IWorkspaceSnapshotService _snapshots;
    private readonly IWorkspaceModificationService _changes;
    private readonly IDiffService _diff;

    public WorkspacesController(AppDbContext db, IWorkspaceManager manager,
        IWorkspaceSnapshotService snapshots, IWorkspaceModificationService changes, IDiffService diff)
    {
        _db = db; _manager = manager; _snapshots = snapshots; _changes = changes; _diff = diff;
    }

    public async Task<IActionResult> Index(int? projectId, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId;
        var list = await _manager.ListAsync(projectId, ct);
        return View(list);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int projectId, string? name, string? description, bool original, CancellationToken ct)
    {
        try { await _manager.CreateWorkspaceAsync(projectId, name, description, original, ct); TempData["Message"] = "Workspace created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index), new { projectId });
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var ws = await _manager.GetAsync(id, ct);
        if (ws is null) return RedirectToAction(nameof(Index));
        ViewBag.Files = await _manager.ListFilesAsync(id, ct);
        ViewBag.Snapshots = await _snapshots.ListAsync(id, ct);
        ViewBag.Changes = await _changes.ListChangesAsync(id, ct);
        return View(ws);
    }

    public async Task<IActionResult> Snapshots(int? workspaceId, CancellationToken ct)
    {
        ViewBag.Workspaces = await _manager.ListAsync(null, ct);
        ViewBag.WorkspaceId = workspaceId;
        if (workspaceId is int wid)
            ViewBag.Snapshots = await _snapshots.ListAsync(wid, ct);
        else
            ViewBag.Snapshots = await _db.WorkspaceSnapshots.AsNoTracking().OrderByDescending(s => s.Id).Take(200).ToListAsync(ct);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateSnapshot(int workspaceId, string? description, CancellationToken ct)
    {
        try { await _snapshots.CreateSnapshotAsync(workspaceId, description, "local", ct); TempData["Message"] = "Snapshot created."; }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Snapshots), new { workspaceId });
    }

    public async Task<IActionResult> Changes(int? workspaceId, CancellationToken ct)
    {
        ViewBag.Workspaces = await _manager.ListAsync(null, ct);
        ViewBag.WorkspaceId = workspaceId;
        if (workspaceId is int wid)
            ViewBag.Changes = await _changes.ListChangesAsync(wid, ct);
        else
            ViewBag.Changes = await _db.WorkspaceChanges.AsNoTracking().OrderByDescending(c => c.Id).Take(200).ToListAsync(ct);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ProposeChange(int workspaceId, string relativePath, string? newContent, string? reason, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath)) { TempData["Error"] = "A relative path is required."; return RedirectToAction(nameof(Changes), new { workspaceId }); }
        try
        {
            await _changes.ProposeChangeAsync(workspaceId, relativePath, newContent, reason, null, "manual", ct);
            TempData["Message"] = "Change proposed (Phase 1 records the proposal; applying to disk is a Phase 2 feature).";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Changes), new { workspaceId });
    }

    public async Task<IActionResult> Diff(int changeId, CancellationToken ct)
    {
        var change = await _changes.GetChangeAsync(changeId, ct);
        if (change is null) return RedirectToAction(nameof(Changes));
        ViewBag.Change = change;
        ViewBag.Diff = _diff.Diff(change.PreviousContent, change.NewContent);
        return View();
    }
}
