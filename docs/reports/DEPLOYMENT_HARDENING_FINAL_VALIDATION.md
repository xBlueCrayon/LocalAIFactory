# Deployment Hardening — Final Validation Gates

**Date:** 2026-06-21 · **Branch:** `ke-008-code-symbols` · **Mode achieved:** **C** (published app + SQL Express)

All gates run live this phase. No test weakened; no failure hidden.

## Gate results

| Gate | Command | Result |
|---|---|---|
| Build (Release) | `dotnet build LocalAIFactory.sln -c Release` | **PASS** — 0 errors |
| Tests | `dotnet test` | **PASS** — **240 / 240** |
| POC verification | `scripts/poc/verify-poc.ps1` | **PASS** (build + test + benchmark + artifacts + LocalDB + Ollama) |
| Benchmark | `--inmemory` | **PASS** — 10 repos, 47/47 POV, 9 Gold + 1 Bronze |
| Enterprise reasoning | `scripts/benchmark/run-enterprise-reasoning-benchmark.ps1` | **PASS** — mean 94.5 |
| UI smoke | `scripts/poc/ui-smoke-test.ps1` | **PASS** |
| Knowledge packs | `scripts/knowledge/verify-all-knowledge-packs.ps1` | **PASS** — 4 packs / 438 (main LocalDB, intact) |
| DB full install | `database/verify-full-install.ps1` | **PASS** (main LocalDB) |
| Release package | `scripts/release/verify-release-package.ps1` | **PASS** |
| Clean-install simulation | `scripts/release/simulate-clean-install.ps1` | **PASS** |
| Security audit | `scripts/security/security-audit.ps1` | **PASS** — 0 HIGH |
| **Post-deploy healthcheck (NEW)** | `scripts/deployment-drill/09-post-deploy-healthcheck.ps1` | **PASS** (Mode C, SQL Express) |

## Deployment proof (Mode C) — executed

| Aspect | Result |
|---|---|
| Mode | **C** — published app + **SQL Server Express 2022**, no IIS |
| Database | fresh `LocalAIFactory_DeploymentProof` on `.\SQLEXPRESS`; **14** migrations; **4 packs / 438 items** |
| HTTP | **13 routes → 200, 0 HTTP 500s**; DB-backed search returns real matches |
| Healthcheck | `09-post-deploy-healthcheck` **PASS** |
| Rollback | app stopped, **port 8095 freed**, 0 processes left, clean teardown |
| Main LocalDB | **untouched** (verify-all-knowledge-packs + verify-full-install still PASS, 4/438) |

## Repository hygiene

```
git ls-files | <bin obj .tmp publish .bak .log node_modules release*.zip>  -> NONE forbidden
git ls-files | <files > 5 MB>                                              -> NONE
```

All deployment scratch (`.tmp-publish`, `.tmp-release`, `.tmp-deploy-*`, `.tmp-deployment-evidence`) is
git-ignored and **not** committed. No publish output, release ZIP, logs, DB backups, or secrets staged.

## Process state

The deployed app (PID 30164) was stopped during the rollback proof; build-server nodes were shut down.
**0** `dotnet`/app processes remain.

## Score change

**Deployment Readiness 70 → 73** (modest; published-app + SQL Express executed — **not IIS, not
production**). Mean ≈ 59.5% → 60.6% (prior phase) → **60.8%**, max 88, **none at 100**.

## Verdict

All gates **green**; the deployment proof is **executed and healthy** (Mode C). The repository and release
package remain handover-ready; the draft `v1.0.0-rc` remains review-ready and unpublished. Commercial GA
remains blocked on a real IIS/production deployment, SSO, external security review, and a signed pilot.
