using System.Text;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Workspaces.Autonomy;

// R2-ACC-CAP7: builds a DRY-RUN plan for a change request and renders a review report. It executes NOTHING,
// commits nothing, and pushes nothing. Every state-changing step (commit/push) and every non-allowlisted
// command is marked RequiresApproval; the plan always ends behind a human-approval gate.
public sealed class AutonomousWorkflowPlanner : IAutonomousWorkflowPlanner
{
    private readonly ICommandPolicy _policy;

    public AutonomousWorkflowPlanner(ICommandPolicy policy) { _policy = policy; }

    public AutonomousPlan Plan(ChangeRequest request)
    {
        var steps = new List<WorkflowStep>
        {
            Step(PlanStepKind.CreateIsolatedWorkspace, "Create an isolated workspace (branch/worktree) — never on a shared branch.", null),
            Step(PlanStepKind.ProposePatch, "Propose a patch as a diff (not applied to the working tree without approval).", null),
            Step(PlanStepKind.Build, "Build the solution.", "dotnet build LocalAIFactory.sln -c Release"),
            Step(PlanStepKind.Test, "Run the test suite.", "dotnet test tests/LocalAIFactory.Tests/LocalAIFactory.Tests.csproj -c Release"),
            Step(PlanStepKind.Benchmark, "Run the validation harness.", "dotnet run --project tools/LocalAIFactory.Benchmark -c Release -- --inmemory"),
            Step(PlanStepKind.UiSmoke, "Run the UI smoke test.", "pwsh scripts/poc/ui-smoke-test.ps1"),
            Step(PlanStepKind.GenerateReport, "Generate an evidence report (build/test/benchmark/smoke results).", null),
            Step(PlanStepKind.RequestHumanApproval, "STOP: require explicit human approval before any state change.", null),
            Step(PlanStepKind.Commit, "Commit (only after approval).", "git commit -m \"<message>\""),
            Step(PlanStepKind.Push, "Push the branch (only after approval; never merge).", "git push origin <branch>"),
        };

        var notes = new List<string>
        {
            "Dry-run: the planner executes nothing and changes no files.",
            "Commit, push, merge, and deploy ALWAYS require explicit human approval.",
            "Denied commands (reset --hard, clean -fdx, drop/truncate, force-push, merge, production deploy, system changes) are never planned.",
            "Work happens in an isolated workspace; production is never touched.",
        };

        return new AutonomousPlan(request, steps, DryRun: true, RequiresHumanApprovalBeforeCommit: true, notes);
    }

    private WorkflowStep Step(PlanStepKind kind, string desc, string? command)
    {
        var decision = command is null ? CommandDecision.Allowed : _policy.Classify(command).Decision;
        bool gate = kind is PlanStepKind.Commit or PlanStepKind.Push or PlanStepKind.RequestHumanApproval
                    || decision != CommandDecision.Allowed;
        return new WorkflowStep(kind, desc, command, decision, gate);
    }

    public string GenerateReport(AutonomousPlan plan)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Autonomous Workspace — DRY-RUN PLAN");
        sb.AppendLine($"Change request: {plan.Request.Id} — {plan.Request.Title}");
        sb.AppendLine($"Target: {plan.Request.TargetRepoPath}");
        sb.AppendLine($"DryRun={plan.DryRun}  ApprovalBeforeCommit={plan.RequiresHumanApprovalBeforeCommit}");
        sb.AppendLine();
        sb.AppendLine("Steps:");
        int i = 1;
        foreach (var s in plan.Steps)
        {
            var gate = s.RequiresApproval ? "  [APPROVAL REQUIRED]" : "";
            sb.AppendLine($"  {i++}. {s.Kind}: {s.Description}{gate}");
            if (s.Command is not null) sb.AppendLine($"       $ {s.Command}   -> {s.CommandDecision}");
        }
        sb.AppendLine();
        sb.AppendLine("Safety:");
        foreach (var n in plan.SafetyNotes) sb.AppendLine($"  - {n}");
        return sb.ToString();
    }
}
