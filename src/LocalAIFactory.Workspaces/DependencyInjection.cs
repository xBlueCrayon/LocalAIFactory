using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Workspaces.Autonomy;
using LocalAIFactory.Workspaces.Management;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LocalAIFactory.Workspaces;

public static class DependencyInjection
{
    public static IServiceCollection AddLocalAIFactoryWorkspaces(this IServiceCollection services, IConfiguration config)
    {
        // Phase 1.1 fix: actually bind the Workspaces configuration section (no silent hardcoded fallback).
        services.Configure<WorkspacesOptions>(config.GetSection("Workspaces"));

        // File-system helpers (stateless): singletons.
        services.AddSingleton<IZipExtractionService, ZipExtractionService>();
        services.AddSingleton<IWorkspaceService, WorkspaceService>();
        services.AddSingleton<IGitService, DisabledGitService>();
        services.AddSingleton<IBuildService, DisabledBuildService>();
        services.AddSingleton<IDiffService, DiffService>();

        // R2-ACC-CAP7: controlled autonomous workspace skeleton — command policy + dry-run planner (no execution).
        services.AddSingleton<ICommandPolicy, CommandPolicy>();
        services.AddSingleton<IAutonomousWorkflowPlanner, AutonomousWorkflowPlanner>();

        // Workspace management (uses DbContext): scoped.
        services.AddScoped<IWorkspaceManager, WorkspaceManager>();
        services.AddScoped<IWorkspaceSnapshotService, WorkspaceSnapshotService>();
        services.AddScoped<IWorkspaceModificationService, WorkspaceModificationService>();

        return services;
    }
}
