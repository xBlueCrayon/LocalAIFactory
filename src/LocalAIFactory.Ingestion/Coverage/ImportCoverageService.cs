using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Ingestion.Coverage;

// R2-P0A: derives the honest coverage / gap report for a project from the immutable artifacts + the derived
// structural tables. Append-only (each compute adds a new ImportCoverageReport). The point: classify every
// file into a visible, explainable bucket — extracted, no-symbols, unsupported, parse-error, skipped, or
// non-code — so a user can always tell what the platform did NOT understand.
public sealed class ImportCoverageService : IImportCoverageService
{
    private readonly AppDbContext _db;
    private readonly ICodeGraphBuilder _graph;

    public ImportCoverageService(AppDbContext db, ICodeGraphBuilder graph) { _db = db; _graph = graph; }

    public async Task<ImportCoverageReport?> LatestForProjectAsync(int? projectId, CancellationToken ct = default)
        => await _db.ImportCoverageReports.Where(r => r.ProjectId == projectId)
            .OrderByDescending(r => r.Id).FirstOrDefaultAsync(ct);

    public async Task<ImportCoverageReport> ComputeAsync(int? projectId, int? ingestionJobId = null, CancellationToken ct = default)
    {
        // Files (artifacts) in scope — lean projection, no RawText materialized.
        var files = await _db.ImportedFiles
            .Where(f => f.ProjectId == projectId)
            .Select(f => new { f.Id, f.RelativePath, f.DetectedLanguage, f.FileClass, f.Skipped, f.SkipReason, f.ExtractionStatus, f.ExtractionNote })
            .ToListAsync(ct);

        // Symbol presence is the source of truth for "extracted" (robust even for artifacts imported before
        // ExtractionStatus existed); ExtractionStatus only distinguishes the parse-error case.
        var symbolArtifacts = (await _db.CodeSymbols.Where(s => s.ProjectId == projectId)
            .Select(s => s.SourceArtifactId).Distinct().ToListAsync(ct)).ToHashSet();

        var r = new ImportCoverageReport { ProjectId = projectId, IngestionJobId = ingestionJobId, FilesDiscovered = files.Count };

        var skipReasons = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var unsupportedLangs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var parseErrors = new List<ParseErrorItem>();

        foreach (var f in files)
        {
            if (f.Skipped)
            {
                r.FilesSkipped++;
                var reason = string.IsNullOrEmpty(f.SkipReason) ? "skipped" : f.SkipReason!;
                skipReasons[reason] = skipReasons.GetValueOrDefault(reason) + 1;
                continue;
            }
            r.FilesImported++;

            if (CoverageLanguages.IsSupported(f.DetectedLanguage))
            {
                if (f.ExtractionStatus == ExtractionStatus.ParseError)
                {
                    r.FilesParseError++;
                    if (parseErrors.Count < 50) parseErrors.Add(new ParseErrorItem(f.RelativePath ?? "?", f.ExtractionNote));
                }
                else if (symbolArtifacts.Contains(f.Id)) r.FilesExtracted++;
                else r.FilesNoSymbols++; // parsed fine, nothing to declare
            }
            else if (CoverageLanguages.IsUnsupportedCode(f.DetectedLanguage))
            {
                r.FilesUnsupported++;
                unsupportedLangs.Add(f.DetectedLanguage!);
            }
            else
            {
                r.FilesNonCode++; // docs / config / data / binary — not expected to yield symbols
            }
        }

        // Structural counts.
        r.SymbolCount = await _db.CodeSymbols.CountAsync(s => s.ProjectId == projectId, ct);
        r.ReferenceCount = await _db.CodeSymbolReferences.CountAsync(x => x.ProjectId == projectId, ct);
        r.EdgeCount = await _db.CodeEdges.CountAsync(e => e.ProjectId == projectId, ct);

        // Resolved vs unresolved (idempotent rebuild returns the honest numbers).
        var g = await _graph.RebuildForProjectAsync(projectId, ct);
        r.UnresolvedReferences = g.Unresolved;
        r.ResolvedReferences = Math.Max(0, r.ReferenceCount - g.Unresolved);

        // Per-language breakdown. "Extracted" uses symbol presence (robust source of truth), not ExtractionStatus.
        var byLangFiles = files.Where(f => !f.Skipped).GroupBy(f => f.DetectedLanguage ?? "(none)")
            .ToDictionary(grp => grp.Key, grp => (Files: grp.Count(), Extracted: grp.Count(x => symbolArtifacts.Contains(x.Id))));
        var symByLang = (await _db.CodeSymbols.Where(s => s.ProjectId == projectId && s.DetectedLanguage != null)
                .GroupBy(s => s.DetectedLanguage!).Select(grp => new { Lang = grp.Key, Count = grp.Count() }).ToListAsync(ct))
            .ToDictionary(x => x.Lang, x => x.Count, StringComparer.OrdinalIgnoreCase);
        var refByLang = (await _db.CodeSymbolReferences.Where(x => x.ProjectId == projectId)
                .Join(_db.CodeSymbols, rr => rr.FromSymbolId, s => s.Id, (rr, s) => s.DetectedLanguage)
                .Where(l => l != null).GroupBy(l => l!).Select(grp => new { Lang = grp.Key, Count = grp.Count() }).ToListAsync(ct))
            .ToDictionary(x => x.Lang, x => x.Count, StringComparer.OrdinalIgnoreCase);

        var languages = byLangFiles.Select(kv => new LanguageCoverage(
            kv.Key, kv.Value.Files, kv.Value.Extracted,
            symByLang.GetValueOrDefault(kv.Key), refByLang.GetValueOrDefault(kv.Key),
            CoverageLanguages.IsSupported(kv.Key))).OrderByDescending(x => x.Files).ToList();

        // Confidence distribution over edges.
        var confRaw = await _db.CodeEdges.Where(e => e.ProjectId == projectId)
            .GroupBy(e => e.Confidence).Select(grp => new { Conf = grp.Key, Count = grp.Count() }).ToListAsync(ct);
        var conf = confRaw.OrderByDescending(x => x.Conf)
            .Select(x => new ConfidenceBand(x.Conf >= 1.0 ? "1.00 (deterministic)" : x.Conf.ToString("0.00") + " (syntax-only)", x.Count)).ToList();

        r.LanguageBreakdownJson = JsonSerializer.Serialize(languages);
        r.SkipReasonsJson = JsonSerializer.Serialize(skipReasons.Select(kv => new SkipReasonCount(kv.Key, kv.Value)).OrderByDescending(x => x.Count));
        r.ParseErrorsJson = JsonSerializer.Serialize(parseErrors);
        r.ConfidenceJson = JsonSerializer.Serialize(conf);
        r.UnsupportedLanguagesJson = JsonSerializer.Serialize(unsupportedLangs.OrderBy(x => x));

        _db.ImportCoverageReports.Add(r);
        await _db.SaveChangesAsync(ct);
        return r;
    }
}
