using LafErp.Core;
using LafErp.Data;
using LafErp.Services;
using LafErp.Web;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database: SQL Server when a connection string is supplied, otherwise a local SQLite file so the
// generated ERP runs on any host without external services (MSSQL-only is fully supported too).
var cs = builder.Configuration.GetConnectionString("Default");
builder.Services.AddDbContext<ErpDbContext>(o =>
{
    if (!string.IsNullOrWhiteSpace(cs))
        o.UseSqlServer(cs);
    else
        o.UseSqlite("Data Source=laferp.db");
});

// Dev authentication: identity comes from a cookie chosen on the login page. This is an explicit DEV
// auth mode for the proof; production would bind ICurrentUser to the Windows/SSO identity.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
builder.Services.AddErpServices();
builder.Services.AddControllersWithViews();
builder.Services.AddAuthentication("LafErpCookie").AddCookie("LafErpCookie", o =>
{
    o.LoginPath = "/Account/Login";
    o.AccessDeniedPath = "/Account/Login";
    // Production-hardened cookie: HttpOnly, SameSite, HTTPS-aware, sliding session timeout.
    o.Cookie.HttpOnly = true;
    o.Cookie.SameSite = SameSiteMode.Lax;
    o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest; // becomes Always behind TLS
    o.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    o.SlidingExpiration = true;
});
// Anti-forgery for all POST forms (login/logout already validate the token).
builder.Services.AddAntiforgery(o => { o.Cookie.HttpOnly = true; o.Cookie.SameSite = SameSiteMode.Lax; });

// Utility: `dotnet run -- schema` prints the SQL Server DDL for the model and exits (no DB connection needed).
if (args.Length > 0 && args[0] == "schema")
{
    using var sp = builder.Services.BuildServiceProvider();
    using var scope0 = sp.CreateScope();
    var ctx = scope0.ServiceProvider.GetRequiredService<ErpDbContext>();
    Console.Write(ctx.Database.GenerateCreateScript());
    return;
}

var app = builder.Build();

// Create schema + seed on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ErpDbContext>();
    // SQL Server gets a real, committed migration history; the portable SQLite mode uses EnsureCreated so
    // tests and zero-config local runs stay fast (relational migrations target SQL Server).
    if (db.Database.IsSqlServer())
        db.Database.Migrate();
    else
        db.Database.EnsureCreated();
    var seed = DataSeeder.Seed(db);
    // Startup has no HttpContext, so ICurrentUser resolves to admin with all roles -> demo posts auto-approve.
    DemoData.Post(db,
        seed,
        scope.ServiceProvider.GetRequiredService<PurchaseService>(),
        scope.ServiceProvider.GetRequiredService<SalesService>(),
        scope.ServiceProvider.GetRequiredService<PaymentService>());
}

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllerRoute("default", "{controller=Home}/{action=Index}/{id?}");
ApiEndpoints.Map(app);

app.Run();

public partial class Program { } // expose for WebApplicationFactory integration tests
