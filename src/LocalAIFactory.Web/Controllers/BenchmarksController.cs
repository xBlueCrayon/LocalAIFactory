using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

// P2 Pilot UX — Benchmark Dashboard. Trust first: this is the first screen of the pilot. It surfaces the
// Validation Harness's authoritative Bronze/Silver/Gold scores so a user sees proven capability before
// exploring. Reads the harness report (benchmarks/reports/latest.json); shows an empty state if absent.
public sealed class BenchmarksController : Controller
{
    private readonly IWebHostEnvironment _env;
    public BenchmarksController(IWebHostEnvironment env) => _env = env;

    public IActionResult Index()
    {
        var path = LocateReport(_env.ContentRootPath);
        if (path is null || !System.IO.File.Exists(path))
            return View(new List<BenchmarkRow>());

        var rows = new List<BenchmarkRow>();
        try
        {
            using var doc = JsonDocument.Parse(System.IO.File.ReadAllText(path));
            foreach (var r in doc.RootElement.EnumerateArray())
            {
                double Get(string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetDouble() : 0;
                int GetI(string n) => r.TryGetProperty(n, out var v) && v.ValueKind == JsonValueKind.Number ? v.GetInt32() : 0;
                string GetS(string n) => r.TryGetProperty(n, out var v) ? v.GetString() ?? "" : "";

                var pov = r.TryGetProperty("Pov", out var pv) ? pv.EnumerateArray().ToList() : new();
                var povPass = pov.Count(p => p.TryGetProperty("Passed", out var b) && b.GetBoolean());

                var discovery = Tier("discovery", Get("DiscoveryCoverage"));
                var graph = Tier("graph", Get("GraphAccuracy"));
                var retrieval = Tier("retrieval", Get("RetrievalAccuracy"));
                var impact = Tier("impact", Get("ImpactAccuracy"));
                rows.Add(new BenchmarkRow(
                    GetS("Name"), GetS("Bucket"), GetS("Sha"),
                    GetI("Symbols"), GetI("Edges"), GetI("References"),
                    povPass, pov.Count, r.TryGetProperty("Convergent", out var c) && c.GetBoolean(),
                    discovery, graph, retrieval, impact, Lowest(discovery, graph, retrieval, impact),
                    pov.Select(p => new PovRow(
                        p.GetProperty("Question").GetString() ?? "",
                        p.TryGetProperty("Passed", out var b) && b.GetBoolean(),
                        p.TryGetProperty("Count", out var n) ? n.GetInt32() : 0)).ToList()));
            }
        }
        catch { /* malformed report -> empty state */ }
        return View(rows);
    }

    private static string? LocateReport(string start)
    {
        var dir = start;
        for (int i = 0; i < 8 && dir is not null; i++)
        {
            var candidate = Path.Combine(dir, "benchmarks", "reports", "latest.json");
            if (System.IO.File.Exists(candidate)) return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        return null;
    }

    private static readonly string[] Order = { "None", "Bronze", "Silver", "Gold" };
    private static string Lowest(params string[] t) => t.OrderBy(x => Array.IndexOf(Order, x)).First();
    private static string Tier(string axis, double v) => axis switch
    {
        "discovery" => v >= 0.98 ? "Gold" : v >= 0.95 ? "Silver" : v >= 0.90 ? "Bronze" : "None",
        _ => v >= 0.95 ? "Gold" : v >= 0.85 ? "Silver" : v > 0 ? "Bronze" : "None"
    };

    public record BenchmarkRow(string Name, string Bucket, string Sha, int Symbols, int Edges, int References,
        int PovPass, int PovTotal, bool Convergent, string Discovery, string Graph, string Retrieval,
        string Impact, string Overall, List<PovRow> Pov);
    public record PovRow(string Question, bool Passed, int Count);
}
