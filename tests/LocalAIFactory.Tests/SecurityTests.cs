using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Security;
using LocalAIFactory.Web.Controllers;
using LocalAIFactory.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class SecurityTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // A test current-user that returns a fixed account (controllers depend only on this interface).
    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public UserAccount? User { get; init; }
        public string? IpAddress => "127.0.0.1";
    }

    // A minimal controller to exercise SecuredController's server-side enforcement helpers.
    private sealed class TestSecured : SecuredController
    {
        public TestSecured(ICurrentUserService me, IAccessControlService a, IAuditTrailService au) : base(me, a, au)
            => ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        public Task<IActionResult?> Admin(string action) => RequireAdminAsync(action);
        public Task<IActionResult?> Project(int id) => RequireProjectAsync(id, "test");
    }

    private static (AccessControlService access, AuditTrailService audit) Services(AppDbContext db) => (new(db), new(db));

    private static async Task<UserAccount> User(AppDbContext db, UserRole role, bool enabled = true)
    {
        var u = new UserAccount { WindowsIdentity = $"DOM\\{role}-{Guid.NewGuid():N}", Role = role, Enabled = enabled };
        db.UserAccounts.Add(u); await db.SaveChangesAsync(); return u;
    }

    // ---- requirement 13: bootstrap admin ----
    [Fact]
    public async Task Bootstrap_admin_is_provisioned_as_admin_others_as_viewer()
    {
        var db = NewDb(); var (access, _) = Services(db);
        var admin = await access.ResolveUserAsync("DOM\\boss", null, "Boss", "DOM\\boss");
        var other = await access.ResolveUserAsync("DOM\\joe", null, "Joe", "DOM\\boss");
        Assert.Equal(UserRole.Admin, admin.Role);
        Assert.Equal(UserRole.Viewer, other.Role); // requirement 9: deny-by-default
    }

    // ---- requirement 9: new users have NO project access ----
    [Fact]
    public async Task New_viewer_has_no_project_access()
    {
        var db = NewDb(); var (access, _) = Services(db);
        var u = await access.ResolveUserAsync("DOM\\new", null, null, null);
        db.Projects.Add(new Project { Name = "P", Code = "P" }); await db.SaveChangesAsync();
        var pid = (await db.Projects.FirstAsync()).Id;
        Assert.False(await access.CanAccessProjectAsync(u, pid));
        Assert.Empty(await access.AccessibleProjectIdsAsync(u));
    }

    // ---- requirements 2/3/4: import gating (server-side) ----
    [Fact]
    public void Role_gates_import_admin_only()
    {
        Assert.True(IAccessControlService.CanImport(new UserAccount { Role = UserRole.Admin, Enabled = true }));
        Assert.False(IAccessControlService.CanImport(new UserAccount { Role = UserRole.Analyst, Enabled = true }));
        Assert.False(IAccessControlService.CanImport(new UserAccount { Role = UserRole.Viewer, Enabled = true }));
        Assert.False(IAccessControlService.CanImport(new UserAccount { Role = UserRole.Admin, Enabled = false })); // disabled
    }

    [Fact]
    public async Task Non_admin_is_forbidden_admin_action_and_denial_is_audited()
    {
        var db = NewDb(); var (access, audit) = Services(db);
        var analyst = await User(db, UserRole.Analyst);
        var c = new TestSecured(new FakeCurrentUser { User = analyst }, access, audit);
        var result = await c.Admin("import");
        Assert.IsType<ViewResult>(result); // AccessDenied view (403)
        Assert.Equal(403, c.Response.StatusCode);
        Assert.True(await db.AuditEvents.AnyAsync(e => e.EventType == AuditEventType.AuthDenied)); // requirement audit
    }

    [Fact]
    public async Task Admin_passes_admin_gate()
    {
        var db = NewDb(); var (access, audit) = Services(db);
        var admin = await User(db, UserRole.Admin);
        var c = new TestSecured(new FakeCurrentUser { User = admin }, access, audit);
        Assert.Null(await c.Admin("import")); // null == allowed
    }

    // ---- requirements 5/6/8: project access enforcement + direct-URL block ----
    [Fact]
    public async Task Analyst_blocked_from_ungranted_project_but_allowed_when_granted()
    {
        var db = NewDb(); var (access, audit) = Services(db);
        var analyst = await User(db, UserRole.Analyst);
        db.Projects.Add(new Project { Name = "Secret", Code = "S" }); await db.SaveChangesAsync();
        var pid = (await db.Projects.FirstAsync()).Id;
        var c = new TestSecured(new FakeCurrentUser { User = analyst }, access, audit);

        Assert.IsType<ViewResult>(await c.Project(pid)); // 403 by direct URL
        Assert.Equal(403, c.Response.StatusCode);

        db.ProjectAccesses.Add(new ProjectAccess { UserAccountId = analyst.Id, ProjectId = pid }); await db.SaveChangesAsync();
        var c2 = new TestSecured(new FakeCurrentUser { User = analyst }, access, audit);
        Assert.Null(await c2.Project(pid)); // allowed once granted
    }

    // ---- requirement 7: project list hides ungranted ----
    [Fact]
    public async Task Accessible_project_ids_hides_ungranted_for_non_admin_shows_all_for_admin()
    {
        var db = NewDb(); var (access, _) = Services(db);
        db.Projects.AddRange(new Project { Name = "A", Code = "A" }, new Project { Name = "B", Code = "B" });
        await db.SaveChangesAsync();
        var ids = await db.Projects.Select(p => p.Id).ToListAsync();
        var analyst = await User(db, UserRole.Analyst);
        db.ProjectAccesses.Add(new ProjectAccess { UserAccountId = analyst.Id, ProjectId = ids[0] }); await db.SaveChangesAsync();
        var admin = await User(db, UserRole.Admin);

        Assert.Equal(new HashSet<int> { ids[0] }, await access.AccessibleProjectIdsAsync(analyst));
        Assert.Equal(ids.ToHashSet(), await access.AccessibleProjectIdsAsync(admin));
    }

    // ---- requirement 12: disabled user is denied everything ----
    [Fact]
    public async Task Disabled_user_is_denied()
    {
        var db = NewDb(); var (access, _) = Services(db);
        db.Projects.Add(new Project { Name = "P", Code = "P" }); await db.SaveChangesAsync();
        var pid = (await db.Projects.FirstAsync()).Id;
        var admin = await User(db, UserRole.Admin, enabled: false);
        db.ProjectAccesses.Add(new ProjectAccess { UserAccountId = admin.Id, ProjectId = pid }); await db.SaveChangesAsync();
        Assert.False(await access.CanAccessProjectAsync(admin, pid));
        Assert.False(IAccessControlService.CanImport(admin));
    }

    // ---- requirements 10/11: audit events for access grant / role change ----
    [Fact]
    public async Task Audit_records_who_what_when_and_project()
    {
        var db = NewDb(); var (_, audit) = Services(db);
        var actor = await User(db, UserRole.Admin);
        await audit.WriteAsync(actor, "10.0.0.1", AuditEventType.AccessGranted, "granted", "User", "5", projectId: 7);
        await audit.WriteAsync(actor, "10.0.0.1", AuditEventType.RoleChanged, "role->Admin", "User", "5");
        var ev = await db.AuditEvents.OrderBy(e => e.Id).ToListAsync();
        Assert.Equal(2, ev.Count);
        Assert.Equal(actor.WindowsIdentity, ev[0].WindowsIdentity);
        Assert.Equal(7, ev[0].ProjectId);
        Assert.Contains(ev, e => e.EventType == AuditEventType.RoleChanged);
    }

    // ---- IDOR guard: a symbol id from another project must not leak via a granted projectId ----
    [Fact]
    public async Task Symbol_detail_does_not_leak_across_projects()
    {
        var db = NewDb(); var (access, audit) = Services(db);
        db.Projects.AddRange(new Project { Name = "A", Code = "A" }, new Project { Name = "B", Code = "B" });
        await db.SaveChangesAsync();
        var pa = (await db.Projects.SingleAsync(p => p.Code == "A")).Id;
        var pb = (await db.Projects.SingleAsync(p => p.Code == "B")).Id;
        // a symbol that belongs to project B
        db.CodeSymbols.Add(new CodeSymbol { ProjectId = pb, FullName = "B.Secret", Name = "Secret", NormalizedKey = "b.secret", SourceLocusKey = "x" });
        await db.SaveChangesAsync();
        var bSym = await db.CodeSymbols.SingleAsync(s => s.FullName == "B.Secret");

        var analyst = await User(db, UserRole.Analyst);
        db.ProjectAccesses.Add(new ProjectAccess { UserAccountId = analyst.Id, ProjectId = pa }); // access to A only
        await db.SaveChangesAsync();

        var ctrl = new GraphController(db, new LocalAIFactory.Rag.Retrieval.StructuralRetrievalService(db),
            new FakeCurrentUser { User = analyst }, access, audit)
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        // Pass an authorized projectId (A) but a symbol id from B — must NOT render B's symbol.
        var result = await ctrl.Symbol(pa, bSym.Id, default);
        Assert.IsType<RedirectToActionResult>(result); // not a ViewResult exposing B.Secret
    }

    // ---- requirement 14: dev/test auth cannot run in Production ----
    [Fact]
    public void Dev_auth_guard_throws_outside_development()
    {
        SecurityStartup.GuardDevAuth(isDevelopment: true, devAuthRequested: true);   // ok
        SecurityStartup.GuardDevAuth(isDevelopment: false, devAuthRequested: false); // ok
        Assert.Throws<InvalidOperationException>(() =>
            SecurityStartup.GuardDevAuth(isDevelopment: false, devAuthRequested: true)); // forbidden
    }
}
