using LafErp.Core;
using LafErp.Data;
using Microsoft.EntityFrameworkCore;

namespace LafErp.Services;

/// <summary>Real username/password authentication against AppUser (PBKDF2 hashes). Returns the user + roles, or null.</summary>
public class UserAuthService
{
    private readonly ErpDbContext _db;
    private readonly AuditService _audit;
    public UserAuthService(ErpDbContext db, AuditService audit) { _db = db; _audit = audit; }

    public record AuthResult(bool Ok, string Username, IReadOnlyList<string> Roles);

    public AuthResult Authenticate(string username, string password)
    {
        var user = _db.AppUsers.Include(u => u.Roles).FirstOrDefault(u => u.Username == username && u.IsActive);
        if (user is null || !PasswordHasher.Verify(password, user.PasswordHash))
            return new AuthResult(false, username, Array.Empty<string>());
        var roleIds = user.Roles.Select(r => r.AppRoleId).ToList();
        var roles = _db.AppRoles.Where(r => roleIds.Contains(r.Id)).Select(r => r.Name).ToList();
        _audit.Record("AppUser", user.Id, "Login", null);
        _db.SaveChanges();
        return new AuthResult(true, user.Username, roles);
    }
}
