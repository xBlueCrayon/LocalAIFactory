using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Terminal;

// Phase 1: execution is disabled. The policy is still enforced so the UI can show what *would* run.
public sealed class TerminalService : ITerminalService
{
    private readonly ICommandPolicyService _policy;
    public TerminalService(ICommandPolicyService policy) => _policy = policy;

    public bool ExecutionEnabled => false;

    public Task<CommandResult> RunAsync(string command, string? workingDirectory = null, CancellationToken ct = default)
    {
        if (!_policy.IsAllowed(command, out var reason))
            return Task.FromResult(new CommandResult(false, -1, "", "", reason));

        return Task.FromResult(new CommandResult(
            Allowed: false, ExitCode: -1, StdOut: "", StdErr: "",
            Reason: "Terminal execution is disabled in Phase 1. The command passed policy and would be eligible to run once execution is explicitly enabled."));
    }
}
