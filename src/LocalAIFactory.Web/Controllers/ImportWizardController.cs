using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class ImportWizardController : SecuredController
{
    private readonly AppDbContext _db;
    private readonly IProjectIngestionService _ingestion;
    private readonly IIngestionQueue _queue;
    private readonly IServiceHealthCache _health;

    public ImportWizardController(AppDbContext db, IProjectIngestionService ingestion, IIngestionQueue queue, IServiceHealthCache health,
        ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _db = db; _ingestion = ingestion; _queue = queue; _health = health; }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (await RequireAdminAsync("import project", ct) is { } denied) return denied;
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.Jobs = await _db.IngestionJobs.AsNoTracking().OrderByDescending(j => j.Id).Take(20).ToListAsync(ct);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(1_073_741_824)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
    public async Task<IActionResult> Start(int? projectId, IFormFile? zip, CancellationToken ct)
    {
        if (await RequireAdminAsync("start project import", ct) is { } denied) return denied;
        if (zip is null || zip.Length == 0) { TempData["Error"] = "Choose a .zip file."; return RedirectToAction(nameof(Index)); }
        using var ms = new MemoryStream();
        await zip.CopyToAsync(ms, ct);
        var jobId = await _ingestion.CreateZipJobAsync(projectId, zip.FileName, ms.ToArray(), ct);
        await _queue.EnqueueAsync(jobId, ct);
        await AuditAsync(LocalAIFactory.Core.Enums.AuditEventType.ImportStarted, "Project import started", "Zip", zip.FileName, projectId, ct: ct);
        TempData["Message"] = "Import started. This page refreshes as it progresses.";
        return RedirectToAction(nameof(Status), new { jobId });
    }

    public async Task<IActionResult> Status(int jobId, CancellationToken ct)
    {
        var job = await _db.IngestionJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        if (job is null) return RedirectToAction(nameof(Index));

        // Phase 1.2: a clear outcome breakdown — imported, embedded/indexed, and skipped (with reasons).
        ViewBag.ImportedCount = await _db.ImportedFiles.AsNoTracking()
            .CountAsync(f => f.IngestionJobId == jobId && !f.Skipped, ct);
        var skips = await _db.ImportedFiles.AsNoTracking()
            .Where(f => f.IngestionJobId == jobId && f.Skipped)
            .GroupBy(f => f.SkipReason)
            .Select(g => new { Reason = g.Key, Count = g.Count() })
            .ToListAsync(ct);
        ViewBag.SkipReasons = skips.ToDictionary(s => s.Reason ?? "other", s => s.Count);
        ViewBag.HealthMode = _health.Current.ModeLabel;
        ViewBag.EmbeddingsState = _health.Current.Embeddings.ToString();
        return View(job);
    }

    [HttpGet]
    public async Task<IActionResult> Progress(int jobId, CancellationToken ct)
    {
        var p = await _ingestion.GetProgressAsync(jobId, ct);
        if (p is null) return NotFound();
        return Json(new
        {
            status = p.Status.ToString(),
            phase = p.Phase.ToString(),
            total = p.TotalFiles,
            processed = p.ProcessedFiles,
            skipped = p.SkippedFiles,
            chunks = p.ChunkCount,
            embedded = p.EmbeddedCount,
            error = p.Error
        });
    }
}
