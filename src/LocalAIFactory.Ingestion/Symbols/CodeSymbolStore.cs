using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Identity;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Ingestion.Symbols;

// KE-008: reconciles the symbols of one source artifact into the CodeSymbols table. Convergent upsert
// keyed on SourceLocusKey: matched symbols keep their Uid (so graph/pack references survive), new symbols
// are inserted, symbols no longer present in the file are deleted. Parent links are resolved in a second
// pass once new rows have ids. Pure MSSQL/EF — no external services.
public sealed class CodeSymbolStore : ICodeSymbolStore
{
    // Column lengths must match AppDbContext; values are truncated defensively so pathological names
    // (deeply nested generics) can never overflow a column at insert time.
    private const int MaxName = 400;
    private const int MaxFullName = 512;
    private const int MaxSignature = 2000;

    private readonly AppDbContext _db;
    private readonly ICodeSymbolExtractorRouter _router;

    public CodeSymbolStore(AppDbContext db, ICodeSymbolExtractorRouter router)
    {
        _db = db; _router = router;
    }

    public async Task<int> UpsertForArtifactAsync(int sourceArtifactId, CancellationToken ct = default)
    {
        var art = await _db.ImportedFiles.FirstOrDefaultAsync(f => f.Id == sourceArtifactId, ct);
        if (art is null || art.Skipped || string.IsNullOrEmpty(art.RawText)) return 0;
        if (!_router.CanExtract(art.DetectedLanguage)) return 0;

        var extracted = _router.Extract(art.DetectedLanguage, art.RawText);
        var fileLocus = SourceLocus.FileKey(art.ProjectId, art.RelativePath);
        var now = DateTime.UtcNow;

        var existing = await _db.CodeSymbols.Where(s => s.FileLocusKey == fileLocus).ToListAsync(ct);
        var byLocus = existing.ToDictionary(s => s.SourceLocusKey);

        // Pass 1: insert/update by locus. parentByLocus remembers each symbol's declared parent FullName.
        var seen = new HashSet<string>();
        var parentByLocus = new Dictionary<string, string?>();
        foreach (var ex in extracted)
        {
            var locus = SourceLocus.SymbolKey(art.ProjectId, art.RelativePath, ex.Kind, ex.FullName, ex.Signature);
            if (!seen.Add(locus)) continue; // identical locus within one file (e.g. partial type) collapses to one row
            parentByLocus[locus] = ex.ParentFullName;
            var hash = SymbolHash(ex);

            if (byLocus.TryGetValue(locus, out var sym))
            {
                if (sym.SymbolHash != hash) Apply(sym, ex, hash);
                sym.SourceArtifactId = art.Id;
                sym.DetectedLanguage = art.DetectedLanguage;
                sym.ExtractedUtc = now;
                sym.ParentSymbolId = null; // re-resolved in pass 2
            }
            else
            {
                var ns = new CodeSymbol
                {
                    ProjectId = art.ProjectId,
                    SourceArtifactId = art.Id,
                    FileLocusKey = fileLocus,
                    SourceLocusKey = locus,
                    DetectedLanguage = art.DetectedLanguage,
                    ExtractedUtc = now
                };
                Apply(ns, ex, hash);
                _db.CodeSymbols.Add(ns);
                byLocus[locus] = ns;
            }
        }

        // Delete symbols that are no longer declared in the file.
        foreach (var kv in byLocus)
            if (!seen.Contains(kv.Key))
                _db.CodeSymbols.Remove(kv.Value);

        await _db.SaveChangesAsync(ct); // assigns ids to inserted rows

        // Pass 2: resolve containment. Parents are namespaces/types, whose FullName is unique within a
        // file, so a FullName -> id map is unambiguous for parent lookup.
        var current = seen.Select(l => byLocus[l]).ToList();
        var parentMap = new Dictionary<string, int>();
        foreach (var s in current)
            if (IsContainer(s.Kind))
                parentMap[s.FullName] = s.Id;

        bool changed = false;
        foreach (var s in current)
        {
            int? parentId = parentByLocus.TryGetValue(s.SourceLocusKey, out var pf) && pf != null
                && parentMap.TryGetValue(pf, out var pid) ? pid : null;
            if (s.ParentSymbolId != parentId) { s.ParentSymbolId = parentId; changed = true; }
        }
        if (changed) await _db.SaveChangesAsync(ct);

        return current.Count;
    }

    private static bool IsContainer(CodeSymbolKind k)
        => k is CodeSymbolKind.Namespace or CodeSymbolKind.Class or CodeSymbolKind.Interface
            or CodeSymbolKind.Struct or CodeSymbolKind.Record or CodeSymbolKind.Enum or CodeSymbolKind.Delegate;

    private static void Apply(CodeSymbol s, ExtractedSymbol ex, string hash)
    {
        s.Kind = ex.Kind;
        s.Name = Trunc(ex.Name, MaxName);
        s.FullName = Trunc(ex.FullName, MaxFullName);
        s.NormalizedKey = s.FullName.ToLowerInvariant(); // KE-010: object key for resolution / KE-011 lexical
        s.Signature = ex.Signature is null ? null : Trunc(ex.Signature, MaxSignature);
        s.Access = ex.Access;
        s.IsPublic = ex.IsPublic;
        s.StartOffset = ex.StartOffset;
        s.EndOffset = ex.EndOffset;
        s.StartLine = ex.StartLine;
        s.EndLine = ex.EndLine;
        s.ComplexitySignal = ex.ComplexitySignal;
        s.SymbolHash = hash;
    }

    // Change-detection digest: location + signature + syntactic attributes. Independent of FileLocusKey/Uid
    // so identical re-extraction is a true no-op (no UpdatedUtc churn, no version noise).
    private static string SymbolHash(ExtractedSymbol ex)
    {
        var s = $"{(int)ex.Kind}|{ex.FullName}|{ex.Signature}|{(int)ex.Access}|{ex.IsPublic}|{ex.StartOffset}|{ex.EndOffset}|{ex.StartLine}|{ex.EndLine}|{ex.ComplexitySignal}";
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
    }

    private static string Trunc(string s, int max) => s.Length <= max ? s : s.Substring(0, max);
}
