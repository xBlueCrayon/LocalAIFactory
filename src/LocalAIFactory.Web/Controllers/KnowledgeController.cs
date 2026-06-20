using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using LocalAIFactory.Core.ViewModels;
namespace LocalAIFactory.Web.Controllers;

public class KnowledgeController : Controller
{
    private readonly AppDbContext _db;
    private readonly IApprovalService _approval;
    private readonly IChunkingService _chunking;
    private readonly IKnowledgeIndexer _indexer;
    private readonly IKnowledgeBackboneService _backbone;
    private readonly RagOptions _rag;

    public KnowledgeController(AppDbContext db, IApprovalService approval, IChunkingService chunking, IKnowledgeIndexer indexer, IKnowledgeBackboneService backbone, IOptions<RagOptions> rag)
    {
        _db = db; _approval = approval; _chunking = chunking; _indexer = indexer; _backbone = backbone; _rag = rag.Value;
    }

    public async Task<IActionResult> Index(int? projectId, KnowledgeStatus? status, string? q, CancellationToken ct)
    {
        ViewBag.Projects = await _db.Projects.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.ProjectId = projectId; ViewBag.Status = status; ViewBag.Query = q;
        var query = _db.KnowledgeItems.AsNoTracking().AsQueryable();
        if (projectId is int pid) query = query.Where(k => k.ProjectId == pid);
        if (status is KnowledgeStatus s) query = query.Where(k => k.Status == s);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(k => k.Title.Contains(term) || k.Content.Contains(term));
        }
        // Project to a lightweight row: the large Content column is filtered on (server-side) but never
        // SELECTed, so the list never materializes file-sized strings into memory.
        var items = await query
            .OrderByDescending(k => k.UpdatedUtc)
            .Take(200)
            .Select(k => new KnowledgeListRow(k.Id, k.Title, k.SourceType, k.Status, k.UpdatedUtc, k.IsApproved))
            .ToListAsync(ct);
        return View(items);
    }

    // Approval history for a single item (from the audit log), shown on the details page.
    public async Task<IActionResult> Details(int id, CancellationToken ct)
    {
        var item = await _db.KnowledgeItems.AsNoTracking().FirstOrDefaultAsync(k => k.Id == id, ct);
        if (item is null) return RedirectToAction(nameof(Index));
        ViewBag.History = await _db.AuditLogs.AsNoTracking()
            .Where(a => a.EntityName == "KnowledgeItem" && a.EntityId == id.ToString())
            .OrderByDescending(a => a.Id).Take(20).ToListAsync(ct);
        // KE-003: read-only version history + provenance (projected; no large snapshot columns).
        ViewBag.Versions = await _db.KnowledgeVersions.AsNoTracking()
            .Where(v => v.KnowledgeItemId == id)
            .OrderByDescending(v => v.VersionNumber)
            .Select(v => new KnowledgeVersionRow(v.VersionNumber, v.ChangeReason, v.Method, v.Actor, v.CreatedUtc))
            .Take(50).ToListAsync(ct);
        ViewBag.Provenance = await _db.ProvenanceEvents.AsNoTracking()
            .Where(p => p.KnowledgeItemId == id)
            .OrderByDescending(p => p.Id)
            .Select(p => new ProvenanceRow(p.Method, p.Actor, p.Reason, p.OriginInstanceId, p.CreatedUtc))
            .Take(50).ToListAsync(ct);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(int? projectId, string title, string content, SourceType sourceType, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            TempData["Error"] = "Title and content are required.";
            return RedirectToAction(nameof(Index));
        }
        var ki = new KnowledgeItem
        {
            ProjectId = projectId, Title = title, Content = content,
            SourceType = sourceType, Status = KnowledgeStatus.Draft, Confidence = 0.6,
            Tier = PermanenceTier.Curated // human-authored, so human-anchored even before approval.
        };
        _db.KnowledgeItems.Add(ki);
        await _db.SaveChangesAsync(ct);
        await _backbone.RecordInitialAsync(ki, ProvenanceMethod.Human, User?.Identity?.Name ?? "user",
            "Created via Knowledge UI", ct: ct);
        int idx = 0;
        foreach (var chunk in _chunking.Chunk(content, _rag.MaxChunkChars, _rag.ChunkOverlap))
            _db.KnowledgeChunks.Add(new KnowledgeChunk { KnowledgeItemId = ki.Id, ChunkIndex = idx++, Content = chunk, TokenCount = _chunking.EstimateTokens(chunk) });
        await _db.SaveChangesAsync(ct);
        try { await _indexer.IndexKnowledgeItemAsync(ki.Id, ct); } catch { }
        TempData["Message"] = "Knowledge item created.";
        return RedirectToAction(nameof(Index));
    }

    // KE-003: human edit records a new version (hash-guarded) plus a provenance event.
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, string title, string content, string? reason, CancellationToken ct)
    {
        var item = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.Id == id, ct);
        if (item is null) return RedirectToAction(nameof(Index));
        if (!string.IsNullOrWhiteSpace(title)) item.Title = title.Trim();
        item.Content = content ?? "";
        item.Tier = PermanenceTier.Curated; // KE-002: a human edit makes the item human-anchored.
        item.UpdatedUtc = DateTime.UtcNow;
        await _backbone.RecordEditAsync(item,
            string.IsNullOrWhiteSpace(reason) ? "Edited via Knowledge UI" : reason!.Trim(),
            ProvenanceMethod.Human, User?.Identity?.Name ?? "user", ct);
        // Re-chunk and re-index so retrieval reflects the new content.
        var oldChunks = await _db.KnowledgeChunks.Where(c => c.KnowledgeItemId == id).ToListAsync(ct);
        _db.KnowledgeChunks.RemoveRange(oldChunks);
        int idx = 0;
        foreach (var chunk in _chunking.Chunk(item.Content, _rag.MaxChunkChars, _rag.ChunkOverlap))
            _db.KnowledgeChunks.Add(new KnowledgeChunk { KnowledgeItemId = id, ChunkIndex = idx++, Content = chunk, TokenCount = _chunking.EstimateTokens(chunk) });
        await _db.SaveChangesAsync(ct);
        try { await _indexer.IndexKnowledgeItemAsync(id, ct); } catch { }
        TempData["Message"] = "Knowledge item updated (new version recorded).";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        await _approval.ApproveKnowledgeItemAsync(id, ct);
        TempData["Message"] = "Approved.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Deprecate(int id, CancellationToken ct)
    {
        await _approval.DeprecateKnowledgeItemAsync(id, ct);
        TempData["Message"] = "Deprecated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _approval.DeleteKnowledgeItemAsync(id, ct);
        TempData["Message"] = "Deleted.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Bulk(string action, int[] ids, int? projectId, KnowledgeStatus? status, CancellationToken ct)
    {
        int n = action switch
        {
            "approve" => await _approval.BulkApproveAsync("knowledge", ids, ct),
            "deprecate" => await _approval.BulkDeprecateAsync("knowledge", ids, ct),
            "delete" => await _approval.BulkDeleteAsync("knowledge", ids, ct),
            _ => 0
        };
        TempData["Message"] = $"Bulk {action}: {n} item(s).";
        return RedirectToAction(nameof(Index), new { projectId, status });
    }
}
