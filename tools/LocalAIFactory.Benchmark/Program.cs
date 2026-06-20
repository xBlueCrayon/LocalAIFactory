using System.Text.Json;
using LocalAIFactory.Benchmark;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;

// LocalAIFactory Validation Harness — the authoritative, reproducible measure of capability progression.
// Deterministic, MSSQL-authoritative (or --inmemory for CI), no model/vectors. Exit code != 0 on regression.
//
// Usage: dotnet run -- [--inmemory] [--update-golden] [--manifest <path>] [--connection <str>]

var inMemory = args.Contains("--inmemory");
var updateGolden = args.Contains("--update-golden");
var manifestPath = ArgValue("--manifest") ?? FindUp("benchmarks/benchmarks.json");
var repoRoot = Directory.GetParent(manifestPath)!.Parent!.FullName;
var cacheDir = Path.Combine(repoRoot, "benchmarks", "cache");
var goldenDir = Path.Combine(repoRoot, "benchmarks", "golden");
var reportDir = Path.Combine(repoRoot, "benchmarks", "reports");
Directory.CreateDirectory(cacheDir); Directory.CreateDirectory(goldenDir); Directory.CreateDirectory(reportDir);

var conn = ArgValue("--connection")
           ?? "Server=(localdb)\\MSSQLLocalDB;Database=LocalAIFactoryBenchmark;Trusted_Connection=True;TrustServerCertificate=True";

DbContextOptions<AppDbContext> BuildOptions() =>
    inMemory
        ? new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("benchmark").Options
        : new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(conn).Options;

Console.WriteLine($"== LocalAIFactory Validation Harness ==  provider={(inMemory ? "InMemory" : "SqlServer")}  goldenUpdate={updateGolden}");
var manifest = Manifest.Load(manifestPath);

// Clean, deterministic database for the run.
await using (var setup = new AppDbContext(BuildOptions()))
{
    if (!inMemory) await setup.Database.EnsureDeletedAsync();
    await setup.Database.EnsureCreatedAsync();
}

var harness = new Harness();
var results = new List<RepoResult>();
int regressions = 0, povFailures = 0;

foreach (var spec in manifest.Repos)
{
    Console.WriteLine($"\n-- {spec.Name} ({spec.Bucket}) --");
    try
    {
        await using var db = new AppDbContext(BuildOptions());
        var project = new Project { Name = spec.Name, Code = spec.Code, Description = "benchmark" };
        db.Projects.Add(project);
        await db.SaveChangesAsync();

        var r = await harness.RunAsync(db, spec, project.Id, cacheDir);
        results.Add(r);

        Console.WriteLine($"   sha={r.Sha}  files={r.ParsedArtifacts}/{r.CandidateFiles}  symbols={r.Symbols}  edges={r.Edges}  refs={r.References}  resolution={r.ResolutionRate:P0}(informational)  convergent={r.Convergent}");
        Console.WriteLine($"   coverage={r.DiscoveryCoverage:P0}[{r.DiscoveryTier}]  graph={r.GraphAccuracy:P0}[{r.GraphTier}]  retrieval={r.RetrievalAccuracy:P0}[{r.RetrievalTier}]  impact={r.ImpactAccuracy:P0}[{r.ImpactTier}]  => {r.OverallTier}");
        foreach (var p in r.Pov)
        {
            Console.WriteLine($"      [{(p.Passed ? "PASS" : "FAIL")}] {p.Question}  (n={p.Count}){(p.Detail is null ? "" : "  -- " + p.Detail)}");
            if (!p.Passed) povFailures++;
        }

        // Golden snapshot regression.
        var goldenPath = Path.Combine(goldenDir, $"{spec.Code}.json");
        if (updateGolden)
        {
            File.WriteAllText(goldenPath, JsonSerializer.Serialize(r, Manifest.Opts));
            Console.WriteLine($"   golden updated: {goldenPath}");
        }
        else if (File.Exists(goldenPath))
        {
            var golden = JsonSerializer.Deserialize<RepoResult>(File.ReadAllText(goldenPath), Manifest.Opts)!;
            if (Regressed(golden, r, out var why)) { regressions++; Console.WriteLine($"   !! REGRESSION vs golden: {why}"); }
            else Console.WriteLine("   == matches golden");
        }
        else Console.WriteLine("   (no golden yet — run with --update-golden to baseline)");
    }
    catch (Exception ex)
    {
        povFailures++;
        Console.WriteLine($"   ERROR: {ex.Message}");
    }
}

// Persist the run report.
var reportPath = Path.Combine(reportDir, "latest.json");
File.WriteAllText(reportPath, JsonSerializer.Serialize(results, Manifest.Opts));
Console.WriteLine($"\nReport: {reportPath}");

// Summary table.
Console.WriteLine("\n== SUMMARY ==");
foreach (var r in results)
    Console.WriteLine($"   {r.Name,-22} {r.OverallTier,-7} symbols={r.Symbols,-5} edges={r.Edges,-4} pov={r.Pov.Count(p => p.Passed)}/{r.Pov.Count}");

var exit = (povFailures == 0 && regressions == 0) ? 0 : 1;
Console.WriteLine($"\nResult: {(exit == 0 ? "PASS" : "FAIL")}  (povFailures={povFailures}, regressions={regressions})");
return exit;

// Regression = a tier drop or a previously-passing PoV now failing. Count drift alone is a warning (not fatal),
// since unpinned branches can shift; pin a SHA in the manifest for exact reproducibility.
static bool Regressed(RepoResult golden, RepoResult now, out string why)
{
    var order = new[] { "None", "Bronze", "Silver", "Gold" };
    if (Array.IndexOf(order, now.OverallTier) < Array.IndexOf(order, golden.OverallTier))
    { why = $"tier {golden.OverallTier} -> {now.OverallTier}"; return true; }
    var goldenPassing = golden.Pov.Where(p => p.Passed).Select(p => p.Question).ToHashSet();
    var nowFailing = now.Pov.Where(p => !p.Passed).Select(p => p.Question).ToHashSet();
    var broke = goldenPassing.Intersect(nowFailing).ToList();
    if (broke.Count > 0) { why = "PoV broke: " + string.Join("; ", broke); return true; }
    why = ""; return false;
}

string? ArgValue(string flag)
{
    var i = Array.IndexOf(args, flag);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}

static string FindUp(string rel)
{
    var dir = Directory.GetCurrentDirectory();
    for (int i = 0; i < 8 && dir is not null; i++)
    {
        var candidate = Path.Combine(dir, rel);
        if (File.Exists(candidate)) return candidate;
        dir = Directory.GetParent(dir)?.FullName;
    }
    throw new FileNotFoundException($"Could not locate {rel} from {Directory.GetCurrentDirectory()}");
}
