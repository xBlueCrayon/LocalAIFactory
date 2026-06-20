using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Ingestion.Symbols;

// KE-009: routes a resolved SQL dialect to its extractor. T-SQL today; PL/pgSQL and PL/SQL register one more
// ISqlSchemaExtractor each, with no change here or in the pipeline. The generic detected-language "sql" maps
// to the configured default dialect ("tsql" for the banking MSSQL estate) — the seam where per-file dialect
// detection (multi-database repositories) plugs in later without a redesign.
public sealed class SqlSchemaExtractorRouter : ISqlSchemaExtractorRouter
{
    private const string DefaultDialect = "tsql";
    private readonly Dictionary<string, ISqlSchemaExtractor> _byDialect;

    public SqlSchemaExtractorRouter(IEnumerable<ISqlSchemaExtractor> extractors)
    {
        _byDialect = extractors.ToDictionary(e => e.Dialect, StringComparer.OrdinalIgnoreCase);
    }

    // "sql" (the generic detected-language) and an explicit dialect ("tsql") both resolve here.
    private string Resolve(string? detectedLanguageOrDialect)
        => string.Equals(detectedLanguageOrDialect, "sql", StringComparison.OrdinalIgnoreCase)
            ? DefaultDialect
            : detectedLanguageOrDialect ?? "";

    public bool CanExtract(string? detectedLanguageOrDialect)
    {
        var d = Resolve(detectedLanguageOrDialect);
        return !string.IsNullOrEmpty(d) && _byDialect.ContainsKey(d);
    }

    public SqlExtractionResult Extract(string? detectedLanguageOrDialect, string content)
    {
        var d = Resolve(detectedLanguageOrDialect);
        return !string.IsNullOrEmpty(d) && _byDialect.TryGetValue(d, out var ex)
            ? ex.Extract(content)
            : SqlExtractionResult.Empty;
    }

    // The dialect a recognized SQL artifact is treated as (for stamping DetectedLanguage on stored symbols).
    public string ResolveDialect(string? detectedLanguageOrDialect) => Resolve(detectedLanguageOrDialect);
}
