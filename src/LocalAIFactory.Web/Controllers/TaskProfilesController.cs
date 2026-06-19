using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class TaskProfilesController : Controller
{
    private readonly AppDbContext _db;
    public TaskProfilesController(AppDbContext db) => _db = db;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewBag.Models = await _db.ModelConfigurations.ToDictionaryAsync(m => m.Id, m => m.Name, ct);
        return View(await _db.TaskProfiles.OrderBy(p => p.TaskType).ToListAsync(ct));
    }

    public async Task<IActionResult> Edit(int id, CancellationToken ct)
    {
        var profile = await _db.TaskProfiles.FirstOrDefaultAsync(p => p.Id == id, ct);
        if (profile is null) return RedirectToAction(nameof(Index));
        ViewBag.Models = await _db.ModelConfigurations.OrderBy(m => m.Name).ToListAsync(ct);
        return View(profile);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, int? primaryModelId, bool validationEnabled, int? validationModelId,
        bool comparisonEnabled, int? comparisonModelId, bool useKnowledgeBase, bool useProjectMemory, bool useKnowledgeGraph,
        double temperature, int maxTokens, bool localOnly, bool requireApprovalBeforeCloudUse, bool isEnabled, CancellationToken ct)
    {
        var p = await _db.TaskProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return RedirectToAction(nameof(Index));
        p.PrimaryModelId = primaryModelId;
        p.ValidationEnabled = validationEnabled; p.ValidationModelId = validationModelId;
        p.ComparisonEnabled = comparisonEnabled; p.ComparisonModelId = comparisonModelId;
        p.UseKnowledgeBase = useKnowledgeBase; p.UseProjectMemory = useProjectMemory; p.UseKnowledgeGraph = useKnowledgeGraph;
        p.Temperature = temperature; p.MaxTokens = maxTokens <= 0 ? 2048 : maxTokens;
        p.LocalOnly = localOnly; p.RequireApprovalBeforeCloudUse = requireApprovalBeforeCloudUse; p.IsEnabled = isEnabled;
        p.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        TempData["Message"] = "Task profile updated.";
        return RedirectToAction(nameof(Index));
    }
}
