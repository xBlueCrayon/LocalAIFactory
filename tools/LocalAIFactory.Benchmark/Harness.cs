using System.Diagnostics;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Classification;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Maintenance;
using LocalAIFactory.Ingestion.Symbols;
using LocalAIFactory.Rag.Retrieval;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Benchmark;

// Runs one benchmark repository end-to-end through the REAL structural services (the same code the product
// uses) — deterministic, MSSQL/EF or InMemory, no model/vectors. Proves convergence (consolidate twice),
// measures discovery/graph/retrieval/impact, and answers the Proof-of-Vision catalogue.
public sealed class Harness
{
    private static readonly HttpClient Http = new();
    private readonly FileClassifier _classifier = new();

    public async Task<RepoResult> RunAsync(AppDbContext db, RepoSpec spec, int projectId, string cacheDir)
    {
        var files = spec.Type == "sqlfiles"
            ? await FetchSqlFilesAsync(spec)
            : CloneAndGatherFiles(spec, cacheDir);

        // Create immutable artifacts for EVERY discovered file (honest discovery — unsupported/non-code/binary
        // are visible, not silently dropped). Binary files are recorded as skipped (no text read).
        foreach (var (rel, content, cls) in files)
        {
            var lang = _classifier.DetectLanguage(Path.GetExtension(rel));
            var binary = cls == FileClass.Binary || content is null;
            db.ImportedFiles.Add(new ImportedFile
            {
                ProjectId = projectId, RelativePath = rel, FileName = Path.GetFileName(rel),
                DetectedLanguage = lang, FileClass = cls, RawText = binary ? null : content,
                Skipped = binary, SkipReason = binary ? "binary" : null,
                Status = ImportStatus.Processed
            });
        }
        await db.SaveChangesAsync();
        // "candidate" = files we CAN analyse (supported languages); discovery is measured over these.
        var candidates = await db.ImportedFiles.CountAsync(f => f.ProjectId == projectId && !f.Skipped
            && (f.DetectedLanguage == "csharp" || f.DetectedLanguage == "sql"));

        // Build the structural layer via consolidation (extracts from raw + graph + prune). Run twice to
        // prove convergence — repeated maintenance does not change the graph.
        var consolidation = new StructuralConsolidationService(db, CodeStore(db), SchemaStore(db), new CodeGraphBuilder(db));
        var first = await consolidation.ConsolidateProjectAsync(projectId);
        var second = await consolidation.ConsolidateProjectAsync(projectId);
        var convergent = first.LiveSymbols == second.LiveSymbols && first.Edges == second.Edges
                         && second.OrphanSymbolsRemoved == 0 && second.OrphanEdgesRemoved == 0;

        // Resolution metrics (edges vs unresolved references).
        var g = await new CodeGraphBuilder(db).RebuildForProjectAsync(projectId);

        var symbols = await db.CodeSymbols.CountAsync(s => s.ProjectId == projectId);
        var parsed = await db.CodeSymbols.Where(s => s.ProjectId == projectId).Select(s => s.SourceArtifactId).Distinct().CountAsync();
        var references = await db.CodeSymbolReferences.CountAsync(r => r.ProjectId == projectId);
        var edgesByType = await db.CodeEdges.Where(e => e.ProjectId == projectId)
            .GroupBy(e => e.RelationType).Select(grp => new { Type = grp.Key, Count = grp.Count() }).ToListAsync();

        // R2-P0A: compute the honest coverage / gap report (the same service the product uses).
        var cov = await new LocalAIFactory.Ingestion.Coverage.ImportCoverageService(db, new CodeGraphBuilder(db))
            .ComputeAsync(projectId);
        var unsupportedLangs = System.Text.Json.JsonSerializer.Deserialize<List<string>>(cov.UnsupportedLanguagesJson) ?? new();

        var result = new RepoResult
        {
            Name = spec.Name, Bucket = spec.Bucket, Sha = spec.Sha ?? "",
            CandidateFiles = candidates, ParsedArtifacts = parsed,
            Symbols = symbols, Edges = g.Edges, References = references, Unresolved = g.Unresolved,
            EdgesByType = edgesByType.ToDictionary(x => x.Type.ToString(), x => x.Count),
            Convergent = convergent,
            Coverage = new CoverageBlock
            {
                FilesDiscovered = cov.FilesDiscovered, FilesImported = cov.FilesImported,
                FilesExtracted = cov.FilesExtracted, FilesNoSymbols = cov.FilesNoSymbols,
                FilesUnsupported = cov.FilesUnsupported, FilesParseError = cov.FilesParseError,
                FilesNonCode = cov.FilesNonCode, FilesSkipped = cov.FilesSkipped,
                ResolvedReferences = cov.ResolvedReferences, UnresolvedReferences = cov.UnresolvedReferences,
                UnsupportedLanguages = unsupportedLangs
            }
        };

        var retrieval = new StructuralRetrievalService(db);
        foreach (var pov in spec.ProofOfVision)
            result.Pov.Add(await RunPovAsync(retrieval, projectId, pov));

        return result;
    }

