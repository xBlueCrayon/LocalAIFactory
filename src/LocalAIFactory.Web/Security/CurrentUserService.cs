using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;

namespace LocalAIFactory.Web.Security;

// R2-P0B: exposes the request's resolved UserAccount (stashed by CurrentUserMiddleware) to controllers/views.
public sealed class CurrentUserService : ICurrentUserService
{
    public const string ItemKey = "R2P0B.CurrentUser";
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) { _http = http; }

    public UserAccount? User => _http.HttpContext?.Items.TryGetValue(ItemKey, out var u) == true ? u as UserAccount : null;
    public string? IpAddress => _http.HttpContext?.Connection.RemoteIpAddress?.ToString();
}
