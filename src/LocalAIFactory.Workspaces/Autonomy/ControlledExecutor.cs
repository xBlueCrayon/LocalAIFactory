using System.Diagnostics;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Workspaces.Autonomy;

// R2-ACC-INDUSTRIAL: controlled local executor for the autonomous workspace. SAFETY is the design:
//  - dry-run by default; execution requires an explicit flag passed by the caller;
//  - only ALLOWED (allowlisted build/test/read) commands ever run;
//  - DENIED commands (destructive/production/history-rewriting) are NEVER run — recorded and skipped;
//  - RequiresApproval commands (commit/push/deploy/migrations/unknown) are NEVER run autonomously;
//  - execution halts at the first non-zero exit (no promotion past a failure);
//  - it cannot commit or push — Promoted is always false.
public sealed class ControlledExecutor : IControlledExecutor
{
    private readonly ICommandPolicy _policy;
    private readonly CommandRunner _runner;

    public ControlledExecutor(ICommandPolicy policy, CommandRunner? runner = null)
    {
        _policy = policy;
        _runner = runner ?? RealRunner;
    }

    public ControlledRunResult Run(IReadOnlyList<string> commands, bool execute, string workingDir)
    {
        var records = new List<CommandRunRecord>();
        bool allPassed = true;
        bool halted = false;

        foreach (var cmd in commands ?? Array.Empty<string>())
        {
            var decision = _policy.Classify(cmd).Decision;

            // Never run anything that isn't explicitly allowed; never run in dry-run; stop after a failure.
            if (halted || !execute || decision != CommandDecision.Allowed)
            {
                records.Add(new CommandRunRecord(cmd, decision, Executed: false, ExitCode: null, Output: null, DurationMs: 0));
                continue;
            }

            var sw = Stopwatch.StartNew();
            (int exit, string output) result;
            try { result = _runner(cmd, workingDir); }
            catch (Exception ex) { result = (-1, "executor error: " + ex.Message); }
            sw.Stop();

            records.Add(new CommandRunRecord(cmd, decision, Executed: true, ExitCode: result.exit,
                Output: Trim(result.output), DurationMs: sw.ElapsedMilliseconds));

            if (result.exit != 0) { allPassed = false; halted = true; } // stop promotion on first failure
        }

        var notes = new List<string>
        {
            execute ? "EXECUTE mode: only allowlisted build/test/read commands ran." : "DRY-RUN: nothing executed.",
            "Denied and approval-gated commands are never run autonomously.",
            "Execution halts on the first failure; no commit/push/deploy is ever performed here.",
        };
        return new ControlledRunResult(records, DryRun: !execute, AllExecutedPassed: allPassed, Promoted: false, notes);
    }

    private static string? Trim(string? s) => s is null ? null : (s.Length <= 4000 ? s : s[..4000] + "…");

    // Real runner: executes via the OS shell with output captured. Used in production; tests inject a fake.
    private static (int, string) RealRunner(string command, string workingDir)
    {
        var isWindows = OperatingSystem.IsWindows();
        var psi = new ProcessStartInfo
        {
            FileName = isWindows ? "cmd.exe" : "/bin/sh",
            Arguments = isWindows ? $"/c {command}" : $"-c \"{command}\"",
            WorkingDirectory = string.IsNullOrWhiteSpace(workingDir) ? Environment.CurrentDirectory : workingDir,
            RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false, CreateNoWindow = true
        };
        using var p = Process.Start(psi)!;
        var stdout = p.StandardOutput.ReadToEnd();
        var stderr = p.StandardError.ReadToEnd();
        p.WaitForExit();
        return (p.ExitCode, (stdout + stderr).Trim());
    }
}
