namespace LocalAIFactory.Reasoning.Tests;

/// <summary>A small but representative C# corpus (controller, service, dbcontext, entity, test) for graph tests.</summary>
internal static class SampleCode
{
    public static IEnumerable<(string Path, string Content)> Files()
    {
        yield return ("src/LafErp.Web/Controllers/AccountController.cs", """
            using LafErp.Services;
            namespace LafErp.Web.Controllers;
            public class AccountController
            {
                private readonly UserAuthService _auth;
                public AccountController(UserAuthService auth) { _auth = auth; }
                public void Login(string username, string password) { }
                public void Logout() { }
            }
            """);

        yield return ("src/LafErp.Services/UserAuthService.cs", """
            using LafErp.Core;
            namespace LafErp.Services;
            public class UserAuthService
            {
                private readonly ErpDbContext _db;
                private readonly AuditService _audit;
                public UserAuthService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }
                public bool Authenticate(string u, string p) { return true; }
            }
            """);

        yield return ("src/LafErp.Services/AuditService.cs", """
            namespace LafErp.Services;
            public class AuditService
            {
                public void Record(string entity, int id, string action) { }
            }
            """);

        yield return ("src/LafErp.Data/ErpDbContext.cs", """
            using Microsoft.EntityFrameworkCore;
            using LafErp.Core;
            namespace LafErp.Data;
            public class ErpDbContext : DbContext
            {
                public DbSet<AppUser> AppUsers { get; set; }
                public DbSet<AuditEvent> AuditEvents { get; set; }
            }
            """);

        yield return ("src/LafErp.Core/PlatformEntities.cs", """
            namespace LafErp.Core;
            public abstract class EntityBase { public int Id { get; set; } }
            public class AppUser : EntityBase { public string Username { get; set; } public string? PasswordHash { get; set; } }
            public class AuditEvent : EntityBase { public string Action { get; set; } }
            """);

        yield return ("tests/LafErp.Tests/AuthTests.cs", """
            using LafErp.Services;
            namespace LafErp.Tests;
            public class AuthTests
            {
                private readonly UserAuthService _svc = new(null, null);
                public void Seeded_admin_authenticates()
                {
                    var ok = _svc.Authenticate("admin", "x");
                }
            }
            """);
    }
}
