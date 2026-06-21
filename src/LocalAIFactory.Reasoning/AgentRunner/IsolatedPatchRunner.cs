using LocalAIFactory.Reasoning.Experience;
using LocalAIFactory.Reasoning.Safety;

namespace LocalAIFactory.Reasoning.AgentRunner;

/// <summary>A proposed file write, relative to (or absolute within) the isolated worktree root.</summary>
public sealed record PatchProposal(string RelativePath, string NewContent);

public sealed record ValidationResult(bool Ok, string Output);

/// <summary>Runs build/test inside a worktree. Abstracted so the runner is unit-testable without invoking dotnet.</summary>
public interface IValidationExecutor
{
    Task<ValidationResult> BuildAsync(string worktreeRoot, CancellationToken ct = default);
    Task<ValidationResult> TestAsync(string worktreeRoot, CancellationToken ct = default);
}

public sealed record AgentRunResult(bool Accepted, string Reason, IReadOnlyList<string> Steps);

/// <summary>
/// The safe agent patch loop: apply a proposed patch ONLY inside an isolated worktree, validate it with
/// build + test, roll back on failure, and record the outcome as experience. It has no capability to commit,
/// push, or write outside the worktree — every write path is checked by the <see cref="SafeToolGateway"/>.
/// </summary>
public sealed class IsolatedPatchRunner
{
    private readonly IValidationExecutor _executor;
    private readonly IExperienceMemory _experience;

    public IsolatedPatchRunner(IValidationExecutor executor, IExperienceMemory experience)
    { _executor = executor; _experience = experience; }

    public async Task<AgentRunResult> RunAsync(string worktreeRoot, IReadOnlyList<PatchProposal> patches, string taskTitle, CancellationToken ct = default)
    {
        var steps = new List<string>();
        if (!Directory.Exists(worktreeRoot))
            return new AgentRunResult(false, "Worktree root does not exist; refusing to run.", steps);

        var gateway = new SafeToolGateway(worktreeRoot, "agent-runner");

        // 1. Validate every write path is inside the sandbox BEFORE touching disk.
        foreach (var p in patches)
        {
            var decision = gateway.Evaluate(new SafeExecutionRequest("apply-patch", worktreeRoot, new[] { p.RelativePath }));
            steps.Add($"gate apply-patch {p.RelativePath}: {(decision.Allowed ? "allow" : "BLOCK")} ({decision.Reason})");
            if (!decision.Allowed)
                return new AgentRunResult(false, $"Patch rejected by safety gateway: {decision.Reason}", steps);
        }

        // 2. Snapshot originals for rollback.
        var rollback = new Dictionary<string, string?>();
        foreach (var p in patches)
        {
            var full = Path.Combine(worktreeRoot, p.RelativePath);
            rollback[full] = File.Exists(full) ? File.ReadAllText(full) : null;
        }

        try
        {
            // 3. Apply.
            foreach (var p in patches)
            {
                var full = Path.Combine(worktreeRoot, p.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(full)!);
                File.WriteAllText(full, p.NewContent);
                steps.Add($"applied {p.RelativePath}");
            }

            // 4. Build.
            var build = await _executor.BuildAsync(worktreeRoot, ct);
            steps.Add($"build: {(build.Ok ? "ok" : "FAIL")}");
            if (!build.Ok)
            {
                Rollback(rollback, steps);
                RecordFailure(ExperienceType.BuildFailure, taskTitle, patches, build.Output);
                return new AgentRunResult(false, "Build failed; rolled back.", steps);
            }

            // 5. Test.
            var test = await _executor.TestAsync(worktreeRoot, ct);
            steps.Add($"test: {(test.Ok ? "ok" : "FAIL")}");
            if (!test.Ok)
            {
                Rollback(rollback, steps);
                RecordFailure(ExperienceType.TestFailure, taskTitle, patches, test.Output);
                return new AgentRunResult(false, "Tests failed; rolled back.", steps);
            }

            // 6. Accept + record success experience.
            _experience.Add(new ExperienceEntry
            {
                Type = ExperienceType.RegressionPrevented,
                Title = $"Patch accepted: {taskTitle}",
                Source = "agent-runner",
                AffectedFiles = patches.Select(p => p.RelativePath).ToList(),
                Symptoms = "n/a",
                RootCause = "n/a",
                Fix = "Patch applied in isolated worktree; build + tests green.",
                ReusableLesson = "Validated patch in isolation before any human reviews or commits it.",
                Confidence = "high"
            });
            return new AgentRunResult(true, "Patch accepted: build + tests passed in the isolated worktree.", steps);
        }
        catch (Exception ex)
        {
            Rollback(rollback, steps);
            return new AgentRunResult(false, $"Runner error; rolled back: {ex.Message}", steps);
        }
    }

    private static void Rollback(Dictionary<string, string?> snapshot, List<string> steps)
    {
        foreach (var (full, original) in snapshot)
        {
            try
            {
                if (original is null) { if (File.Exists(full)) File.Delete(full); }
                else File.WriteAllText(full, original);
            }
            catch { /* best-effort rollback */ }
        }
        steps.Add("rolled back");
    }

    private void RecordFailure(ExperienceType type, string task, IReadOnlyList<PatchProposal> patches, string output)
    {
        _experience.Add(new ExperienceEntry
        {
            Type = type,
            Title = $"Patch rejected ({type}): {task}",
            Source = "agent-runner",
            AffectedFiles = patches.Select(p => p.RelativePath).ToList(),
            Symptoms = output.Length > 500 ? output[..500] : output,
            RootCause = "Proposed patch did not pass validation.",
            Fix = "Rolled back automatically; no change reached the working tree.",
            ReusableLesson = "Isolated validation caught a bad patch before it could be committed.",
            Confidence = "high"
        });
    }
}
