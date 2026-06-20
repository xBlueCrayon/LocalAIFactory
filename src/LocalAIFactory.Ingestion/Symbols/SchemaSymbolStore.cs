using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Identity;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Ingestion.Symbols;

// KE-009: reconciles the schema of one SQL artifact into the CodeSymbols table, plus its references into the
// CodeSymbolReferences staging table. Identity is OBJECT-scoped (SchemaObjectKey), not file-scoped: a matched
// object keeps its Uid so a CREATE in one file and an ALTER in another converge on one logical symbol. Upsert-
// only — cross-file deletion is deferred to project-scoped consolidation (KE-012). References are pure staging:
// the artifact's references are replaced wholesale on each run. Pure MSSQL/EF — no external services.
public sealed class SchemaSymbolStore : ISchemaSymbolStore
{
    private const int MaxName = 400;
    private const int MaxFullName = 512;
    private const int MaxSignature = 2000;

    private readonly AppDbContext _db;
    private readonly ISqlSchemaExtractorRouter _router;

    public SchemaSymbolStore(AppDbContext db, ISqlSchemaExtractorRouter router)
    {
        _db = db; _router = router;
    }

    public async Task<int> UpsertForArtifactAsync(int sourceArtifactId, CancellationToken ct = default)
    {
        var art = await _db.ImportedFiles.FirstOrDefaultAsync(f => f.Id == sourceArtifactId, ct);
        if (art is null || art.Skipped || string.IsNullOrEmpty(art.RawText)) return 0;
        if (!_router.CanExtract(art.DetectedLanguage)) return 0;

        var result = _router.Extract(art.DetectedLanguage, art.RawText);
        if (result.Symbols.Count == 0)
        {
            // No DDL (e.g. a DML-only seed script). Parsed fine, nothing to declare — honest NoSymbols, not a
            // silent zero. Still clear this artifact's stale references and exit.
            art.ExtractionStatus = ExtractionStatus.NoSymbols;
            await ReplaceReferencesAsync(art, result.References, ct);
            return 0;
        }

        var dialect = _router.ResolveDialect(art.DetectedLanguage);
        var fileLocus = SourceLocus.FileKey(art.ProjectId, art.RelativePath);
        var now = DateTime.UtcNow;

        // Build the run's symbols keyed by object identity; collapse duplicates (e.g. a column repeated across
        // CREATE + ALTER in the same file) to the first occurrence.
        var run = new List<(string key, ExtractedSqlSymbol ex)>();
        var seen = new HashSet<string>();
        foreach (var ex in result.Symbols)
        {
            var key = SourceLocus.SchemaObjectKey(art.ProjectId, ex.Database, ex.Schema, ex.ObjectName, ex.Column, ex.Kind);
            if (seen.Add(key)) run.Add((key, ex));
        }

        // Load any existing symbols for these object keys (project-scoped — they may live in other files).
        var keys = run.Select(r => r.key).ToList();
        var existing = await _db.CodeSymbols
            .Where(s => s.ProjectId == art.ProjectId && keys.Contains(s.SourceLocusKey))
            .ToListAsync(ct);
        var byKey = existing.ToDictionary(s => s.SourceLocusKey);

        var current = new List<CodeSymbol>(run.Count);
        var parentOf = new Dictionary<string, string?>(); // objectKey -> parent display FullName
        foreach (var (key, ex) in run)
        {
            var hash = SymbolHash(ex);
            parentOf[key] = ex.ParentFullName;
            if (byKey.TryGetValue(key, out var sym))
            {
                if (sym.SymbolHash != hash) Apply(sym, ex, hash);
                sym.SourceArtifactId = art.Id;       // last writer wins (provenance "where")
                sym.FileLocusKey = fileLocus;
                sym.DetectedLanguage = dialect;
                sym.ExtractedUtc = now;
                sym.ParentSymbolId = null;            // re-resolved below
                current.Add(sym);
            }
            else
            {
                var ns = new CodeSymbol
                {
                    ProjectId = art.ProjectId,
                    SourceArtifactId = art.Id,
                    FileLocusKey = fileLocus,
                    SourceLocusKey = key,
                    DetectedLanguage = dialect,
                    Access = CodeAccess.NotApplicable,
                    IsPublic = true,                  // schema objects are part of the public surface
                    ExtractedUtc = now
                };
                Apply(ns, ex, hash);
                _db.CodeSymbols.Add(ns);
                byKey[key] = ns;
                current.Add(ns);
            }
        }

        await _db.SaveChangesAsync(ct); // assigns ids to inserted rows

        // Containment: resolve each symbol's parent (column/constraint/index -> table; table/view/routine ->
        // schema) by display FullName. Containers may be defined in another file, so query project-wide.
        var parentNames = parentOf.Values.Where(v => !string.IsNullOrEmpty(v)).Distinct().ToList();
        var containerKinds = new[]
        {
            CodeSymbolKind.Namespace, CodeSymbolKind.Table, CodeSymbolKind.View
        };
        var parentMap = await _db.CodeSymbols
            .Where(s => s.ProjectId == art.ProjectId && containerKinds.Contains(s.Kind) && parentNames.Contains(s.FullName))
            .Select(s => new { s.FullName, s.Id })
            .ToListAsync(ct);
        var parentIdByName = parentMap
            .GroupBy(x => x.FullName)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);

