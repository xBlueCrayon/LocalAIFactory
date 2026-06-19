using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Workspaces;

// Phase 1 stubs: disabled by default. Wiring real execution is a later, opt-in phase.
public sealed class DisabledGitService : IGitService
{
    public bool Enabled => false;
    public Task<CommandResult> StatusAsync(string repoPath, CancellationToken ct = default)
        => Task.FromResult(new CommandResult(false, -1, "", "", "Git integration is disabled in Phase 1."));
    public Task<CommandResult> DiffAsync(string repoPath, CancellationToken ct = default)
        => Task.FromResult(new CommandResult(false, -1, "", "", "Git integration is disabled in Phase 1."));
}

public sealed class DisabledBuildService : IBuildService
{
    public bool Enabled => false;
    public Task<CommandResult> BuildAsync(string projectOrSolutionPath, CancellationToken ct = default)
        => Task.FromResult(new CommandResult(false, -1, "", "", "Build integration is disabled in Phase 1."));
    public Task<CommandResult> TestAsync(string projectOrSolutionPath, CancellationToken ct = default)
        => Task.FromResult(new CommandResult(false, -1, "", "", "Build integration is disabled in Phase 1."));
}
