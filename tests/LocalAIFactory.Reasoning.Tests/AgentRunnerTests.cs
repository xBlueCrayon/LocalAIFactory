using LocalAIFactory.Reasoning.AgentRunner;
using LocalAIFactory.Reasoning.Experience;
using Xunit;

namespace LocalAIFactory.Reasoning.Tests;

public class AgentRunnerTests : System.IDisposable
{
    private readonly string _root;
    public AgentRunnerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "laf-runner-" + System.Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_root);
        File.WriteAllText(Path.Combine(_root, "Existing.cs"), "// original\n");
    }
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private sealed class FakeExecutor : IValidationExecutor
    {
        private readonly bool _build, _test;
        public FakeExecutor(bool build, bool test) { _build = build; _test = test; }
        public Task<ValidationResult> BuildAsync(string r, CancellationToken ct = default) => Task.FromResult(new ValidationResult(_build, _build ? "ok" : "error CS1061"));
        public Task<ValidationResult> TestAsync(string r, CancellationToken ct = default) => Task.FromResult(new ValidationResult(_test, _test ? "passed" : "1 failed"));
    }

    [Fact] public async Task Compiling_passing_patch_is_accepted()
    {
        var mem = new ExperienceMemory();
        var runner = new IsolatedPatchRunner(new FakeExecutor(true, true), mem);
        var res = await runner.RunAsync(_root, new[] { new PatchProposal("New.cs", "public class New {}") }, "add New");
        Assert.True(res.Accepted);
        Assert.True(File.Exists(Path.Combine(_root, "New.cs")));
        Assert.Contains(mem.All, e => e.Type == ExperienceType.RegressionPrevented);
    }

    [Fact] public async Task Build_failure_is_rejected_and_rolled_back()
    {
        var mem = new ExperienceMemory();
        var runner = new IsolatedPatchRunner(new FakeExecutor(build: false, test: true), mem);
        var res = await runner.RunAsync(_root, new[] { new PatchProposal("New.cs", "broken") }, "bad patch");
        Assert.False(res.Accepted);
        Assert.False(File.Exists(Path.Combine(_root, "New.cs"))); // rolled back (new file removed)
        Assert.Contains(mem.All, e => e.Type == ExperienceType.BuildFailure);
    }

    [Fact] public async Task Test_failure_is_rejected_and_rolled_back()
    {
        var mem = new ExperienceMemory();
        var runner = new IsolatedPatchRunner(new FakeExecutor(build: true, test: false), mem);
        var res = await runner.RunAsync(_root, new[] { new PatchProposal("New.cs", "x") }, "t");
        Assert.False(res.Accepted);
        Assert.Contains(mem.All, e => e.Type == ExperienceType.TestFailure);
    }

    [Fact] public async Task Edit_to_existing_file_is_restored_on_failure()
    {
        var mem = new ExperienceMemory();
        var runner = new IsolatedPatchRunner(new FakeExecutor(build: false, test: true), mem);
        await runner.RunAsync(_root, new[] { new PatchProposal("Existing.cs", "// changed") }, "edit");
        Assert.Equal("// original\n", File.ReadAllText(Path.Combine(_root, "Existing.cs"))); // restored
    }

    [Fact] public async Task Patch_outside_worktree_is_rejected_before_any_write()
    {
        var mem = new ExperienceMemory();
        var runner = new IsolatedPatchRunner(new FakeExecutor(true, true), mem);
        var res = await runner.RunAsync(_root, new[] { new PatchProposal("../escape.cs", "evil") }, "escape");
        Assert.False(res.Accepted);
        Assert.Contains("safety gateway", res.Reason);
        Assert.False(File.Exists(Path.Combine(_root, "..", "escape.cs")));
    }

    [Fact] public async Task Missing_worktree_refuses_to_run()
    {
        var runner = new IsolatedPatchRunner(new FakeExecutor(true, true), new ExperienceMemory());
        var res = await runner.RunAsync(Path.Combine(_root, "does-not-exist"), new[] { new PatchProposal("a.cs", "x") }, "t");
        Assert.False(res.Accepted);
    }

    [Fact] public async Task Success_experience_records_affected_files()
    {
        var mem = new ExperienceMemory();
        var runner = new IsolatedPatchRunner(new FakeExecutor(true, true), mem);
        await runner.RunAsync(_root, new[] { new PatchProposal("New.cs", "public class New {}") }, "add");
        Assert.Contains(mem.All, e => e.AffectedFiles.Contains("New.cs"));
    }
}
