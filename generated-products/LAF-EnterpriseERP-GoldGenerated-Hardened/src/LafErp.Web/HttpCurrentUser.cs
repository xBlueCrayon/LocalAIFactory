using System.Security.Claims;
using LafErp.Services;

namespace LafErp.Web;

/// <summary>
/// Resolves the acting identity. Prefers the REAL authenticated principal (cookie auth via /Account/Login);
/// falls back to a dev cookie / default identity when not signed in (keeps the deterministic test suite green
/// and supports first-run). Production can enforce login with the Auth:RequireLogin flag.
/// </summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    public string Username { get; }
    public IReadOnlyList<string> Roles { get; }

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        var ctx = accessor.HttpContext;
        var principal = ctx?.User;
        if (principal?.Identity?.IsAuthenticated == true)
        {
            Username = principal.Identity!.Name ?? "user";
            Roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();
            return;
        }
        Username = ctx?.Request.Cookies["erp_user"] ?? "admin";
        var roles = ctx?.Request.Cookies["erp_roles"];
        Roles = string.IsNullOrWhiteSpace(roles)
            ? new[] { "System Manager", "Sales User", "Purchase User", "Accounts User", "Accounts Manager", "Stock User" }
            : roles.Split('|', StringSplitOptions.RemoveEmptyEntries);
    }
}
