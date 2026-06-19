using LocalAIFactory.Agent.Chat;
using LocalAIFactory.Agent.Execution;
using LocalAIFactory.Agent.Providers;
using LocalAIFactory.Agent.Routing;
using LocalAIFactory.Core.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIFactory.Agent;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalAIFactoryAgent(this IServiceCollection services)
    {
        services.AddHttpClient();

        services.AddScoped<IChatModelProvider, OllamaChatProvider>();
        services.AddScoped<IChatModelProvider, OpenAiChatProvider>();
        services.AddScoped<IChatModelProvider, OpenAiCompatibleChatProvider>();
        services.AddScoped<IChatModelProvider, ClaudeChatProvider>();

        services.AddScoped<IModelRouter, ModelRouter>();
        services.AddScoped<ITaskProfileResolver, TaskProfileResolver>();
        services.AddScoped<IModelExecutionService, ModelExecutionService>();
        services.AddScoped<IChatOrchestrator, ChatOrchestrator>();

        return services;
    }
}
