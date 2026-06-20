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

// R2-ACC-INDUSTRIAL: a controlled local executor. Dry-run by default; even in execute mode it runs ONLY
// allowlisted (build/test/read) commands, NEVER denied or approval-gated ones (commit/push/deploy/destructive),
// captures every command's output, and STOPS on the first failure (no promotion). It cannot commit or push.

// What actually runs a command (injected so it is testable without spawning real processes).
public delegate (int ExitCode, string Output) CommandRunner(string command, string workingDir);

public sealed record CommandRunRecord(
    string Command, CommandDecision Decision, bool Executed, int? ExitCode, string? Output, long DurationMs);

public sealed record ControlledRunResult(
    IReadOnlyList<CommandRunRecord> Records,
    bool DryRun,
    bool AllExecutedPassed,   // true if every command that actually ran returned exit 0
    bool Promoted,            // ALWAYS false here — promotion (commit/push) requires explicit human approval
    IReadOnlyList<string> SafetyNotes);

public interface IControlledExecutor
{
    // Run the given commands. In dry-run nothing executes. In execute mode only Allowed commands run; Denied and
    // RequiresApproval commands are recorded and skipped; execution halts at the first non-zero exit.
    ControlledRunResult Run(IReadOnlyList<string> commands, bool execute, string workingDir);
}

// R2-ACC-20X: the safest possible local fix loop. A single file patch set is applied to an ISOLATED workspace
// directory, allowlisted checks (build/test) run, and on ANY failure the patch is ROLLED BACK so the workspace
// is left exactly as found. The loop NEVER commits, pushes or merges — Committed is always false; promotion to
// a commit is a separate, explicit, human-approved step outside this loop. Default is dry-run (apply nothing).
public sealed record FilePatch(string RelativePath, string NewContent);

public sealed record FixLoopResult(
    string ChangeRequestId,
    bool DryRun,
    bool PatchApplied,
    bool ChecksPassed,
    bool RolledBack,
    bool Committed,            // ALWAYS false — commit requires explicit human approval outside the loop
    IReadOnlyList<CommandRunRecord> CheckRecords,
    IReadOnlyList<string> Notes);

public interface ILocalFixLoop
{
    // Apply `patches` to request.TargetRepoPath, run `checks`, roll back on failure. execute=false => dry-run.
    // `runner` actually runs a check command (injected for testability; a real deployment uses a process runner).
    FixLoopResult Run(ChangeRequest request, IReadOnlyList<FilePatch> patches,
        IReadOnlyList<string> checks, bool execute, CommandRunner runner);
}
