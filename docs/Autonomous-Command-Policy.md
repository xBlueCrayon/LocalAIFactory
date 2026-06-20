# Autonomous Command Policy

The single chokepoint (`CommandPolicy`, `src/LocalAIFactory.Workspaces/Autonomy/CommandPolicy.cs`) that
classifies every command the autonomous workspace might run. **Deny-by-severity, default to approval.**

## Decisions

| Decision | Meaning | Examples |
|---|---|---|
| **Denied** | never planned, never executed | `git reset --hard`, `git clean -fd`, `git push --force`, `git rebase`, `git filter-branch`, **`git merge`**, `rm -rf`, `del /f`, `drop database`, `drop table`, `truncate`, `format`, `mkfs`, `dd if=`, `diskpart`, `shutdown`, `iisreset`, `netsh`, `firewall`, `reg delete`, `sc delete`, `deploy to production`, fork-bomb |
| **RequiresApproval** | never run autonomously; a human must approve | `git commit …`, `git push …`, `git tag …`, `dotnet ef migrations/database …`, and **any command not on the allowlist** |
| **Allowed** | safe to run in execute mode | `dotnet build`, `dotnet test`, `dotnet restore`, `dotnet run --project tools/localaifactory.benchmark`, `git status/diff/log/fetch/branch/rev-parse/show`, `ls`, `dir`, `cat`, `type`, `echo` |

## Guarantees (tested)

- Default-deny: anything not explicitly allowlisted is `RequiresApproval` — never silently run.
- The `ControlledExecutor` runs a command **only** if its decision is `Allowed` and execute mode is on.
- `AutonomousExecutorTests` proves: denied + approval commands never reach the runner; allowed commands run only
  in execute mode; failure halts; nothing runs in dry-run.

## Extending the policy

Add to the denylist for new destructive/production patterns; add to the allowlist only for genuinely safe
read/build/test commands. Never weaken the gate to make automation "smoother" — approval is the safety budget.
