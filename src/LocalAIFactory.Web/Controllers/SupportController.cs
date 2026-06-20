using System.Diagnostics;
using System.Reflection;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Licensing;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

// R2-ACC-20X: a read-only Supportability / Operations dashboard for an admin or support engineer. It answers
// "is this install healthy and what version is it?" at a glance. Hard rules honoured:
//   * Health is read from the cached snapshot (IServiceHealthCache) — NEVER a synchronous Qdrant/Ollama call.
//   * DB facts use lightweight CountAsync queries, each wrapped so a DB hiccup degrades a tile to "unavailable"
//     rather than 500-ing the page. No large text columns are materialised.
//   * License/edition is evaluated deterministically and demo-safe (missing license => Community core).
// The page must always render — on an empty DB, a seeded DB, or MSSQL-only.
public sealed class SupportController : Controller
{
    private readonly AppDbContext _db;
    private readonly IServiceHealthCache _health;
    private readonly ILicenseVerifier _license;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public SupportController(AppDbContext db, IServiceHealthCache health, ILicenseVerifier license,
        IWebHostEnvironment env, IConfiguration config)
    { _db = db; _health = health; _license = license; _env = env; _config = config; }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var vm = new SupportVm
        {
            Environment = _env.EnvironmentName,
            MachineName = System.Environment.MachineName,
            Framework = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
            Os = System.Runtime.InteropServices.RuntimeInformation.OSDescription,
            Version = (Assembly.GetEntryAssembly()?.GetName().Version)?.ToString() ?? "unknown",
            InformationalVersion = Assembly.GetEntryAssembly()?
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "—",
            ServerTimeUtc = DateTime.UtcNow,
            ProcessUptime = FormatUptime(DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime())
        };

        // Health snapshot — from cache only (no blocking probe).
        var h = _health.Current;
        vm.Mode = h.ModeLabel;
        vm.ChatAvailable = h.ChatAvailable;
        vm.HealthCheckedUtc = h.LastCheckedUtc;
        vm.Health["Qdrant (optional vector store)"] = ServiceHealthSnapshot.Label(h.Qdrant);
        vm.Health["Ollama (optional local AI)"] = ServiceHealthSnapshot.Label(h.Ollama);
        vm.Health["Embeddings (optional)"] = ServiceHealthSnapshot.Label(h.Embeddings);

        // License / edition — deterministic, demo-safe.
        var lic = LoadLicenseFromConfig();
        var eval = _license.Evaluate(lic, DateOnly.FromDateTime(DateTime.UtcNow));
        vm.Edition = eval.EffectiveEdition.ToString();
        vm.LicenseStatus = eval.Status.ToString();
        vm.LicenseReason = eval.Reason;
        vm.EnabledFeatureCount = eval.Features.Count;

        // Database facts — lightweight counts, each independently guarded.
        vm.DbReachable = await TryAsync(async () => { await _db.Database.ExecuteSqlRawAsync("SELECT 1", ct); return true; }, false);
        if (vm.DbReachable)
        {
            vm.Counts["Projects"] = await CountAsync(() => _db.Projects.CountAsync(ct));
            vm.Counts["Knowledge items"] = await CountAsync(() => _db.KnowledgeItems.CountAsync(ct));
            vm.Counts["Knowledge packs"] = await CountAsync(() => _db.KnowledgePacks.CountAsync(ct));
            vm.Counts["Code symbols"] = await CountAsync(() => _db.CodeSymbols.CountAsync(ct));
            vm.Counts["Imported files"] = await CountAsync(() => _db.ImportedFiles.CountAsync(ct));
            vm.Counts["Chat messages"] = await CountAsync(() => _db.ChatMessages.CountAsync(ct));
            vm.Counts["Audit events"] = await CountAsync(() => _db.AuditEvents.CountAsync(ct));

            // Last import — most recent ingestion job (project name avoided; lightweight columns only).
            vm.LastImport = await TryAsync(async () =>
            {
                var j = await _db.IngestionJobs.AsNoTracking()
                    .OrderByDescending(x => x.CreatedUtc)
                    .Select(x => new { x.FileName, x.Status, x.CreatedUtc, x.CompletedUtc })
                    .FirstOrDefaultAsync(ct);
                return j is null ? null : $"{j.FileName} — {j.Status} ({(j.CompletedUtc ?? j.CreatedUtc):yyyy-MM-dd HH:mm} UTC)";
            }, (string?)null);

            vm.LastAudit = await TryAsync(async () =>
            {
                var a = await _db.AuditEvents.AsNoTracking()
                    .OrderByDescending(x => x.CreatedUtc)
                    .Select(x => new { x.Action, x.CreatedUtc })
                    .FirstOrDefaultAsync(ct);
                return a is null ? null : $"{a.Action} ({a.CreatedUtc:yyyy-MM-dd HH:mm} UTC)";
            }, (string?)null);
        }