        bool changed = false;
        foreach (var s in current)
        {
            int? pid = parentOf.TryGetValue(s.SourceLocusKey, out var pf) && pf != null
                && parentIdByName.TryGetValue(pf, out var found) ? found : null;
            if (s.ParentSymbolId != pid) { s.ParentSymbolId = pid; changed = true; }
        }

        art.ExtractionStatus = ExtractionStatus.Extracted; // R2-P0A: honest outcome for the gap report

        // References: replace this artifact's references wholesale (pure staging for KE-010).
        var fullNameToId = current
            .GroupBy(s => s.FullName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().Id, StringComparer.OrdinalIgnoreCase);
        await ReplaceReferencesAsync(art, result.References, ct, fileLocus, fullNameToId, now);

        if (changed) await _db.SaveChangesAsync(ct);
        return current.Count;
    }

    private async Task ReplaceReferencesAsync(
        ImportedFile art, IReadOnlyList<ExtractedSqlReference> refs,
        CancellationToken ct, string? fileLocus = null, Dictionary<string, int>? fullNameToId = null, DateTime? now = null)
    {
        var stale = await _db.CodeSymbolReferences.Where(r => r.SourceArtifactId == art.Id).ToListAsync(ct);
        if (stale.Count > 0) _db.CodeSymbolReferences.RemoveRange(stale);

        if (refs.Count > 0 && fullNameToId is not null)
        {
            foreach (var r in refs)
            {
                if (!fullNameToId.TryGetValue(r.FromFullName, out var fromId)) continue; // owner not emitted
                _db.CodeSymbolReferences.Add(new CodeSymbolReference
                {
                    ProjectId = art.ProjectId,
                    FromSymbolId = fromId,
                    SourceArtifactId = art.Id,
                    ReferenceKind = r.Kind,
                    ReferencedDatabase = SourceLocus.NormalizeSqlIdentifier(r.ReferencedDatabase) is { Length: > 0 } d ? d : null,
                    ReferencedSchema = string.IsNullOrEmpty(r.ReferencedSchema) ? "dbo" : SourceLocus.NormalizeSqlIdentifier(r.ReferencedSchema),
                    ReferencedObject = SourceLocus.NormalizeSqlIdentifier(r.ReferencedObject),
                    ReferencedColumn = r.ReferencedColumn is null ? null : SourceLocus.NormalizeSqlIdentifier(r.ReferencedColumn),
                    ReferencedKey = SourceLocus.ReferencedObjectKey(r.ReferencedDatabase, r.ReferencedSchema, r.ReferencedObject),
                    FileLocusKey = fileLocus ?? SourceLocus.FileKey(art.ProjectId, art.RelativePath),
                    ExtractedUtc = now ?? DateTime.UtcNow
                });
            }
        }
        await _db.SaveChangesAsync(ct);
    }

    private static void Apply(CodeSymbol s, ExtractedSqlSymbol ex, string hash)
    {
        s.Kind = ex.Kind;
        s.Name = Trunc(ex.Name, MaxName);
        s.FullName = Trunc(ex.FullName, MaxFullName);
        s.NormalizedKey = s.FullName.ToLowerInvariant(); // KE-010: object key for resolution / KE-011 lexical
        s.Signature = ex.Signature is null ? null : Trunc(ex.Signature, MaxSignature);
        s.IsPublic = true;
        s.Access = CodeAccess.NotApplicable;
        s.StartOffset = ex.StartOffset;
        s.EndOffset = ex.EndOffset;
        s.StartLine = ex.StartLine;
        s.EndLine = ex.EndLine;
        s.ComplexitySignal = ex.ComplexitySignal;
        s.SymbolHash = hash;
    }

    // Change-detection digest: kind + identity + location + signature + complexity. Independent of Uid/locus
    // so identical re-extraction is a true no-op.
    private static string SymbolHash(ExtractedSqlSymbol ex)
    {
        var s = $"{(int)ex.Kind}|{ex.FullName}|{ex.Signature}|{ex.StartOffset}|{ex.EndOffset}|{ex.StartLine}|{ex.EndLine}|{ex.ComplexitySignal}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
}
