using LafErp.Core;
using LafErp.Services;
using Xunit;

namespace LafErp.Tests;

/// <summary>Production auth hardening: password policy, failed-login lockout, audited security events.</summary>
public class AuthHardeningTests
{
    [Theory]
    [InlineData("short1!", false)]      // too short
    [InlineData("alllowercase1!", false)] // no uppercase
    [InlineData("ALLUPPERCASE1!", false)] // no lowercase
    [InlineData("NoDigitsHere!", false)]  // no digit
    [InlineData("NoSymbol12345", false)]  // no symbol
    [InlineData("Admin#12345", true)]     // strong
    public void Password_policy_enforces_complexity(string pw, bool expected)
        => Assert.Equal(expected, PasswordPolicy.IsStrong(pw));

    [Fact]
    public void Wrong_password_increments_failure_count()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        svc.Authenticate("alice", "wrong-1");
        var u = h.Db.AppUsers.First(x => x.Username == "alice");
        Assert.Equal(1, u.FailedLoginCount);
    }

    [Fact]
    public void Account_locks_after_max_failed_attempts()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        for (var i = 0; i < UserAuthService.MaxFailedAttempts; i++) svc.Authenticate("alice", "bad");
        var u = h.Db.AppUsers.First(x => x.Username == "alice");
        Assert.NotNull(u.LockoutEndUtc);
        // Even the correct password is refused while locked.
        var r = svc.Authenticate("alice", "Alice#12345");
        Assert.False(r.Ok);
        Assert.True(r.LockedOut);
    }

    [Fact]
    public void Successful_login_resets_failure_count_and_stamps_last_login()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        svc.Authenticate("bob", "bad");
        Assert.Equal(1, h.Db.AppUsers.First(x => x.Username == "bob").FailedLoginCount);
        var r = svc.Authenticate("bob", "Bob#12345");
        Assert.True(r.Ok);
        var u = h.Db.AppUsers.First(x => x.Username == "bob");
        Assert.Equal(0, u.FailedLoginCount);
        Assert.NotNull(u.LastLoginUtc);
    }

    [Fact]
    public void Failed_login_is_audited()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        svc.Authenticate("alice", "bad");
        Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "AppUser" && a.Action == "LoginFailed");
    }

    [Fact]
    public void Lockout_is_audited()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        for (var i = 0; i < UserAuthService.MaxFailedAttempts; i++) svc.Authenticate("alice", "bad");
        Assert.Contains(h.Db.AuditEvents, a => a.Action == "AccountLocked");
    }

    [Fact]
    public void Logout_is_audited()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        svc.RecordLogout("admin");
        Assert.Contains(h.Db.AuditEvents, a => a.EntityType == "AppUser" && a.Action == "Logout");
    }

    [Fact]
    public void Admin_password_reset_enforces_policy_and_rehashes()
    {
        using var h = new TestHost();
        var svc = new UserAuthService(h.Db, h.Audit);
        Assert.Throws<DomainException>(() => svc.SetPassword("alice", "weak"));
        svc.SetPassword("alice", "Str0ng#Reset1");
        Assert.True(svc.Authenticate("alice", "Str0ng#Reset1").Ok);
        Assert.Contains(h.Db.AuditEvents, a => a.Action == "PasswordReset");
    }
}
