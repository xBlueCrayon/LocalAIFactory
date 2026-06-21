using LafErp.Services;

namespace LafErp.Web;

/// <summary>Resolves the acting identity from a dev cookie. Defaults to admin/System Manager.</summary>
public sealed class HttpCurrentUser : ICurrentUser
{
    public string Username { get; }
    public IReadOnlyList<string> Roles { get; }

    public HttpCurrentUser(IHttpContextAccessor accessor)
    {
        var ctx = accessor.HttpContext;
        Username = ctx?.Request.Cookies["erp_user"] ?? "admin";
        var roles = ctx?.Request.Cookies["erp_roles"];
        Roles = string.IsNullOrWhiteSpace(roles)
            ? new[] { "System Manager", "Sales User", "Purchase User", "Accounts User", "Accounts Manager", "Stock User" }
            : roles.Split('|', StringSplitOptions.RemoveEmptyEntries);
    }
}
