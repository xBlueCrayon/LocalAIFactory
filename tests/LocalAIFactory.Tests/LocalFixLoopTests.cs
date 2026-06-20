using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Workspaces.Autonomy;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-20X: the local fix loop applies a patch to an ISOLATED workspace, runs allowlisted checks, and ROLLS
// BACK on any failure — and NEVER commits. These tests use a real temp workspace + a fake check runner.
public class LocalFixLoopTests : IDisposable
{
    private readonly string _ws;
    private readonly ILocalFixLoop _loop = new LocalFixLoop(new CommandPolicy());

    public LocalFixLoopTests()
    {
        _ws = Path.Combine(Path.GetTempPath(), "laf_fixloop_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_ws);
        File.WriteAllText(Path.Combine(_ws, "Calc.cs"), "int Add(int a,int b)=>a-b; // BUG");
    }

    public void Dispose() { try { Directory.Delete(_ws, true); } catch { } }

    private ChangeRequest Req() => new("CR-1", "Fix Add()", "Addition uses minus", _ws);
    private static CommandRunner Runner(int exit) => (cmd, dir) => (exit, "ran: " + cmd);

    [Fact]
    public void Dry_run_changes_no_files_and_runs_no_checks()
    {
        var before = File.ReadAllText(Path.Combine(_ws, "Calc.cs"));
        var patch = new FilePatch("Calc.cs", "int Add(int a,int b)=>a+b; // FIXED");
        var ran = false;
        CommandRunner spy = (c, d) => { ran = true; return (0, ""); };

        var res = _loop.Run(Req(), new[] { patch }, new[] { "dotnet build" }, execute: false, spy);

        Assert.True(res.DryRun);
        Assert.False(res.PatchApplied);
        Assert.False(ran);                                            // no check executed
        Assert.Equal(before, File.ReadAllText(Path.Combine(_ws, "Calc.cs"))); // file untouched
        Assert.False(res.Committed);
    }

    [Fact]
    public void Passing_checks_retain_patch_but_never_commit()
    {
        var patch = new FilePatch("Calc.cs", "int Add(int a,int b)=>a+b; // FIXED");
        var res = _loop.Run(Req(), new[] { patch }, new[] { "dotnet build", "dotnet test" }, execute: true, Runner(0));

        Assert.True(res.PatchApplied);
        Assert.True(res.ChecksPassed);
        Assert.False(res.RolledBack);
        Assert.False(res.Committed);                                  // promotion is a separate human step
        Assert.Contains("int Add(int a,int b)=>a+b; // FIXED", File.ReadAllText(Path.Combine(_ws, "Calc.cs")));
        Assert.Contains(res.Notes, n => n.Contains("PENDING HUMAN APPROVAL"));
    }

    [Fact]
    public void Failing_checks_roll_back_the_patch()
    {
        var original = File.ReadAllText(Path.Combine(_ws, "Calc.cs"));
        var patch = new FilePatch("Calc.cs", "this will not compile");
        var res = _loop.Run(Req(), new[] { patch }, new[] { "dotnet build" }, execute: true, Runner(1)); // build fails

        Assert.True(res.RolledBack);
        Assert.False(res.ChecksPassed);
        Assert.False(res.Committed);
        Assert.Equal(original, File.ReadAllText(Path.Combine(_ws, "Calc.cs"))); // restored exactly
    }

    [Fact]
    public void New_file_patch_is_deleted_on_rollback()
    {
        var newFile = Path.Combine(_ws, "New.cs");
        Assert.False(File.Exists(newFile));
        var patch = new FilePatch("New.cs", "// brand new file");
        var res = _loop.Run(Req(), new[] { patch }, new[] { "dotnet test" }, execute: true, Runner(1)); // fails

        Assert.True(res.RolledBack);
        Assert.False(File.Exists(newFile));                           // created file removed on rollback
    }

    [Fact]
    public void Patch_escaping_the_workspace_is_rejected_and_rolled_back()
    {
        var patch = new FilePatch(Path.Combine("..", "escape.cs"), "malicious");
        var res = _loop.Run(Req(), new[] { patch }, new[] { "dotnet build" }, execute: true, Runner(0));

        Assert.True(res.RolledBack);
        Assert.False(res.Committed);
        Assert.False(File.Exists(Path.Combine(Path.GetDirectoryName(_ws)!, "escape.cs")));
    }
}