    private static async Task<PovResult> RunPovAsync(StructuralRetrievalService r, int projectId, PovCheck pov)
    {
        var must = pov.MustContain ?? Array.Empty<string>();
        int count; HashSet<string> names;
        switch (pov.Mode)
        {
            case "find":
                var hits = await r.FindByIdentifierAsync(projectId, pov.Target);
                count = hits.Count; names = hits.Select(h => h.FullName).ToHashSet();
                break;
            case "dependents":
                var deps = await r.DependentsOfAsync(projectId, pov.Target);
                count = deps.Count; names = deps.Select(d => d.Symbol.FullName).ToHashSet();
                break;
            case "impact":
                var imp = await r.ImpactOfAsync(projectId, pov.Target);
                var all = imp is null ? new List<string>() : imp.Direct.Concat(imp.Transitive).Select(n => n.Symbol.FullName).ToList();
                count = all.Count; names = all.ToHashSet();
                break;
            default:
                return new PovResult { Question = pov.Question, Mode = pov.Mode, Target = pov.Target, Passed = false, Detail = "unknown mode" };
        }
        var missing = must.Where(m => !names.Contains(m)).ToList();
        var passed = count >= pov.MinCount && missing.Count == 0;
        return new PovResult
        {
            Question = pov.Question, Mode = pov.Mode, Target = pov.Target, Count = count, Passed = passed,
            Detail = passed ? null : (missing.Count > 0 ? $"missing: {string.Join(", ", missing)}" : $"count {count} < {pov.MinCount}")
        };
    }

    // ---- sources ----

    private static async Task<List<(string rel, string? content, FileClass cls)>> FetchSqlFilesAsync(RepoSpec spec)
    {
        var outp = new List<(string, string?, FileClass)>();
        foreach (var url in spec.FileUrls ?? Array.Empty<string>())
        {
            var content = await Http.GetStringAsync(url);
            var name = Uri.UnescapeDataString(url[(url.LastIndexOf('/') + 1)..]);
            outp.Add(($"db/{name}", content, FileClass.SqlScript));
        }
        spec.Sha = "pinned-ssdt-files";
        return outp;
    }

    // Gather EVERY file under the configured srcDirs (not just *.cs) so the gap report can honestly account for
    // unsupported, non-code and binary files. Binary files are returned with null content (recorded as skipped).
    private List<(string rel, string? content, FileClass cls)> CloneAndGatherFiles(RepoSpec spec, string cacheDir)
    {
        var dir = Path.Combine(cacheDir, spec.Code);
        if (!Directory.Exists(Path.Combine(dir, ".git")))
        {
            Directory.CreateDirectory(dir);
            Git(dir, "init");
            Git(dir, $"remote add origin {spec.GitUrl}");
            Git(dir, $"fetch --depth 1 origin {spec.Sha}");
            Git(dir, "checkout FETCH_HEAD");
        }
        spec.Sha = Git(dir, "rev-parse HEAD").Trim();

        var sep = Path.DirectorySeparatorChar;
        var outp = new List<(string, string?, FileClass)>();
        foreach (var sub in spec.SrcDirs ?? new[] { "" })
        {
            var root = Path.Combine(dir, sub);
            if (!Directory.Exists(root)) continue;
            foreach (var f in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                if (f.Contains($"{sep}obj{sep}") || f.Contains($"{sep}bin{sep}") || f.Contains($"{sep}.git{sep}")) continue;
                var rel = Path.GetRelativePath(dir, f).Replace('\\', '/');
                var cls = _classifier.Classify(rel);
                if (cls == FileClass.Binary) { outp.Add((rel, null, cls)); continue; }
                string? content; try { content = File.ReadAllText(f); } catch { content = null; }
                outp.Add((rel, content, cls));
            }
        }
        return outp;
    }

    private static string Git(string dir, string args)
    {
        var psi = new ProcessStartInfo("git", args)
        {
            WorkingDirectory = dir, RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false
        };
        using var p = Process.Start(psi)!;
        var o = p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        return o;
    }

    private static CodeSymbolStore CodeStore(AppDbContext db) =>
        new(db, new CodeSymbolExtractorRouter(new[] { new CSharpSymbolExtractor() }));
    private static SchemaSymbolStore SchemaStore(AppDbContext db) =>
        new(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() }));
}
