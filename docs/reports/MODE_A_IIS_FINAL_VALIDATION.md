# Mode A (IIS) — Final Validation Gates

**Date:** 2026-06-21 · **Branch:** `ke-008-code-symbols` · **Mode achieved:** **A** (real IIS + SQL Express, pilot)

All gates run live this phase. No test weakened; no failure hidden.

## Gate results

| Gate | Result |
|---|---|
| `dotnet build -c Release` | **PASS** — 0 errors |
| `dotnet test` | **PASS** — **240 / 240** |
| `scripts/poc/verify-poc.ps1` | **PASS** (build + test + benchmark + artifacts + LocalDB + Ollama) |
| `scripts/poc/ui-smoke-test.ps1` | **PASS** |
| `scripts/knowledge/verify-all-knowledge-packs.ps1` | **PASS** — 4 packs / 438 (main LocalDB, **intact**) |
| `database/verify-full-install.ps1` | **PASS** (main LocalDB intact) |
| `scripts/release/verify-release-package.ps1` | **PASS** |
| `scripts/release/simulate-clean-install.ps1` | **PASS** |
| `scripts/security/security-audit.ps1` | **PASS** — 0 HIGH |
| `scripts/benchmark/run-enterprise-reasoning-benchmark.ps1` | **PASS** — mean 94.5 |
| **`scripts/deployment-drill/11-iis-mode-a-healthcheck.ps1`** | **PASS** — IIS endpoint, 0 HTTP 500s |

## Mode A proof — executed

| Aspect | Result |
|---|---|
| IIS enablement | `IIS-WebServerRole` + ManagementConsole + WindowsAuthentication enabled via `dism` (**no reboot**); `W3SVC` Running |
| Hosting / ANCM | **ASP.NET Core Hosting Bundle 10.0.9** (winget) → `AspNetCoreModuleV2` registered |
| Site / app pool | `LocalAIFactoryPilot` (Started) / `LocalAIFactoryPilotPool` (No Managed Code, ApplicationPoolIdentity, Started) |
| Served through IIS | **`Server: Microsoft-IIS/10.0`** — 7 routes 200 + search OCR57/Mauritius90/market23, **0 HTTP 500s** |
| Database | SQL Express `LocalAIFactory_IISProof`, 14 migrations, 4 packs/438 items |
| Least privilege | app-pool login `db_datareader + db_datawriter + EXECUTE` (no db_owner) — proven sufficient at runtime |
| Windows auth | IIS-WindowsAuthentication enabled; **401 Negotiate challenge** demonstrated (full round-trip = production posture, not done) |
| Rollback | stop frees port 8095, restart restores HTTP 200 — proven |
| Main LocalDB | **untouched** (knowledge + full-install gates still PASS) |

## Repository hygiene

```
git ls-files | <bin obj .tmp publish .bak .log node_modules inetpub release*.zip .mdf .ldf>  -> NONE
git ls-files | <files > 5 MB>                                                                -> NONE
```

The IIS physical folder (`C:\inetpub\LocalAIFactoryPilot`), publish output, release ZIP, support bundle,
event logs, and all `.tmp-*` scratch are **not** committed.

## Score change

**Deployment Readiness 73 → 80** · **Controlled Pilot 70 → 72**. Mean ≈ 60.8% → **61.2%**, max 88,
**none at 100**.

## Proof ladder

**Local POC ✅ → Published-app + SQL Express ✅ → IIS pilot ✅ → Production ⬜ → Commercial GA ⬜**

## Verdict

All gates **green**; Mode A (real IIS + ANCM + least-privilege SQL Express) **executed and healthy**. The
draft `v1.0.0-rc` remains review-ready and unpublished. Commercial GA remains blocked on a **production**
deployment (HTTPS, Server edition, full Negotiate+RBAC, staged rollout), enterprise SSO, external security
review, and a signed customer pilot.
