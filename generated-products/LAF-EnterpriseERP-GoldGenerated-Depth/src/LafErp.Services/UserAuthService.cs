using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>
/// Real username/password authentication against AppUser (PBKDF2 hashes) with production hardening:
/// failed-login lockout, audited failed/blocked logins, login-success counter reset and last-login stamp.
/// </summary>
public class UserAuthService
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public UserAuthService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public record AuthResult(bool Ok, string Username, IReadOnlyList<string> Roles, string? Error = null, bool LockedOut = false);

    public AuthResult Authenticate(string username, string password)
    {
        var user = _db.AppUsers.Include(u => u.Roles).FirstOrDefault(u => u.Username == username && u.IsActive);
        if (user is null)
        {
            // Do not reveal whether the username exists; still audit the failed attempt.
            _audit.Record("AppUser", 0, "LoginFailed", $"unknown or inactive user '{username}'");
            _db.SaveChanges();
            return new AuthResult(false, username, Array.Empty<string>(), "Invalid username or password.");
        }

        // Locked out?
        if (user.LockoutEndUtc is { } until && until > DateTime.UtcNow)
        {
            _audit.Record("AppUser", user.Id, "LoginBlocked", $"locked until {until:o}");
            _db.SaveChanges();
            return new AuthResult(false, user.Username, Array.Empty<string>(),
                "Account is temporarily locked due to failed login attempts. Try again later.", LockedOut: true);
        }

        if (!PasswordHasher.Verify(password, user.PasswordHash))
        {
            user.FailedLoginCount++;
            _audit.Record("AppUser", user.Id, "LoginFailed", $"attempt {user.FailedLoginCount}/{MaxFailedAttempts}");
            if (user.FailedLoginCount >= MaxFailedAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.Add(LockoutDuration);
                _audit.Record("AppUser", user.Id, "AccountLocked", $"locked for {LockoutDuration.TotalMinutes} min after {user.FailedLoginCount} failures");
            }
            _db.SaveChanges();
            return new AuthResult(false, user.Username, Array.Empty<string>(), "Invalid username or password.");
        }

        // Success: clear failure state, stamp last login, audit.
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        user.LastLoginUtc = DateTime.UtcNow;
        var roleIds = user.Roles.Select(r => r.AppRoleId).ToList();
        var roles = _db.AppRoles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToList();
        _audit.Record("AppUser", user.Id, "Login", null);
        _db.SaveChanges();
        return new AuthResult(true, user.Username, roles);
    }

    /// <summary>Audit a logout for the named user.</summary>
    public void RecordLogout(string username)
    {
        var id = _db.AppUsers.Where(u => u.Username == username).Select(u => u.Id).FirstOrDefault();
        _audit.Record("AppUser", id, "Logout", null);
        _db.SaveChanges();
    }

    /// <summary>Set (or reset) a user's password, enforcing the password policy. Used by admin reset + change flows.</summary>
    public void SetPassword(string username, string newPassword)
    {
        var (ok, error) = PasswordPolicy.Validate(newPassword);
        if (!ok) throw new DomainException(error!);
        var user = _db.AppUsers.FirstOrDefault(u => u.Username == username)
                   ?? throw new DomainException($"User '{username}' not found.");
        user.PasswordHash = PasswordHasher.Hash(newPassword);
        user.MustChangePassword = false;
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        _audit.Record("AppUser", user.Id, "PasswordReset", null);
        _db.SaveChanges();
    }
}
