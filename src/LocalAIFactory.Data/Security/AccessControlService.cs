using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Security;

// R2-P0B: server-side access control. Deny-by-default: a new Windows user is provisioned as a Viewer with NO
// project access; only the configured bootstrap-admin is provisioned as Admin. All project-scoped decisions
// route through here so they are enforced consistently and tested without a web host.
public sealed class AccessControlService : IAccessControlService
{
    private readonly AppDbContext _db;
    public AccessControlService(AppDbContext db) { _db = db; }

    public async Task<UserAccount> ResolveUserAsync(string windowsIdentity, string? sid, string? displayName, string? bootstrapAdmin, CancellationToken ct = default)
    {
        var user = await _db.UserAccounts.FirstOrDefaultAsync(u => u.WindowsIdentity == windowsIdentity, ct);
        var isBootstrap = !string.IsNullOrWhiteSpace(bootstrapAdmin)
            && string.Equals(windowsIdentity, bootstrapAdmin, StringComparison.OrdinalIgnoreCase);
        if (user is null)
        {
            user = new UserAccount
            {
                WindowsIdentity = windowsIdentity, Sid = sid,
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? windowsIdentity : displayName!,
                Role = isBootstrap ? UserRole.Admin : UserRole.Viewer, // deny-by-default otherwise
                Enabled = true, LastSeenUtc = DateTime.UtcNow
            };
            _db.UserAccounts.Add(user);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            // Keep the bootstrap admin an admin (recovery), refresh last-seen.
            if (isBootstrap && user.Role != UserRole.Admin) user.Role = UserRole.Admin;
            user.LastSeenUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
        return user;
    }

    public async Task<bool> CanAccessProjectAsync(UserAccount user, int projectId, CancellationToken ct = default)
    {
        if (!user.Enabled) return false;
        if (user.Role == UserRole.Admin) return true;
        return await _db.ProjectAccesses.AnyAsync(p => p.UserAccountId == user.Id && p.ProjectId == projectId
            && p.AccessLevel != AccessLevel.None, ct);
    }

    public async Task<HashSet<int>> AccessibleProjectIdsAsync(UserAccount user, CancellationToken ct = default)
    {
        if (!user.Enabled) return new HashSet<int>();
        if (user.Role == UserRole.Admin)
            return (await _db.Projects.Select(p => p.Id).ToListAsync(ct)).ToHashSet();
        return (await _db.ProjectAccesses.Where(p => p.UserAccountId == user.Id && p.AccessLevel != AccessLevel.None)
            .Select(p => p.ProjectId).ToListAsync(ct)).ToHashSet();
    }
}
