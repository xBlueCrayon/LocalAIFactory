using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

public class AuthTests
{
    [Fact]
    public void PasswordHasher_round_trips_and_rejects_wrong()
    {
        var h = PasswordHasher.Hash("S3cret!pw");
        Assert.True(PasswordHasher.Verify("S3cret!pw", h));
        Assert.False(PasswordHasher.Verify("wrong", h));
        Assert.False(PasswordHasher.Verify("S3cret!pw", null));
    }

    [Fact]
    public void Seeded_admin_authenticates_with_roles()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        var r = svc.Authenticate("admin", "Admin#12345");
        Assert.True(r.Ok);
        Assert.Contains("System Manager", r.Roles);
    }

    [Fact]
    public void Wrong_password_is_rejected()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        Assert.False(svc.Authenticate("admin", "nope").Ok);
        Assert.False(svc.Authenticate("ghost", "x").Ok);
    }

    [Fact]
    public void Login_records_an_audit_event()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        svc.Authenticate("bob", "Bob#12345");
        Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "AppUser" && a.Action == "Login");
    }
}
