# Fresh-Clone Pullable Repository Proof

**Date:** 2026-06-21 · **MANDATORY proof — executed.**

The repository was cloned **from scratch** from GitHub into a temporary, git-ignored path and built/tested/
gated end-to-end. No local working-tree state was used.

| Step | Command | Result |
|---|---|---|
| Clone | `git clone https://github.com/xBlueCrayon/LocalAIFactory.git` | **OK** |
| Checkout | `git checkout ke-008-code-symbols` | cloned commit **`0504815`** |
| Restore | `dotnet restore LocalAIFactory.sln` | **OK** (1.5 s) |
| Build | `dotnet build LocalAIFactory.sln -c Release` | **0 errors** (7.7 s) |
| Test | `dotnet test` | **240 / 240 pass** |
| Fast gate | `scripts/poc/verify-poc.ps1 -Fast` | **PASS** (artifacts + hygiene + LocalDB config + Ollama) |
| Production gate | `scripts/production/verify-production-readiness.ps1` | **PILOT_READY** (19 PASS / 0 FAIL) |

## Prerequisites for a fresh operator machine

- **.NET 10 SDK** (build + test).
- **SQL Server LocalDB** (the app's default connection; auto-migrates + seeds on first run).
- **PowerShell 7+** (`pwsh`) for the scripts.
- Optional: SQL Server **Express** + **IIS** + **ASP.NET Core Hosting Bundle** (only for the Mode A/IIS proofs);
  **Ollama** (only for the local-LLM proof). The core build/test/gates run **without** these.

## Final operator clone command

```powershell
git clone https://github.com/xBlueCrayon/LocalAIFactory.git
cd LocalAIFactory
git checkout ke-008-code-symbols
dotnet restore
dotnet build LocalAIFactory.sln -c Release
dotnet test
pwsh scripts/poc/verify-poc.ps1 -Fast
pwsh scripts/production/verify-production-readiness.ps1
```

## Result

**Pullable + buildable + testable + gate-passing from a clean clone.** The temporary clone
(`.tmp-fresh-clone-proof/`, git-ignored) was deleted after the proof and is **not** committed.
