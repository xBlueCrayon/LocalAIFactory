using LocalAIFactory.Reasoning.AgentRunner;
using LocalAIFactory.Reasoning.Experience;
using Xunit;

namespace LocalAIFactory.Reasoning.Tests;

public class ModelDrivenRunnerTests : System.IDisposable
{
    private readonly string _root;
    public ModelDrivenRunnerTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "laf-mdr-" + System.Guid.NewGuid().ToString("n"));
        Directory.CreateDirectory(_root);
    }
    public void Dispose() { try { Directory.Delete(_root, true); } catch { } }

    private sealed class FakeExecutor : IValidationExecutor
    {
        private readonly bool _build, _test;
        public FakeExecutor(bool build, bool test) { _build = build; _test = test; }
        public Task<ValidationResult> BuildAsync(string r, CancellationToken ct = default) => Task.FromResult(new ValidationResult(_build, _build ? "ok" : "error"));
        public Task<ValidationResult> TestAsync(string r, CancellationToken ct = default) => Task.FromResult(new ValidationResult(_test, _test ? "passed" : "failed"));
    }

    private static PatchPlan SafePlan() => new("Add a helper", "introduce a small helper class",
        new[] { new PatchProposal("Helper.cs", "public class Helper {}") }, "fake-model");

    private static PatchPlan HallucinatedPlan() => new("Edit outside", "model invented a path",
        new[] { new PatchProposal("../../escape.cs", "evil") }, "fake-model");

    [Fact] public void Risk_assessment_blocks_hallucinated_path()
    {
        var runner = new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(true, true), new ExperienceMemory());
        var risk = runner.AssessRisk(_root, HallucinatedPlan());
        Assert.True(risk.IsBlocked);
    }

    [Fact] public void Risk_assessment_allows_a_safe_plan()
        => Assert.False(new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(true, true), new ExperienceMemory()).AssessRisk(_root, SafePlan()).IsBlocked);

    [Fact] public async Task Safe_compiling_plan_is_accepted_with_a_knowledge_proposal()
    {
        var mem = new ExperienceMemory();
        var runner = new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(true, true), mem);
        var report = await runner.RunAsync(_root, SafePlan());
        Assert.True(report.Accepted);
        Assert.NotNull(report.Proposal);
        Assert.False(report.Proposal!.Approved); // proposals require human approval
        Assert.True(File.Exists(Path.Combine(_root, "Helper.cs")));
    }

    [Fact] public async Task Test_failure_is_rejected_and_rolled_back()
    {
        var mem = new ExperienceMemory();
        var runner = new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(build: true, test: false), mem);
        var report = await runner.RunAsync(_root, SafePlan());
        Assert.False(report.Accepted);
        Assert.False(File.Exists(Path.Combine(_root, "Helper.cs"))); // rolled back
        Assert.Contains(mem.All, e => e.Type == ExperienceType.TestFailure);
    }

    [Fact] public async Task Build_failure_is_rejected_and_rolled_back()
    {
        var mem = new ExperienceMemory();
        var report = await new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(build: false, test: true), mem).RunAsync(_root, SafePlan());
        Assert.False(report.Accepted);
        Assert.Contains(mem.All, e => e.Type == ExperienceType.BuildFailure);
    }

    [Fact] public async Task Hallucinated_plan_is_blocked_before_any_write_and_recorded()
    {
        var mem = new ExperienceMemory();
        var report = await new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(true, true), mem).RunAsync(_root, HallucinatedPlan());
        Assert.False(report.Accepted);
        Assert.True(report.Risk.IsBlocked);
        Assert.Null(report.RunResult); // never even ran the patch
        Assert.Contains(mem.All, e => e.Type == ExperienceType.SecurityFinding);
    }

    [Fact] public void Forbidden_command_in_a_script_patch_is_blocked()
    {
        var plan = new PatchPlan("bad script", "rationale",
            new[] { new PatchProposal("deploy.ps1", "git push origin main --force") }, "fake-model");
        Assert.True(new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(true, true), new ExperienceMemory()).AssessRisk(_root, plan).IsBlocked);
    }

    [Fact] public void Empty_plan_is_medium_risk_not_accepted_blindly()
    {
        var plan = new PatchPlan("nothing", "no-op", System.Array.Empty<PatchProposal>(), "fake-model");
        var risk = new ModelDrivenPlanPatchVerifyRunner(new FakeExecutor(true, true), new ExperienceMemory()).AssessRisk(_root, plan);
        Assert.Contains(risk.Reasons, r => r.Contains("empty plan"));
    }
}
