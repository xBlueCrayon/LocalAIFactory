using System.Globalization;
using LocalAIFactory.Agent;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion;
using LocalAIFactory.Rag;
using LocalAIFactory.Terminal;
using LocalAIFactory.Web.Hosted;
using LocalAIFactory.Web.Middleware;
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

// Phase 1.1: support large project ZIP uploads (up to 1 GB) across Kestrel, IIS and form parsing.
const long MaxUploadBytes = 1_073_741_824; // 1 GB
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

// Application modules.
builder.Services.AddLocalAIFactoryData(builder.Configuration);
builder.Services.AddLocalAIFactoryRag(builder.Configuration);
builder.Services.AddLocalAIFactoryAgent();
builder.Services.AddLocalAIFactoryIngestion();
builder.Services.AddLocalAIFactoryWorkspaces(builder.Configuration);
builder.Services.AddLocalAIFactoryTerminal();

// Dashboard query service (SQL-Server-friendly counts + structured logging).
builder.Services.AddScoped<DashboardService>();

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
        logger.LogInformation("Database migrated and seeded.");
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
app.MapControllerRoute(name: "default", pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
