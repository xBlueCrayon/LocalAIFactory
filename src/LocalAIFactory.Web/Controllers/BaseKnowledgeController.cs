using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.ViewModels;
using LocalAIFactory.Data;
using LocalAIFactory.Web.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

// R2-ACC-B1: Base Knowledge area. Baseline professional knowledge (shipped via a Knowledge Pack) is shown
// distinctly from imported project knowledge — every item here has a non-null KnowledgePackId. Viewing is
// open to authenticated users (same posture as the Knowledge page); INSTALLING a pack is Admin-only and
// audited (server-side enforcement via SecuredController). MSSQL-only; never blocks on external services.
public sealed class BaseKnowledgeController : SecuredController
{
    private readonly AppDbContext _db;
    private readonly IKnowledgePackInstaller _installer;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;

    public BaseKnowledgeController(AppDbContext db, IKnowledgePackInstaller installer, IConfiguration config,
        IWebHostEnvironment env, ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _db = db; _installer = installer; _config = config; _env = env; }

    public async Task<IActionResult> Index(string? q, string? category, KnowledgeScope? scope, CancellationToken ct)
    {
        ViewBag.Packs = await _db.KnowledgePacks.AsNoTracking()
            .OrderByDescending(p => p.InstalledUtc)
            .Select(p => new InstalledPackRow(p.Id, p.Uid, p.Name, p.Version, p.ItemCount, p.InstalledUtc, p.Status))
            .ToListAsync(ct);
        ViewBag.Categories = await _db.Tags.AsNoTracking()
            .Where(t => t.Name.StartsWith("cat:")).Select(t => t.Name.Substring(4)).OrderBy(x => x).ToListAsync(ct);
        ViewBag.Query = q; ViewBag.Category = category; ViewBag.Scope = scope;

        var query = _db.KnowledgeItems.AsNoTracking().Where(k => k.KnowledgePackId != null);
        if (!string.IsNullOrWhiteSpace(q))
        {
            // Multi-term AND: every whitespace-separated token must appear in the title or body, so a phrase
            // like "backup restore" matches an item that contains both words (order-independent).
            foreach (var token in q.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var t = token;
                query = query.Where(k => k.Title.Contains(t) || k.Content.Contains(t));
            }
        }
        if (!string.IsNullOrWhiteSpace(category))
        {
            var catTag = "cat:" + category.Trim();
            query = query.Where(k => k.Tags.Any(t => t.Tag!.Name == catTag));
        }
        if (scope is KnowledgeScope s) query = query.Where(k => k.Scope == s);

        // Project to a lightweight row — the (large) Content column is filtered on but never SELECTed.
        var items = await query
            .OrderBy(k => k.Title)
            .Take(400)
            .Select(k => new BaseKnowledgeRow(
                k.Id, k.Title,
                k.Tags.Where(t => t.Tag!.Name.StartsWith("cat:")).Select(t => t.Tag!.Name.Substring(4)).FirstOrDefault(),
                k.KnowledgeType, k.Scope, k.Confidence, k.Status, k.LastReviewedUtc))
            .ToListAsync(ct);
        return View(items);
    }

    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await _db.KnowledgeItems.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id && k.KnowledgePackId != null, ct);
        if (item is null) return RedirectToAction(nameof(Index));
        ViewBag.Pack = await _db.KnowledgePacks.AsNoTracking().FirstOrDefaultAsync(p => p.Id == item.KnowledgePackId, ct);
        ViewBag.Tags = await _db.KnowledgeItemTags.AsNoTracking()
            .Where(t => t.KnowledgeItemId == id).Select(t => t.Tag!.Name).ToListAsync(ct);
        ViewBag.Provenance = await _db.ProvenanceEvents.AsNoTracking()
            .Where(p => p.KnowledgeItemId == id).OrderByDescending(p => p.Id)
            .Select(p => new ProvenanceRow(p.Method, p.Actor, p.Reason, p.OriginInstanceId, p.CreatedUtc))
            .Take(20).ToListAsync(ct);
        return View(item);
    }

    // Admin-only: (re)install the Professional Base Knowledge Pack from disk. Idempotent; never overwrites
    // user-edited baseline items (a proposed revision is raised instead). Server-side enforced + audited.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Install(CancellationToken ct)
    {
        if (await RequireAdminAsync("install knowledge pack", ct) is { } denied) return denied;
        var path = KnowledgePackLocator.FindBaseV1(_config, _env.ContentRootPath);
        if (path is null) { TempData["Error"] = "Base knowledge pack files were not found on disk."; return RedirectToAction(nameof(Index)); }

        var res = await _installer.InstallAsync(path, CurrentUser?.WindowsIdentity ?? "admin", ct);
        if (res.Success)
        {
            await AuditAsync(AuditEventType.KnowledgePackInstalled,
                $"Installed {res.Name} v{res.Version}", "KnowledgePack", res.PackUid.ToString(),
                detail: $"created={res.Created} updated={res.Updated} unchanged={res.Unchanged} proposed={res.ProposedRevisions} current={res.AlreadyCurrent}", ct: ct);
            TempData["Message"] = res.AlreadyCurrent
                ? $"{res.Name} v{res.Version} already current ({res.TotalItems} items)."
                : $"{res.Name} v{res.Version} installed: {res.Created} new, {res.Updated} updated, {res.Unchanged} unchanged, {res.ProposedRevisions} proposed for review.";
        }
        else TempData["Error"] = "Knowledge pack install failed: " + (res.Errors.FirstOrDefault() ?? "unknown error");
        return RedirectToAction(nameof(Index));
    }
}
