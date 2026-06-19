namespace LocalAIFactory.Core.Abstractions;

// Phase 1: interfaces and safe abstractions only. No unrestricted execution is wired up.

public sealed record CommandResult(bool Allowed, int ExitCode, string StdOut, string StdErr, string? Reason = null);

public interface ICommandPolicyService
{
    bool IsAllowed(string command, out string? reason);
}

public interface ITerminalService
{
    bool ExecutionEnabled { get; }
    Task<CommandResult> RunAsync(string command, string? workingDirectory = null, CancellationToken ct = default);
}

public interface IWorkspaceService
{
    bool IsReadOnly { get; }
    IReadOnlyList<string> ListFiles(string root, string searchPattern = "*.*");
    string? ReadText(string path);
}

public interface IGitService
{
    bool Enabled { get; }
    Task<CommandResult> StatusAsync(string repoPath, CancellationToken ct = default);
    Task<CommandResult> DiffAsync(string repoPath, CancellationToken ct = default);
}

public interface IBuildService
{
    bool Enabled { get; }
    Task<CommandResult> BuildAsync(string projectOrSolutionPath, CancellationToken ct = default);
    Task<CommandResult> TestAsync(string projectOrSolutionPath, CancellationToken ct = default);
}
