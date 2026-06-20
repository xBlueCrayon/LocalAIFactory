using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Workspaces.Autonomy;

// R2-ACC-CAP7: hard command allow/deny policy for the controlled autonomous workspace. Deny-by-severity:
// destructive, history-rewriting, production, or system commands are DENIED; a small set of read/build/test
// commands are ALLOWED; everything else (incl. commit/push) defaults to RequiresApproval — never silently run.
public sealed class CommandPolicy : ICommandPolicy
{
    // Substrings that make a command DENIED outright (never planned, never executed).
    private static readonly (string token, string why)[] Denylist =
    {
        ("git reset --hard", "destructive: discards work"),
        ("git clean -fd", "destructive: deletes untracked files"),
        ("git clean -f", "destructive: deletes untracked files"),
        ("git push --force", "history rewrite / force push"),
        ("git push -f", "history rewrite / force push"),
        ("git rebase", "history rewrite"),
        ("git filter-branch", "history rewrite"),
        ("git merge", "merge is forbidden in this workflow"),
        ("rm -rf", "destructive recursive delete"),
        ("rmdir /s", "destructive recursive delete"),
        ("del /f", "destructive force delete"),
        ("drop database", "destructive database operation"),
        ("drop table", "destructive database operation"),
        ("truncate", "destructive database operation"),
        ("format ", "disk format"),
        ("mkfs", "disk format"),
        ("dd if=", "raw disk write"),
        ("diskpart", "disk partition tool"),
        ("shutdown", "system power control"),
        ("iisreset", "production web server control"),
        ("netsh", "network/firewall change"),
        ("firewall", "firewall change"),
        ("reg delete", "registry deletion"),
        ("sc delete", "service deletion"),
        ("deploy to production", "production deployment"),
        ("publish -c release --self-contained", "deployment packaging — requires human gate"),
        (":(){", "fork bomb"),
    };

    // Prefixes that are explicitly safe to run autonomously (read / build / test only).
    private static readonly string[] Allowlist =
    {
        "dotnet build", "dotnet test", "dotnet restore", "dotnet run --project tools/localaifactory.benchmark",
        "git status", "git diff", "git log", "git fetch", "git branch", "git rev-parse", "git show",
        "ls", "dir", "cat ", "type ", "echo ",
    };

    public CommandPolicyResult Classify(string command)
    {
        var c = (command ?? "").Trim();
        var lc = c.ToLowerInvariant();
        if (lc.Length == 0) return new(CommandDecision.Denied, "empty command", "invalid");

        foreach (var (token, why) in Denylist)
            if (lc.Contains(token)) return new(CommandDecision.Denied, why, "denylist");

        // commit / push are state-changing on shared history -> always require approval (not denied, but gated).
        if (lc.StartsWith("git commit") || lc.StartsWith("git push") || lc.StartsWith("git tag"))
            return new(CommandDecision.RequiresApproval, "changes shared/committed state — human approval required", "state-change");
        if (lc.Contains("dotnet ef migrations") || lc.Contains("dotnet ef database"))
            return new(CommandDecision.RequiresApproval, "schema/database change — human approval required", "state-change");

        foreach (var allow in Allowlist)
            if (lc.StartsWith(allow)) return new(CommandDecision.Allowed, "read/build/test command", "allowlist");

        return new(CommandDecision.RequiresApproval, "not on the allowlist — human approval required", "default-deny");
    }
}
