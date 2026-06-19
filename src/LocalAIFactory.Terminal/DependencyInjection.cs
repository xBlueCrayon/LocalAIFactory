using LocalAIFactory.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIFactory.Terminal;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalAIFactoryTerminal(this IServiceCollection services)
    {
        services.AddSingleton<ICommandPolicyService, CommandPolicyService>();
        services.AddSingleton<ITerminalService, TerminalService>();
        return services;
    }
}
