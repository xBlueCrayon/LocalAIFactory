using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Workspaces.Autonomy;

// R2-ACC-20X: the safest possible local fix loop. It applies a file patch set to an ISOLATED workspace, runs
// allowlisted checks via the ControlledExecutor, and ROLLS BACK on any failure so the workspace is left exactly
// as found. It NEVER commits, pushes or merges (Committed is always false) and defaults to dry-run. The runner
// is injected so the loop is fully testable without spawning real processes.
public sealed class LocalFixLoop : ILocalFixLoop
{
    private readonly ICommandPolicy _policy;

    public LocalFixLoop(ICommandPolicy policy) { _policy = policy; }

    public FixLoopResult Run(ChangeRequest request, IReadOnlyList<FilePatch> patches,
        IReadOnlyList<string> checks, bool execute, CommandRunner runner)
    {
        var notes = new List<string>();
        var workingDir = request.TargetRepoPath;
        patches ??= Array.Empty<FilePatch>();
        checks ??= Array.Empty<string>();

        // ---- DRY-RUN: change nothing, run nothing; just show the plan + classify the checks. ----
        if (!execute)
        {
            notes.Add("DRY-RUN: no files were modified and no checks were executed.");
            notes.Add($"Planned: apply {patches.Count} patch(es), run {checks.Count} check(s), then HALT for human approval before any commit.");
            var dry = new ControlledExecutor(_policy, runner).Run(checks, execute: false, workingDir);
            notes.AddRange(dry.SafetyNotes);
            return new FixLoopResult(request.Id, DryRun: true, PatchApplied: false, ChecksPassed: false,
                RolledBack: false, Committed: false, dry.Records, notes);
        }

        if (string.IsNullOrWhiteSpace(workingDir) || !Directory.Exists(workingDir))
        {
            notes.Add($"Target workspace does not exist: '{workingDir}'. Nothing applied.");
            return new FixLoopResult(request.Id, false, false, false, false, false, Array.Empty<CommandRunRecord>(), notes);
        }

        var workspaceRoot = Path.GetFullPath(workingDir);
        var backups = new List<(string path, string? original, bool existed)>();
        bool applied = false;
        try
        {
            foreach (var patch in patches)
            {
                var full = Path.GetFullPath(Path.Combine(workspaceRoot, patch.RelativePath));
                // Safety: a patch may never write outside the isolated workspace.
                if (!full.StartsWith(workspaceRoot, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException($"Patch path escapes the workspace: {patch.RelativePath}");

                bool existed = File.Exists(full);
                backups.Add((full, existed ? File.ReadAllText(full) : null, existed));
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllText(full, patch.NewContent);
            }
            applied = patches.Count > 0;
            if (applied) notes.Add($"Applied {patches.Count} patch(es) to the isolated workspace.");

            var exec = new ControlledExecutor(_policy, runner).Run(checks, execute: true, workspaceRoot);
            notes.AddRange(exec.SafetyNotes);

            if (exec.AllExecutedPassed)
            {
                notes.Add("Checks PASSED. Patch retained in the isolated workspace, PENDING HUMAN APPROVAL.");
                notes.Add("NOT committed and NOT pushed — promotion to a commit is a separate, human-approved step.");
                return new FixLoopResult(request.Id, DryRun: false, PatchApplied: applied, ChecksPassed: true,
                    RolledBack: false, Committed: false, exec.Records, notes);
            }

            Rollback(backups);
            notes.Add("Checks FAILED. All patches were ROLLED BACK; the workspace is unchanged.");
            return new FixLoopResult(request.Id, DryRun: false, PatchApplied: applied, ChecksPassed: false,
                RolledBack: true, Committed: false, exec.Records, notes);
        }
        catch (Exception ex)
        {
            Rollback(backups);
            notes.Add("Error during fix loop: " + ex.Message + ". Patches rolled back; workspace unchanged.");
            return new FixLoopResult(request.Id, DryRun: false, PatchApplied: applied, ChecksPassed: false,
                RolledBack: true, Committed: false, Array.Empty<CommandRunRecord>(), notes);
        }
    }

    private static void Rollback(List<(string path, string? original, bool existed)> backups)
    {
        for (int i = backups.Count - 1; i >= 0; i--)
        {
            var (path, original, existed) = backups[i];
            try
            {
                if (existed) File.WriteAllText(path, original!);
                else if (File.Exists(path)) File.Delete(path);
            }
            catch { /* best-effort rollback */ }
        }
    }
}
