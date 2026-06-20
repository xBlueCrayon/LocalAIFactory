using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Web.Controllers;

// R2-P0B: Admin-only append-only audit trail. Answers who did what, when, to which project, and whether
// access was denied.
public sealed class AuditController : SecuredController
{
    private readonly AppDbContext _db;
    public AuditController(AppDbContext db, ICurrentUserService me, IAccessControlService access, IAuditTrailService audit)
        : base(me, access, audit) { _db = db; }

    public async Task<IActionResult> Index(string? user, string? type, int take = 200, CancellationToken ct = default)
    {
        if (await RequireAdminAsync("view audit trail", ct) is { } denied) return denied;
        var q = _db.AuditEvents.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(user)) q = q.Where(e => e.WindowsIdentity != null && e.WindowsIdentity.Contains(user));
        if (!string.IsNullOrWhiteSpace(type) && Enum.TryParse<LocalAIFactory.Core.Enums.AuditEventType>(type, out var t)) q = q.Where(e => e.EventType == t);
        var events = await q.OrderByDescending(e => e.Id).Take(Math.Clamp(take, 1, 1000)).ToListAsync(ct);
        ViewBag.User = user; ViewBag.Type = type;
        return View(events);
    }
}
