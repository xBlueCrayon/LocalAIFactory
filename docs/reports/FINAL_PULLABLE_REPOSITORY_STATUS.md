# Final Pullable Repository Status

**Date:** 2026-06-21 · **Branch:** `ke-008-code-symbols` · **Remote:** `https://github.com/xBlueCrayon/LocalAIFactory.git`

## State

- Working tree: **clean** (after the final commit).
- Draft release `v1.0.0-rc`: **draft + prerelease**, unpublished; **no** final v1.0 tag.
- **Fresh-clone pullable: proven** — see `FRESH_CLONE_PULLABLE_REPO_PROOF.md` (clone → restore → build 0
  errors → 240/240 tests → verify-poc → production gate, all from a clean GitHub clone).
- Production-readiness gate **V2 = `PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED`**.

## Exact pull commands for a fresh operator machine

```powershell
git clone https://github.com/xBlueCrayon/LocalAIFactory.git
cd LocalAIFactory
git checkout ke-008-code-symbols
dotnet restore
dotnet build LocalAIFactory.sln -c Release
dotnet test
pwsh scripts/poc/verify-poc.ps1 -Fast
pwsh scripts/production/verify-production-readiness.ps1
pwsh scripts/production/verify-production-readiness-v2.ps1
```

Prerequisites: **.NET 10 SDK**, **SQL Server LocalDB**, **PowerShell 7+**. Optional (only for the IIS / local-LLM
proofs): SQL Express + IIS + ASP.NET Core Hosting Bundle + Ollama. The core build/test/gates run without them.

## Remaining external/human/customer blockers (LEVEL 4)

Real Windows Server host · CA-issued TLS + domain · real Entra/OIDC tenant · external penetration test ·
signed customer pilot · commercial license enforcement · monitoring/alerting + incident on-call. All are
**EMULATED** in `operator-emulation/` with clear pass criteria; none is claimed as real.

## Exact next human action

Provision a Windows **Server** host; install a **CA-issued certificate** for a real domain; set
`ASPNETCORE_ENVIRONMENT=Production` and bind app RBAC to the IIS Windows identity (domain service account);
commission an **external pen-test** + an **Entra/OIDC** tenant; run a **signed customer pilot**. Then re-run
`verify-production-readiness-v2.ps1` — it advances beyond `PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED`
only when those real proofs exist.
