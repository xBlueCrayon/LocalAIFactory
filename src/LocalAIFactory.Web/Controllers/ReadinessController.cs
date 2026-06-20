using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace LocalAIFactory.Web.Controllers;

// R2-ACC-POC-ENTERPRISE: a read-only Enterprise Readiness page driven by docs/readiness-scorecard.json.
// Honest, evidence-linked maturity view for a CEO/CTO. Never blocks on external services; if the scorecard
// file is absent it renders a friendly message rather than 500-ing. Open to authenticated users (read-only).
public sealed class ReadinessController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    public ReadinessController(IWebHostEnvironment env, IConfiguration config) { _env = env; _config = config; }

    public IActionResult Index()
    {
        var path = ResolveScorecard();
        if (path is null) { ViewBag.Missing = true; return View(new ScorecardVm()); }
        try
        {
            var vm = JsonSerializer.Deserialize<ScorecardVm>(System.IO.File.ReadAllText(path), Json) ?? new ScorecardVm();
            return View(vm);
        }
        catch { ViewBag.Missing = true; return View(new ScorecardVm()); }
    }

    private string? ResolveScorecard()
    {
        var candidates = new List<string>();
        var cfg = _config["Readiness:ScorecardPath"];
        if (!string.IsNullOrWhiteSpace(cfg)) candidates.Add(cfg);
        candidates.Add(Path.Combine(AppContext.BaseDirectory, "readiness", "readiness-scorecard.json")); // published output
        var dir = new DirectoryInfo(_env.ContentRootPath);
        for (int i = 0; i < 6 && dir is not null; i++, dir = dir.Parent)
            candidates.Add(Path.Combine(dir.FullName, "docs", "readiness-scorecard.json"));
        foreach (var c in candidates) if (System.IO.File.Exists(c)) return c;
        return null;
    }

    public sealed class ScorecardVm
    {
        public string? Title { get; set; }
        public string? Phase { get; set; }
        public string? AsOfCommit { get; set; }
        public string? LastReviewedUtc { get; set; }
        public string? HonestyNote { get; set; }
        public List<AreaVm> Areas { get; set; } = new();
        public int OverallAverage => Areas.Count == 0 ? 0 : (int)Math.Round(Areas.Average(a => a.CurrentScore));
    }

    public sealed class AreaVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public int CurrentScore { get; set; }
        public string? Confidence { get; set; }
        public List<string> Evidence { get; set; } = new();
        public List<string> Blockers { get; set; } = new();
        public string? ProofRequiredFor100 { get; set; }
    }
}
