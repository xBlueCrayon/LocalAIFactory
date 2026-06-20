namespace LocalAIFactory.Core.Entities;

// R2-P0A: the per-import honesty record — exactly what the platform did and did NOT understand for a project.
// Append-only (each ingestion/compute adds a new row; history is preserved). Scalar counts are columns; the
// breakdowns (per-language, skip reasons, parse errors, confidence bands, unsupported languages) are JSON so
// the table stays lean and additive. The whole purpose: no silent blind spots.
public class ImportCoverageReport : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();
    public int? ProjectId { get; set; }
    public int? IngestionJobId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

    // ---- file coverage ----
    public int FilesDiscovered { get; set; }   // artifacts that reached the import layer
    public int FilesImported { get; set; }      // not skipped (have content)
    public int FilesSkipped { get; set; }       // dedup / binary / oversized — with reasons
    public int FilesExtracted { get; set; }     // parsed and produced >= 1 symbol
    public int FilesNoSymbols { get; set; }     // parsed fine, nothing to declare
    public int FilesUnsupported { get; set; }   // a code language with no extractor yet
    public int FilesParseError { get; set; }    // extractor threw / could not parse
    public int FilesNonCode { get; set; }       // docs/config/binary — not expected to yield symbols

    // ---- structural coverage ----
    public int SymbolCount { get; set; }
    public int ReferenceCount { get; set; }
    public int ResolvedReferences { get; set; }
    public int UnresolvedReferences { get; set; }
    public int EdgeCount { get; set; }

    // ---- breakdowns (JSON) ----
    public string LanguageBreakdownJson { get; set; } = "[]";   // [{language, files, extracted, symbols, references}]
    public string SkipReasonsJson { get; set; } = "[]";         // [{reason, count}]
    public string ParseErrorsJson { get; set; } = "[]";         // [{path, note}] (top N)
    public string ConfidenceJson { get; set; } = "[]";          // [{band, count}] over edges
    public string UnsupportedLanguagesJson { get; set; } = "[]"; // ["python","vbnet",...]

    // ---- honesty flags / derived ----
    // Coverage is measured over ANALYZABLE (supported-language) files only — extracted vs (extracted + no-symbols
    // + parse-error). Unsupported and non-code files are reported separately, not counted as analysis failures.
    public int AnalyzableFiles => FilesExtracted + FilesNoSymbols + FilesParseError;
    public double FileCoverage => AnalyzableFiles == 0 ? 0 : (double)FilesExtracted / AnalyzableFiles;
    public double ResolutionRate => (ResolvedReferences + UnresolvedReferences) == 0
        ? 0 : (double)ResolvedReferences / (ResolvedReferences + UnresolvedReferences);
    public bool HasUnsupported => FilesUnsupported > 0;
    public bool HasParseErrors => FilesParseError > 0;
    // Structural analysis is project-scoped only; cross-project/repo/DB resolution does not exist yet.
    public bool ProjectScopedOnly { get; set; } = true;
}