        // Disk — free/total for the content-root drive (best effort).
        try
        {
            var root = Path.GetPathRoot(_env.ContentRootPath);
            if (!string.IsNullOrEmpty(root))
            {
                var d = new DriveInfo(root);
                if (d.IsReady)
                {
                    vm.DiskDrive = d.Name;
                    vm.DiskFreeGb = Math.Round(d.AvailableFreeSpace / 1024.0 / 1024 / 1024, 1);
                    vm.DiskTotalGb = Math.Round(d.TotalSize / 1024.0 / 1024 / 1024, 1);
                }
            }
        }
        catch { /* disk tile is best-effort */ }

        // Derived warnings.
        if (!vm.DbReachable) vm.Warnings.Add("Database is not reachable — core pages depend on MSSQL.");
        if (eval.Status is LicenseStatus.GracePeriod) vm.Warnings.Add(eval.Reason);
        if (eval.Status is LicenseStatus.Expired or LicenseStatus.Invalid) vm.Warnings.Add(eval.Reason);
        if (vm.DiskFreeGb is > 0 and < 5) vm.Warnings.Add($"Low disk space: {vm.DiskFreeGb} GB free on {vm.DiskDrive}.");
        if (vm.HealthCheckedUtc is null) vm.Warnings.Add("Optional-service health has not been probed yet (first probe pending).");

        return View(vm);
    }

    private LicenseInfo? LoadLicenseFromConfig()
    {
        var ed = _config["Licensing:Edition"];
        if (string.IsNullOrWhiteSpace(ed) || !Enum.TryParse<Edition>(ed, true, out var edition) || edition == Edition.Community)
            return null; // absent/community => null => Community core
        DateOnly? expiry = DateOnly.TryParse(_config["Licensing:ExpiryUtc"], out var d) ? d : null;
        return new LicenseInfo(edition, _config["Licensing:CustomerId"] ?? "", _config["Licensing:CustomerName"] ?? "", expiry);
    }

    private static async Task<int> CountAsync(Func<Task<int>> f)
    { try { return await f(); } catch { return -1; } }

    private static async Task<T> TryAsync<T>(Func<Task<T>> f, T fallback)
    { try { return await f(); } catch { return fallback; } }

    private static string FormatUptime(TimeSpan t) =>
        t.TotalDays >= 1 ? $"{(int)t.TotalDays}d {t.Hours}h {t.Minutes}m" : $"{t.Hours}h {t.Minutes}m {t.Seconds}s";

    public sealed class SupportVm
    {
        public string Environment { get; set; } = "";
        public string MachineName { get; set; } = "";
        public string Framework { get; set; } = "";
        public string Os { get; set; } = "";
        public string Version { get; set; } = "";
        public string InformationalVersion { get; set; } = "";
        public DateTime ServerTimeUtc { get; set; }
        public string ProcessUptime { get; set; } = "";

        public string Mode { get; set; } = "";
        public bool ChatAvailable { get; set; }
        public DateTime? HealthCheckedUtc { get; set; }
        public Dictionary<string, string> Health { get; } = new();

        public string Edition { get; set; } = "";
        public string LicenseStatus { get; set; } = "";
        public string LicenseReason { get; set; } = "";
        public int EnabledFeatureCount { get; set; }

        public bool DbReachable { get; set; }
        public Dictionary<string, int> Counts { get; } = new();
        public string? LastImport { get; set; }
        public string? LastAudit { get; set; }

        public string? DiskDrive { get; set; }
        public double? DiskFreeGb { get; set; }
        public double? DiskTotalGb { get; set; }

        public List<string> Warnings { get; } = new();
    }
}
