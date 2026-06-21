namespace LocalAIFactory.Reasoning.Safety;

public enum CommandRisk { ReadOnly, SafeValidation, ControlledWrite, Forbidden }
public enum ApprovalRequirement { None, HumanApproval, AlwaysBlocked }

public sealed record SafeExecutionRequest(string Command, string? WorkingDir = null, IReadOnlyList<string>? Paths = null);
public sealed record SafeExecutionDecision(bool Allowed, CommandRisk Risk, ApprovalRequirement Approval, string Reason);
public sealed record SafeExecutionLogEntry(string Command, CommandRisk Risk, bool Allowed, string Reason, string StampTag);

/// <summary>
/// Classifies a command's risk for the safe agent runner. Deterministic and conservative: anything not on an
/// explicit allowlist is Forbidden by default (no model decides what is safe). Used to gate every tool call.
/// </summary>
public sealed class CommandRiskClassifier
{
    // Read-only: never mutate the repo.
    private static readonly string[] ReadOnly =
    {
        "dir", "ls", "type", "cat", "git status", "git diff", "git log", "git ls-files", "git show",
        "findstr", "rg ", "grep ", "select-string", "get-content", "get-childitem"
    };
    // Safe validation: may compile/run tests/scans but not write source or touch git history.
    private static readonly string[] SafeValidation =
    {
        "dotnet build", "dotnet test", "dotnet restore", "npx playwright test", "playwright test",
        "verify-all-knowledge-packs", "verify-production-readiness", "security-audit"
    };
    // Controlled write: allowed ONLY inside an isolated worktree (enforced separately by the sandbox guard).
    private static readonly string[] ControlledWrite =
    {
        "apply-patch", "git apply", "write-file", "update-template", "create-report", "update-knowledge"
    };
    // Always forbidden — destructive, outbound, or capable of escaping the sandbox.
    private static readonly string[] Forbidden =
    {
        "git push", "git commit", "git reset --hard", "git clean", "rm -rf", "remove-item -recurse",
        "del /", "rmdir /s", "format ", "drop database", "drop table",
        "invoke-expression", "iex ", "| powershell", "| iex", "curl ", "wget ", "invoke-webrequest",
        "npm install", "dotnet tool install", "pip install", "shutdown", "reg add", "reg delete",
        "net user", "schtasks", "certutil -urlcache", "bitsadmin"
    };

    public CommandRisk Classify(string command)
    {
        var c = (command ?? "").Trim().ToLowerInvariant();
        if (c.Length == 0) return CommandRisk.Forbidden;
        if (Forbidden.Any(f => c.Contains(f))) return CommandRisk.Forbidden;   // forbidden wins over everything
        if (SafeValidation.Any(s => c.StartsWith(s) || c.Contains(s))) return CommandRisk.SafeValidation;
        if (ControlledWrite.Any(w => c.StartsWith(w) || c.Contains(w))) return CommandRisk.ControlledWrite;
        if (ReadOnly.Any(r => c.StartsWith(r))) return CommandRisk.ReadOnly;
        return CommandRisk.Forbidden; // default-deny: unknown commands are not allowed
    }
}

/// <summary>Guards that a write path stays inside a single isolated worktree root (blocks path traversal / absolute escapes).</summary>
public sealed class PathSandboxGuard
{
    private readonly string _root;
    public PathSandboxGuard(string worktreeRoot) => _root = Path.GetFullPath(worktreeRoot).Replace('\\', '/').TrimEnd('/') + "/";

    public bool IsInside(string candidate)
    {
        if (string.IsNullOrWhiteSpace(candidate)) return false;
        string full;
        try { full = Path.GetFullPath(Path.IsPathRooted(candidate) ? candidate : Path.Combine(_root, candidate)).Replace('\\', '/'); }
        catch { return false; }
        if (candidate.Contains("..")) // explicit traversal is rejected even if it resolves inside
        {
            var resolved = full.TrimEnd('/') + "/";
            if (!resolved.StartsWith(_root, StringComparison.OrdinalIgnoreCase)) return false;
        }
        return (full + "/").StartsWith(_root, StringComparison.OrdinalIgnoreCase);
    }
}

/// <summary>
/// The safe tool gateway: every tool call is classified, optionally checked against an isolated-worktree
/// sandbox, decided, and logged. No model or agent ever bypasses this. Controlled writes require a worktree;
/// forbidden commands are always blocked.
/// </summary>
public sealed class SafeToolGateway
{
    private readonly CommandRiskClassifier _classifier = new();
    private readonly PathSandboxGuard? _sandbox;
    private readonly List<SafeExecutionLogEntry> _log = new();
    private readonly string _stampTag;

    public SafeToolGateway(string? worktreeRoot = null, string stampTag = "agent")
    {
        _sandbox = worktreeRoot is null ? null : new PathSandboxGuard(worktreeRoot);
        _stampTag = stampTag;
    }

    public IReadOnlyList<SafeExecutionLogEntry> Log => _log;

    public SafeExecutionDecision Evaluate(SafeExecutionRequest request)
    {
        var risk = _classifier.Classify(request.Command);
        SafeExecutionDecision decision = risk switch
        {
            CommandRisk.Forbidden => new(false, risk, ApprovalRequirement.AlwaysBlocked, "Command is forbidden by policy (default-deny)."),
            CommandRisk.ReadOnly => new(true, risk, ApprovalRequirement.None, "Read-only command allowed."),
            CommandRisk.SafeValidation => new(true, risk, ApprovalRequirement.None, "Safe validation command allowed."),
            CommandRisk.ControlledWrite => EvaluateWrite(request, risk),
            _ => new(false, risk, ApprovalRequirement.AlwaysBlocked, "Unclassified command blocked.")
        };
        _log.Add(new SafeExecutionLogEntry(request.Command, risk, decision.Allowed, decision.Reason, _stampTag));
        return decision;
    }

    private SafeExecutionDecision EvaluateWrite(SafeExecutionRequest request, CommandRisk risk)
    {
        if (_sandbox is null)
            return new(false, risk, ApprovalRequirement.HumanApproval, "Controlled write requires an isolated worktree sandbox.");
        if (request.Paths is { Count: > 0 })
            foreach (var p in request.Paths)
                if (!_sandbox.IsInside(p))
                    return new(false, risk, ApprovalRequirement.AlwaysBlocked, $"Write path escapes the worktree sandbox: {p}");
        return new(true, risk, ApprovalRequirement.None, "Controlled write inside the isolated worktree allowed.");
    }
}
