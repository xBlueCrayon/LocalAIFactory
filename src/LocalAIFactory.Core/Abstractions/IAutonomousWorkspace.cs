namespace LocalAIFactory.Core.Abstractions;

// R2-ACC-CAP7: controlled autonomous fix/test workspace skeleton. Local-only, dry-run-by-default, with a hard
// command allow/deny policy. The whole point of this type surface is SAFETY: dangerous commands are denied,
// commit/push/merge/deploy ALWAYS require explicit human approval, and the planner executes nothing.

public enum CommandDecision { Allowed = 0, RequiresApproval = 1, Denied = 2 }

public sealed record CommandPolicyResult(CommandDecision Decision, string Reason, string Category);

// Classifies a shell command against an allowlist (safe build/test/read) and a denylist (destructive /
// production / history-rewriting). Anything not explicitly safe defaults to RequiresApproval — never silently
// allowed.
public interface ICommandPolicy
{
    CommandPolicyResult Classify(string command);
}

public sealed record ChangeRequest(string Id, string Title, string Description, string TargetRepoPath);

public enum PlanStepKind
{
    CreateIsolatedWorkspace = 0, ProposePatch = 1, Build = 2, Test = 3, Benchmark = 4, UiSmoke = 5,
    GenerateReport = 6, RequestHumanApproval = 7, Commit = 8, Push = 9
}

// One planned step. Command (if any) has already been classified by the policy. RequiresApproval is true for
// any step that changes shared state (commit/push) or runs a non-allowlisted command.
public sealed record WorkflowStep(
    PlanStepKind Kind,
    string Description,
    string? Command,
    CommandDecision CommandDecision,
    bool RequiresApproval);

// A plan is DRY-RUN: nothing is executed, nothing is committed/pushed. It always ends with an approval gate
// before any state-changing step.
public sealed record AutonomousPlan(
    ChangeRequest Request,
    IReadOnlyList<WorkflowStep> Steps,
    bool DryRun,
    bool RequiresHumanApprovalBeforeCommit,
    IReadOnlyList<string> SafetyNotes);

public interface IAutonomousWorkflowPlanner
{
    // Build a dry-run plan for a change request. Executes nothing; commits nothing.
    AutonomousPlan Plan(ChangeRequest request);

    // Render a human-readable report of the plan (for review/approval). Pure formatting; no side effects.
    string GenerateReport(AutonomousPlan plan);
}
