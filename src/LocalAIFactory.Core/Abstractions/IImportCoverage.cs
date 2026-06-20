using LocalAIFactory.Core.Entities;

namespace LocalAIFactory.Core.Abstractions;

// R2-P0A: shared shapes for the JSON breakdowns in ImportCoverageReport (service writes, UI reads).
public sealed record LanguageCoverage(string Language, int Files, int Extracted, int Symbols, int References, bool Supported);
public sealed record SkipReasonCount(string Reason, int Count);
public sealed record ParseErrorItem(string Path, string? Note);
public sealed record ConfidenceBand(string Band, int Count);

// R2-P0A: the authoritative list of what deterministic structural extraction supports today. Surfaced in the
// gap report so a user can see which languages are analyzed and which are not — no silent assumptions.
public static class CoverageLanguages
{
    // Languages with a real extractor.
    public static readonly HashSet<string> Supported = new(StringComparer.OrdinalIgnoreCase) { "csharp", "sql" };

    // Code languages we detect but do NOT extract yet (honest "unsupported", not "empty").
    public static readonly HashSet<string> UnsupportedCode = new(StringComparer.OrdinalIgnoreCase)
    {
        "vbnet", "fsharp", "python", "javascript", "typescript", "java", "kotlin", "cpp", "go", "ruby", "php", "razor"
    };

    public static bool IsSupported(string? lang) => lang != null && Supported.Contains(lang);
    public static bool IsUnsupportedCode(string? lang) => lang != null && UnsupportedCode.Contains(lang);
}

// R2-P0A: computes and persists the per-import coverage / gap report. Append-only; reads only artifacts and
// derived structural tables; never touches curated knowledge.
public interface IImportCoverageService
{
    Task<ImportCoverageReport> ComputeAsync(int? projectId, int? ingestionJobId = null, CancellationToken ct = default);
    Task<ImportCoverageReport?> LatestForProjectAsync(int? projectId, CancellationToken ct = default);
}
