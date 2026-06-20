using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Workspaces.Autonomy;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-INDUSTRIAL: the controlled local executor must be SAFE — dry-run by default, only allowlisted commands
// ever run, denied/approval-gated commands never run, execution halts on failure, and it never promotes.
public class AutonomousExecutorTests
{
    // Records which commands actually reached the runner, and returns scripted exit codes.
    private sealed class FakeRunner
    {
        public List<string> Invoked { get; } = new();
        private readonly Dictionary<string, int> _exits;
        public FakeRunner(Dictionary<string, int>? exits = null) => _exits = exits ?? new();
        public CommandRunner Delegate => (cmd, dir) => { Invoked.Add(cmd); return (_exits.GetValueOrDefault(cmd, 0), "output of: " + cmd); };
    }

    private static ControlledExecutor New(FakeRunner r) => new(new CommandPolicy(), r.Delegate);

    [Fact]
    public void Dry_run_executes_nothing()
    {
        var r = new FakeRunner();
        var res = New(r).Run(new[] { "dotnet build", "dotnet test" }, execute: false, ".");
        Assert.True(res.DryRun);
        Assert.Empty(r.Invoked);                                  // nothing ran
        Assert.All(res.Records, x => Assert.False(x.Executed));
        Assert.False(res.Promoted);
    }

    [Fact]
    public void Execute_runs_only_allowlisted_commands()
    {
        var r = new FakeRunner();
        var res = New(r).Run(new[] { "dotnet build", "git status" }, execute: true, ".");
        Assert.Equal(new[] { "dotnet build", "git status" }, r.Invoked); // both allowlisted -> ran
        Assert.True(res.AllExecutedPassed);
        Assert.False(res.Promoted);                                // executor NEVER promotes
    }

    [Fact]
    public void Denied_and_approval_commands_never_execute()
    {
        var r = new FakeRunner();
        var res = New(r).Run(new[] { "git reset --hard", "git commit -m x", "DROP DATABASE X", "rm -rf /" }, execute: true, ".");
        Assert.Empty(r.Invoked);                                  // none ran
        Assert.Contains(res.Records, x => x.Command.Contains("reset --hard") && x.Decision == CommandDecision.Denied && !x.Executed);
        Assert.Contains(res.Records, x => x.Command.StartsWith("git commit") && x.Decision == CommandDecision.RequiresApproval && !x.Executed);
    }

    [Fact]
    public void Execution_halts_on_first_failure_no_promotion()
    {
        var r = new FakeRunner(new Dictionary<string, int> { ["dotnet build"] = 1 }); // build fails
        var res = New(r).Run(new[] { "dotnet build", "dotnet test" }, execute: true, ".");
        Assert.Equal(new[] { "dotnet build" }, r.Invoked);        // test never ran after build failed
        Assert.False(res.AllExecutedPassed);
        Assert.False(res.Promoted);
        Assert.False(res.Records.Single(x => x.Command == "dotnet test").Executed);
    }
}
