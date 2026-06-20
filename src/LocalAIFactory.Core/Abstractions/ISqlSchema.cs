using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-009: one schema symbol produced by a SQL dialect extractor before reconciliation to a CodeSymbol row.
// Identity is object-scoped (database/schema/object[/column]) — NOT file-scoped — so a CREATE in one file and
// an ALTER in another converge on the same logical object. ParentFullName is the display FullName of the
// containing object (schema for a table/view/proc; table for a column/constraint/FK/index).
public sealed record ExtractedSqlSymbol(
    CodeSymbolKind Kind,
    string? Database,
    string Schema,
    string ObjectName,
    string? Column,
    string Name,        // simple name (the object or column)
    string FullName,    // display, e.g. "dbo.Account" or "dbo.Account.AccountId"
    string? Signature,  // column type / parameter+return shape
    int StartOffset,
    int EndOffset,
    int StartLine,
    int EndLine,
    int ComplexitySignal,
    string? ParentFullName);

// KE-009: a deterministic reference one symbol makes to another object (staging for KE-010). The owner is
// identified by FromFullName (matched to the emitted symbol); the target by canonical name parts.
public sealed record ExtractedSqlReference(
    CodeReferenceKind Kind,
    string FromFullName,
    string? ReferencedDatabase,
    string ReferencedSchema,
    string ReferencedObject,
    string? ReferencedColumn);

// KE-009: the full result of parsing one SQL artifact — schema symbols plus the references they make.
public sealed record SqlExtractionResult(
    IReadOnlyList<ExtractedSqlSymbol> Symbols,
    IReadOnlyList<ExtractedSqlReference> References)
{
    public static readonly SqlExtractionResult Empty =
        new(Array.Empty<ExtractedSqlSymbol>(), Array.Empty<ExtractedSqlReference>());
}

// KE-009: a per-dialect, deterministic, offline SQL schema extractor. Pluggable — T-SQL now (the concrete
// parser lives in the Ingestion layer, never here); PL/pgSQL and PL/SQL register the same contract later with
// no pipeline redesign. Implementations MUST be
// pure (same input -> same output), MUST NOT open a database connection or read a live catalog, and MUST
// classify DDL vs DML (only DDL CREATE/ALTER yields structural symbols).
public interface ISqlSchemaExtractor
{
    string Dialect { get; } // e.g. "tsql"
    SqlExtractionResult Extract(string content);
}

// KE-009: routes a resolved SQL dialect to its extractor. Unknown/unsupported dialects return false / Empty —
// never throw. The router also maps the generic "sql" detected-language to the default dialect ("tsql").
public interface ISqlSchemaExtractorRouter
{
    bool CanExtract(string? detectedLanguageOrDialect);
    SqlExtractionResult Extract(string? detectedLanguageOrDialect, string content);

    // The concrete dialect a recognized SQL artifact resolves to (e.g. "sql" -> "tsql"), for stamping on
    // stored symbols so KE-010/KE-011 know which dialect produced them.
    string ResolveDialect(string? detectedLanguageOrDialect);
}

// KE-009: persists/reconciles SQL schema symbols + references for one artifact. Object-scoped, upsert-only:
// matched objects keep their Uid; new objects insert; cross-file deletion is deferred to project-scoped
// consolidation (KE-012). References for the artifact are replaced wholesale (pure staging).
public interface ISchemaSymbolStore
{
    // Extracts and reconciles the schema of the given SQL source artifact. Returns the number of symbols
    // upserted for that artifact. A no-op (returns 0) when the artifact is skipped, has no text, is not a
    // recognized SQL dialect, or yields no DDL.
    Task<int> UpsertForArtifactAsync(int sourceArtifactId, CancellationToken ct = default);
}
