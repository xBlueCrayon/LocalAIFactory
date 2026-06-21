# LAF Safe Tool Gateway V1 — Report

Component: `src/LocalAIFactory.Reasoning/Safety`
Tests for this component: **~40 PASS** (includes xUnit Theory cases; part of the 113 reasoning-engine tests).

## What was built

`SafeToolGateway` — the chokepoint that classifies, decides, and logs **every** tool/command. No model
or agent bypasses it.

- **`CommandRiskClassifier`** — **default-deny**, four tiers:
  - `ReadOnly` — e.g. `git status`, `git diff`, `ls`, `findstr`, `get-content`.
  - `SafeValidation` — e.g. `dotnet build`, `dotnet test`, `dotnet restore`, `playwright test`,
    knowledge/readiness/security scans.
  - `ControlledWrite` — e.g. `apply-patch`, `git apply`, `write-file`, `update-template`,
    `update-knowledge`.
  - `Forbidden` — e.g. `git push`, `git commit`, `git reset --hard`, `git clean`, `rm -rf`,
    `drop database`/`drop table`, `curl`/`wget`/`invoke-webrequest`, `npm install`/`pip install`,
    `shutdown`, `reg add`/`reg delete`, and more.

  **Forbidden wins over every other tier**, and any command not on an allowlist resolves to
  **Forbidden** (default-deny).
- **`PathSandboxGuard`** — blocks **absolute paths** and **`..` traversal** that escape a single
  worktree root.
- **Controlled writes require an isolated worktree.** Without one, a write is not allowed; with one,
  every write path must resolve inside the worktree.
- **Every decision is logged** (`SafeExecutionLogEntry`: command, risk, allowed, reason, stamp tag).

## What it proves

- The agent surface is **safe by construction**: unknown and destructive commands are blocked by
  default, writes cannot escape the sandbox, and there is a complete audit log.
- The four-tier classification and the sandbox guard are exhaustively unit-tested, including Theory
  cases that sweep representative commands per tier and traversal/absolute-path escapes.

## Honest limitations / not met

- **Target not met:** the ambitious target of **60+** safety tests was not reached (~40 delivered,
  Theory cases included).
- **Substring/allowlist classification.** The classifier matches against string allowlists; it is
  deliberately conservative (default-deny) but is not a full command parser, so obfuscated or
  shell-chained commands rely on the default-deny fallback rather than precise parsing.
- The gateway governs the engine's own tool calls; it does not sandbox arbitrary OS-level process
  execution beyond classification and path-guarding.
