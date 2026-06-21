using LocalAIFactory.Reasoning.Experience;
using LocalAIFactory.Reasoning.Safety;

namespace LocalAIFactory.Reasoning.AgentRunner;

public sealed record PatchPlan(string Requirement, string Rationale, IReadOnlyList<PatchProposal> Patches, string PlanSource);
public sealed record PatchRiskAssessment(string Level, IReadOnlyList<string> Reasons) // Low | Medium | Blocked
{
    public bool IsBlocked => Level == "Blocked";
}
public sealed record KnowledgeProposal(string Title, string Rule, string Evidence, double Confidence, bool Approved = false);
public sealed record PatchVerificationReport(
    bool Accepted, PatchRiskAssessment Risk, AgentRunResult? RunResult, KnowledgeProposal? Proposal, string Summary);

/// <summary>A model (or any planner) that proposes a patch plan for a requirement. Implementations must not execute anything.</summary>
public interface IPatchPlanner
{
    Task<PatchPlan> PlanAsync(string requirement, CancellationToken ct = default);
}

/// <summary>
/// The model-assisted Plan → Patch → Verify loop. It is NOT reckless: a planner (a model or deterministic
/// stub) only PROPOSES a plan; this runner assesses risk against the safe gateway, applies the patch ONLY in an
/// isolated worktree via <see cref="IsolatedPatchRunner"/>, validates with build/test, rolls back on failure,
/// records experience, and emits a knowledge PROPOSAL (never auto-approved). It has no commit/push capability
/// and never edits the main repo.
/// </summary>
public sealed class ModelDrivenPlanPatchVerifyRunner
{
    private readonly IValidationExecutor _executor;
    private readonly IExperienceMemory _experience;

    public ModelDrivenPlanPatchVerifyRunner(IValidationExecutor executor, IExperienceMemory experience)
    { _executor = executor; _experience = experience; }

    /// <summary>Deterministically assess a plan's risk: every target path must be inside the worktree sandbox.</summary>
    public PatchRiskAssessment AssessRisk(string worktreeRoot, PatchPlan plan)
    {
        var reasons = new List<string>();
        if (plan.Patches.Count == 0) reasons.Add("empty plan (no patches)");
        var gateway = new SafeToolGateway(worktreeRoot, "plan-verify");
        foreach (var p in plan.Patches)
        {
            var d = gateway.Evaluate(new SafeExecutionRequest("apply-patch", worktreeRoot, new[] { p.RelativePath }));
            if (!d.Allowed) reasons.Add($"path escapes the sandbox (likely hallucinated): {p.RelativePath}");
            if (p.NewContent.Length == 0) reasons.Add($"empty content for {p.RelativePath}");
            // a patch must not try to smuggle a forbidden command into a script
            if (new CommandRiskClassifier().Classify(p.NewContent) == CommandRisk.Forbidden && p.RelativePath.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase))
                reasons.Add($"forbidden command in script content: {p.RelativePath}");
        }
        var blocked = reasons.Any(r => r.Contains("escapes the sandbox") || r.Contains("forbidden command"));
        var level = blocked ? "Blocked" : reasons.Count > 0 ? "Medium" : "Low";
        return new PatchRiskAssessment(level, reasons);
    }

    public async Task<PatchVerificationReport> RunAsync(string worktreeRoot, PatchPlan plan, CancellationToken ct = default)
    {
        var risk = AssessRisk(worktreeRoot, plan);
        if (risk.IsBlocked)
        {
            _experience.Add(new ExperienceEntry
            {
                Type = ExperienceType.SecurityFinding,
                Title = $"Plan rejected (risk Blocked): {plan.Requirement}",
                Source = "model-driven-runner", Symptoms = string.Join("; ", risk.Reasons),
                RootCause = "Planner proposed an unsafe/hallucinated patch.",
                Fix = "Blocked before any write; nothing applied.",
                ReusableLesson = "Risk assessment + sandbox blocks unsafe model-proposed patches before they touch disk.",
                Confidence = "high"
            });
            return new PatchVerificationReport(false, risk, null, null, "Blocked by risk assessment before any write.");
        }

        var runner = new IsolatedPatchRunner(_executor, _experience);
        var run = await runner.RunAsync(worktreeRoot, plan.Patches, plan.Requirement, ct);

        if (!run.Accepted)
            return new PatchVerificationReport(false, risk, run, null, run.Reason);

        var proposal = new KnowledgeProposal(
            Title: $"Validated change: {plan.Requirement}",
            Rule: plan.Rationale,
            Evidence: $"Applied in an isolated worktree; build + tests green. Files: {string.Join(", ", plan.Patches.Select(p => p.RelativePath))}",
            Confidence: 0.8,
            Approved: false); // proposals require human approval
        return new PatchVerificationReport(true, risk, run, proposal, "Accepted: plan validated in isolation; knowledge proposal emitted (awaiting approval).");
    }
}
