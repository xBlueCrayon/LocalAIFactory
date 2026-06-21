using System.Globalization;
using LocalAIFactory.Agent;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Backbone;
using LocalAIFactory.Data.Identity;
using LocalAIFactory.Ingestion;
using LocalAIFactory.Rag;
using LocalAIFactory.Terminal;
using LocalAIFactory.Web.Hosted;
using LocalAIFactory.Web.Middleware;
using LocalAIFactory.Web.Security;
using LocalAIFactory.Web.Services;
using LocalAIFactory.Workspaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

// INFRA-001: globalization-invariant *mode* is disabled (SqlClient requires it). We still pin the
// default culture to invariant so formatting, parsing, and sorting stay deterministic and locale-
// independent across environments — important for a banking estate.
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient();

// KE-007: import ceiling for very large project archives (large BDM ZIPs) across Kestrel, IIS and form
// parsing. Configurable via Import:MaxUploadBytes; default 4 GB. Extraction is disk-streamed; jobs resume.
long MaxUploadBytes = long.TryParse(builder.Configuration["Import:MaxUploadBytes"], out var cfgMaxUpload)
    ? cfgMaxUpload : 4_294_967_296L; // 4 GB
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = MaxUploadBytes;
    o.ValueLengthLimit = int.MaxValue;
    o.MultipartHeadersLengthLimit = int.MaxValue;
});
builder.Services.Configure<IISServerOptions>(o => o.MaxRequestBodySize = MaxUploadBytes);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = MaxUploadBytes);

// Persist Data Protection keys so encrypted API keys survive restarts.
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysDir);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("LocalAIFactory");

// R2-P0B: pilot security — Windows auth (IIS) in production, dev-only auth in Development; deny-by-default.
// Fails fast if dev-auth is misconfigured for a non-Development environment.
builder.Services.AddPilotSecurity(builder.Configuration, builder.Environment.IsDevelopment());

// Application modules.
builder.Services.AddLocalAIFactoryData(builder.Configuration);
builder.Services.AddLocalAIFactoryRag(builder.Configuration);
builder.Services.AddLocalAIFactoryAgent();
builder.Services.AddLocalAIFactoryIngestion();
builder.Services.AddLocalAIFactoryWorkspaces(builder.Configuration);
builder.Services.AddLocalAIFactoryTerminal();

// Dashboard query service (SQL-Server-friendly counts + structured logging).
builder.Services.AddScoped<DashboardService>();

// R2-ACC-20X: deterministic edition/license evaluation (demo-safe — no DRM, degrades to Community core).
builder.Services.AddSingleton<LocalAIFactory.Core.Licensing.ILicenseVerifier, LocalAIFactory.Core.Licensing.LicenseVerifier>();

builder.Services.AddHostedService<IngestionBackgroundService>();
builder.Services.AddHostedService<HealthMonitorService>();

var app = builder.Build();

// Apply migrations and seed baseline data (projects, one local model, task profiles).
using (var scope = app.Services.CreateScope())
{
    var sp = scope.ServiceProvider;
    var logger = sp.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");
    try
    {
        var db = sp.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
        await DbSeeder.SeedAsync(db);
        // KE-003: stamp pre-existing knowledge with content hash + v1 version + provenance (idempotent).
        await KnowledgeBackboneBackfill.RunAsync(db,
            sp.GetRequiredService<IContentHasher>(),
            sp.GetRequiredService<IInstanceContext>());
        // KE-004: assign source-locus keys to pre-existing file-linked items (idempotent).
        await SourceLocusBackfill.RunAsync(db);
        // KE-006: one-time quality-band backfill (guarded by a marker; batched, idempotent).
        if (!await db.SystemSettings.AnyAsync(s => s.Key == "KE006.QualityBackfilled"))
        {
            await sp.GetRequiredService<IQualityService>().RecomputeAllAsync(null);
            db.SystemSettings.Add(new SystemSetting { Key = "KE006.QualityBackfilled", Value = "true" });
            await db.SaveChangesAsync();
        }
        // R2-ACC-B1/FINAL: install the included knowledge base — ALL shipped packs (idempotent; best-effort,
        // never blocks startup). Set KnowledgePacks:InstallAllAtStartup=false to seed only the base pack.
        try
        {
            var installer = sp.GetRequiredService<IKnowledgePackInstaller>();
            var installAll = builder.Configuration.GetValue("KnowledgePacks:InstallAllAtStartup", true);
            var packPaths = installAll
                ? LocalAIFactory.Web.Services.KnowledgePackLocator.FindAllPacks(builder.Configuration, builder.Environment.ContentRootPath)
                : new[] { LocalAIFactory.Web.Services.KnowledgePackLocator.FindBaseV1(builder.Configuration, builder.Environment.ContentRootPath) }
                    .Where(p => p is not null).Select(p => p!).ToList();

            if (packPaths.Count == 0) logger.LogInformation("No knowledge packs found to install (optional).");
            foreach (var packPath in packPaths)
            {
                var res = await installer.InstallAsync(packPath, "system (startup)");
                if (res.Success)
                    logger.LogInformation("Knowledge pack '{Name}' v{Version}: {Created} created, {Updated} updated, {Unchanged} unchanged, {Proposed} proposed (current={Cur}).",
                        res.Name, res.Version, res.Created, res.Updated, res.Unchanged, res.ProposedRevisions, res.AlreadyCurrent);
                else
                    logger.LogWarning("Knowledge pack at '{Path}' reported {N} error(s); first: {E}", packPath, res.Errors.Count, res.Errors.FirstOrDefault());
            }
        }
        catch (Exception ex) { logger.LogError(ex, "Knowledge pack install failed (non-fatal)."); }
        logger.LogInformation("Database migrated, seeded, and knowledge backbone backfilled.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed. Check the connection string and that SQL Server/LocalDB is running.");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();
app.UseMiddleware<RequestTimingMiddleware>();
app.UseRouting();

// R2-P0B: authenticate → authorize → resolve/audit the current user. Deny-by-default; project access enforced
// server-side in controllers.
app.UseAuthentication();
app.UseAuthorization();
app.UseSession();
app.UseMiddleware<LocalAIFactory.Web.Security.CurrentUserMiddleware>();

app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// R2-P0B: expose the implicit Program class so integration tests can host the app with a test auth scheme.
public partial class Program { }
