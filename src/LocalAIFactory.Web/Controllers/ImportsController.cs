using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

public class ImportsController : SecuredController
{
    private readonly AppDbContext _db;
    private readonly IFileImportService _fileImport;
    private readonly IChatGptImportService _chatImport;

    public ImportsController(AppDbContext db, IFileImportService fileImport, IChatGptImportService chatImport,
        ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _db = db; _fileImport = fileImport; _chatImport = chatImport; }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (await RequireAdminAsync("view imports", ct) is { } denied) return denied;
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.Files = await _db.ImportedFiles.AsNoTracking().OrderByDescending(f => f.Id).Take(100).ToListAsync(ct);
        ViewBag.Conversations = await _db.ImportedConversations.AsNoTracking().OrderByDescending(c => c.Id).Take(100).ToListAsync(ct);
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(1_073_741_824)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
    public async Task<IActionResult> UploadFile(int? projectId, IFormFile? file, CancellationToken ct)
    {
        if (await RequireAdminAsync("import file", ct) is { } denied) return denied;
        if (file is null || file.Length == 0) { TempData["Error"] = "Choose a file."; return RedirectToAction(nameof(Index)); }
        await AuditAsync(LocalAIFactory.Core.Enums.AuditEventType.ImportStarted, "File import started", "File", file.FileName, projectId, ct: ct);
        var bytes = await ToBytesAsync(file, ct);
        try
        {
            await _fileImport.ImportAsync(projectId, file.FileName, bytes, ct);
            await AuditAsync(LocalAIFactory.Core.Enums.AuditEventType.ImportCompleted, "File import completed", "File", file.FileName, projectId, ct: ct);
            TempData["Message"] = "File imported (needs review).";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(1_073_741_824)]
    [RequestFormLimits(MultipartBodyLengthLimit = 1_073_741_824)]
    public async Task<IActionResult> UploadConversation(int? projectId, IFormFile? file, CancellationToken ct)
    {
        if (await RequireAdminAsync("import conversation", ct) is { } denied) return denied;
        if (file is null || file.Length == 0) { TempData["Error"] = "Choose an export file."; return RedirectToAction(nameof(Index)); }
        var bytes = await ToBytesAsync(file, ct);
        try
        {
            var convos = await _chatImport.ImportAsync(projectId, file.FileName, bytes, ct);
            TempData["Message"] = $"Imported {convos.Count} conversation(s) (needs review).";
        }
        catch (Exception ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Index));
    }

    private static async Task<byte[]> ToBytesAsync(IFormFile file, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await file.CopyToAsync(ms, ct);
        return ms.ToArray();
    }
}
