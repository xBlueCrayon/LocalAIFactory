using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

// R2-P0A: the Coverage / Gap Report screen — "Can I trust this analysis?" and "What did LocalAIFactory not
// understand?" Reads the latest persisted report for a project; computes one on-demand if none exists yet.
public sealed class CoverageController : SecuredController
{
    private readonly AppDbContext _db;
    private readonly IImportCoverageService _coverage;
    public CoverageController(AppDbContext db, IImportCoverageService coverage,
        ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _db = db; _coverage = coverage; }

    public async Task<IActionResult> Index(int projectId, CancellationToken ct)
    {
        if (await RequireProjectAsync(projectId, "view coverage report", ct) is { } denied) return denied;
        await AuditAsync(LocalAIFactory.Core.Enums.AuditEventType.CoverageViewed, "Viewed coverage report", "Project", projectId.ToString(), projectId, ct: ct);
        var project = await _db.Projects.FirstOrDefaultAsync(p => p.Id == projectId, ct);
        var report = await _coverage.LatestForProjectAsync(projectId, ct);
        if (report is null && project is not null && await _db.ImportedFiles.AnyAsync(f => f.ProjectId == projectId, ct))
            report = await _coverage.ComputeAsync(projectId, null, ct); // first-time compute for older imports

        ViewBag.ProjectName = project?.Name ?? $"Project {projectId}";
        ViewBag.ProjectId = projectId;
        return View(BuildVm(report));
    }

    private static CoverageVm BuildVm(ImportCoverageReport? r)
    {
        if (r is null) return new CoverageVm(null, new(), new(), new(), new());
        List<T> D<T>(string j) { try { return JsonSerializer.Deserialize<List<T>>(j) ?? new(); } catch { return new(); } }
        return new CoverageVm(r,
            D<LanguageCoverage>(r.LanguageBreakdownJson),
            D<SkipReasonCount>(r.SkipReasonsJson),
            D<ParseErrorItem>(r.ParseErrorsJson),
            D<ConfidenceBand>(r.ConfidenceJson));
    }

    public record CoverageVm(ImportCoverageReport? Report, List<LanguageCoverage> Languages,
        List<SkipReasonCount> SkipReasons, List<ParseErrorItem> ParseErrors, List<ConfidenceBand> Confidence);
}
