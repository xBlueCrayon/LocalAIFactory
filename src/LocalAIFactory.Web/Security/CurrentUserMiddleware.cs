using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Web.Security;

// R2-P0B: after authentication, resolve the Windows principal to a UserAccount (auto-provisioning a deny-by-
// default Viewer on first sight; the configured bootstrap-admin becomes Admin), stash it for the request, and
// audit the authenticated session. A disabled user is authenticated but treated as denied downstream.
public sealed class CurrentUserMiddleware
{
    private readonly RequestDelegate _next;
    public CurrentUserMiddleware(RequestDelegate next) { _next = next; }

    public async Task InvokeAsync(HttpContext ctx, IAccessControlService access, IAuditTrailService audit, IConfiguration config)
    {
        var identity = ctx.User?.Identity;
        if (identity?.IsAuthenticated == true && !string.IsNullOrWhiteSpace(identity.Name))
        {
            var bootstrap = config["Security:BootstrapAdmin"];
            var user = await access.ResolveUserAsync(identity.Name!, sid: null, displayName: identity.Name, bootstrap, ctx.RequestAborted);
            ctx.Items[CurrentUserService.ItemKey] = user;

            // Audit the session once per browser session (avoid a row per request).
            if (ctx.Session?.IsAvailable == true && ctx.Session.GetString("R2P0B.Audited") is null)
            {
                try
                {
                    await audit.WriteAsync(user, ctx.Connection.RemoteIpAddress?.ToString(),
                        user.Enabled ? AuditEventType.AuthSuccess : AuditEventType.AuthDenied,
                        user.Enabled ? "Authenticated" : "Authenticated but disabled");
                    ctx.Session.SetString("R2P0B.Audited", "1");
                }
                catch { /* auditing must never break the request */ }
            }
        }
        await _next(ctx);
    }
}
