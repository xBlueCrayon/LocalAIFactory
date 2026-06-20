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
            : CloneAndGatherCSharp(spec, cacheDir);

        // Create immutable artifacts (mirrors the import layer minimally — no model/embeddings needed).
        foreach (var (rel, content) in files)
        {
            var lang = _classifier.DetectLanguage(Path.GetExtension(rel));
            if (lang is not ("csharp" or "sql")) continue;
            db.ImportedFiles.Add(new ImportedFile
            {
                ProjectId = projectId, RelativePath = rel, FileName = Path.GetFileName(rel),
                DetectedLanguage = lang, RawText = content, Status = ImportStatus.Processed
            });
        }
        await db.SaveChangesAsync();
        var candidates = await db.ImportedFiles.CountAsync(f => f.ProjectId == projectId);

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

        var result = new RepoResult
        {
            Name = spec.Name, Bucket = spec.Bucket, Sha = spec.Sha ?? "",
            CandidateFiles = candidates, ParsedArtifacts = parsed,
            Symbols = symbols, Edges = g.Edges, References = references, Unresolved = g.Unresolved,
            EdgesByType = edgesByType.ToDictionary(x => x.Type.ToString(), x => x.Count),
            Convergent = convergent
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

    private static async Task<List<(string rel, string content)>> FetchSqlFilesAsync(RepoSpec spec)
    {
        var outp = new List<(string, string)>();
        foreach (var url in spec.FileUrls ?? Array.Empty<string>())
        {
            var content = await Http.GetStringAsync(url);
            var name = Uri.UnescapeDataString(url[(url.LastIndexOf('/') + 1)..]);
            outp.Add(($"db/{name}", content));
        }
        spec.Sha = "pinned-ssdt-files";
        return outp;
    }

    private static List<(string rel, string content)> CloneAndGatherCSharp(RepoSpec spec, string cacheDir)
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

        var outp = new List<(string, string)>();
        foreach (var sub in spec.SrcDirs ?? new[] { "" })
        {
            var root = Path.Combine(dir, sub);
            if (!Directory.Exists(root)) continue;
            foreach (var f in Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories))
            {
                if (f.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") ||
                    f.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}")) continue;
                outp.Add((Path.GetRelativePath(dir, f).Replace('\\', '/'), File.ReadAllText(f)));
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
