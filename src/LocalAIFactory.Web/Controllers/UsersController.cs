using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

// R2-P0B: Admin-only user & project-access management. Every change is audited.
public sealed class UsersController : SecuredController
{
    private readonly AppDbContext _db;
    public UsersController(AppDbContext db, ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _db = db; }

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (await RequireAdminAsync("manage users", ct) is { } denied) return denied;
        var users = await _db.UserAccounts.OrderBy(u => u.WindowsIdentity).ToListAsync(ct);
        ViewBag.Projects = await _db.Projects.OrderBy(p => p.Name).ToListAsync(ct);
        ViewBag.Access = (await _db.ProjectAccesses.ToListAsync(ct))
            .GroupBy(a => a.UserAccountId).ToDictionary(g => g.Key, g => g.Select(a => a.ProjectId).ToHashSet());
        return View(users);
    }

    [HttpPost]
    public async Task<IActionResult> SetRole(int userId, UserRole role, CancellationToken ct)
    {
        if (await RequireAdminAsync("set role", ct) is { } denied) return denied;
        var u = await _db.UserAccounts.FindAsync(new object?[] { userId }, ct);
        if (u is not null && u.Id != CurrentUser?.Id) // an admin cannot demote themselves accidentally
        {
            u.Role = role;
            await _db.SaveChangesAsync(ct);
            await AuditAsync(AuditEventType.RoleChanged, $"Role set to {role}", "User", u.WindowsIdentity, detail: $"role={role}", ct: ct);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> SetEnabled(int userId, bool enabled, CancellationToken ct)
    {
        if (await RequireAdminAsync("enable/disable user", ct) is { } denied) return denied;
        var u = await _db.UserAccounts.FindAsync(new object?[] { userId }, ct);
        if (u is not null && u.Id != CurrentUser?.Id)
        {
            u.Enabled = enabled;
            await _db.SaveChangesAsync(ct);
            if (!enabled) await AuditAsync(AuditEventType.UserDisabled, "User disabled", "User", u.WindowsIdentity, ct: ct);
            else await AuditAsync(AuditEventType.RoleChanged, "User enabled", "User", u.WindowsIdentity, ct: ct);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> GrantAccess(int userId, int projectId, CancellationToken ct)
    {
        if (await RequireAdminAsync("grant project access", ct) is { } denied) return denied;
        if (!await _db.ProjectAccesses.AnyAsync(a => a.UserAccountId == userId && a.ProjectId == projectId, ct))
        {
            _db.ProjectAccesses.Add(new ProjectAccess { UserAccountId = userId, ProjectId = projectId, AccessLevel = AccessLevel.Read, GrantedByUserId = CurrentUser?.Id });
            await _db.SaveChangesAsync(ct);
            await AuditAsync(AuditEventType.AccessGranted, "Project access granted", "User", userId.ToString(), projectId, ct: ct);
        }
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> RevokeAccess(int userId, int projectId, CancellationToken ct)
    {
        if (await RequireAdminAsync("revoke project access", ct) is { } denied) return denied;
        var row = await _db.ProjectAccesses.FirstOrDefaultAsync(a => a.UserAccountId == userId && a.ProjectId == projectId, ct);
        if (row is not null)
        {
            _db.ProjectAccesses.Remove(row);
            await _db.SaveChangesAsync(ct);
            await AuditAsync(AuditEventType.AccessRevoked, "Project access revoked", "User", userId.ToString(), projectId, ct: ct);
        }
        return RedirectToAction(nameof(Index));
    }
}
