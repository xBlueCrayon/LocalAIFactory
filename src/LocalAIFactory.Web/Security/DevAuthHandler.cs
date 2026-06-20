using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Web.Security;

// R2-P0B: DEVELOPMENT-ONLY authentication. Authenticates as a configured identity (Security:DevIdentity) or an
// X-Dev-User header so different roles can be exercised locally and in tests. This handler is ONLY registered
// when the environment is Development; SecurityStartup.GuardDevAuth additionally fails startup if dev-auth is
// requested in a non-Development environment. It must never run in Production.
public sealed class DevAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string Scheme = "Dev";
    private readonly IConfiguration _config;

    public DevAuthHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, IConfiguration config) : base(options, logger, encoder) { _config = config; }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var name = Context.Request.Headers["X-Dev-User"].FirstOrDefault()
                   ?? _config["Security:DevIdentity"] ?? "DEV\\developer";
        var id = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, name) }, Scheme);
        return Task.FromResult(AuthenticateResult.Success(
            new AuthenticationTicket(new ClaimsPrincipal(id), Scheme)));
    }
}
