using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Data.Security;
using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;

namespace LocalAIFactory.Web.Security;

// R2-P0B: wires pilot security. Windows Authentication (Negotiate/NTLM/Kerberos) for the real IIS target;
// a DEVELOPMENT-ONLY dev scheme for local/test use. Deny-by-default at the framework level (every endpoint
// requires an authenticated user). Fails startup if dev-auth is requested outside Development.
public static class SecurityStartup
{
    // Pure, unit-testable guard: dev/test auth must never be usable in a non-Development environment.
    public static void GuardDevAuth(bool isDevelopment, bool devAuthRequested)
    {
        if (devAuthRequested && !isDevelopment)
            throw new InvalidOperationException(
                "Security:UseDevAuth is enabled but the environment is not Development. Dev/test authentication " +
                "must never run in Production. Remove the setting or run in Development.");
    }

    public static IServiceCollection AddPilotSecurity(this IServiceCollection services, IConfiguration config, bool isDevelopment)
    {
        var devAuthRequested = config.GetValue<bool>("Security:UseDevAuth");
        GuardDevAuth(isDevelopment, devAuthRequested);
        var useDev = isDevelopment; // dev scheme ONLY in Development — physically absent in Production

        var auth = services.AddAuthentication(useDev ? DevAuthHandler.Scheme : NegotiateDefaults.AuthenticationScheme);
        if (useDev) auth.AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions, DevAuthHandler>(DevAuthHandler.Scheme, _ => { });
        else auth.AddNegotiate();

        // Deny-by-default: every endpoint requires an authenticated user unless it opts out with [AllowAnonymous].
        services.AddAuthorization(o => o.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build());

        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddSession(o => { o.Cookie.HttpOnly = true; o.IdleTimeout = TimeSpan.FromHours(8); });

        services.AddScoped<IAccessControlService, AccessControlService>();
        services.AddScoped<IAuditTrailService, AuditTrailService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        return services;
    }
}
