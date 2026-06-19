using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Terminal;

// Conservative denylist. Even when execution is later enabled, destructive commands stay blocked.
public sealed class CommandPolicyService : ICommandPolicyService
{
    private static readonly string[] Denied =
    {
        "rm ", "rmdir", "del ", "format ", "mkfs", "shutdown", "reboot",
        ":(){", "dd ", "diskpart", "fdisk", "reg delete", "rd /s",
        "curl ", "wget ", "invoke-webrequest", "iwr ", "powershell -enc",
        "git push", "git reset --hard", "drop database", "drop table", "truncate "
    };

    public bool IsAllowed(string command, out string? reason)
    {
        var c = (command ?? "").Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(c)) { reason = "Empty command."; return false; }
        foreach (var d in Denied)
        {
            if (c.Contains(d))
            {
                reason = $"Command blocked by policy (matched '{d.Trim()}').";
                return false;
            }
        }
        reason = null;
        return true;
    }
}
