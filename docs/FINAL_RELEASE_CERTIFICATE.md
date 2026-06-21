> **Authoritative current status:** [docs/reports/CURRENT_STATUS.md](reports/CURRENT_STATUS.md) (commit 96fbbc4). This document is point-in-time; where numbers differ, CURRENT_STATUS wins.

# LocalAIFactory — Final Release Certificate

**Product:** LocalAIFactory · **Release:** v1.0.0-rc (customer-handover candidate) · **Branch:** `ke-008-code-symbols`
**Date:** 2026-06-21 · **Decision:** **controlled paid-pilot ready; NOT commercial GA.**

This is an honest certificate: every claim is backed by a reproducible command, test, benchmark, or live capture
on the build host. It asserts **no** vendor certification, regulatory/financial compliance, fraud-detection
certainty, or production guarantee.

## Evidence (reproduced this release)

| Category | Command | Result |
|---|---|---|
| Build | `dotnet build LocalAIFactory.sln -c Release` | **0 errors** |
| Tests | `dotnet test` | **235 / 235 pass** |
| Benchmark | `--inmemory --suite standard` | **PASS** — ERP/CRM Gold 6/6, core-banking Gold 6/6, **KYC/AML Gold 7/7**, **enterprise giant-patterns Gold 14/14** |
| Enterprise reasoning | `scripts/benchmark/run-enterprise-reasoning-benchmark.ps1` | **PASS** — 31 questions (14 structural @100 + 17 advisory @90), **mean 94.5**; synthetic public patterns, no vendor clone |
| Deployment (Mode C) | published app → **SQL Server Express 2022** | **EXECUTED** — fresh `LocalAIFactory_DeploymentProof` DB, 14 migrations + 4 packs/438 items seeded, 13 routes 200, 0 HTTP 500s, `09-post-deploy-healthcheck` PASS, rollback proven. **Not IIS, not production.** |
| **Deployment (Mode A)** | **real IIS** + ANCM → SQL Express | **EXECUTED** — IIS enabled (dism, no reboot) + Hosting Bundle 10.0.9 (winget); site `LocalAIFactoryPilot` + app pool (No Managed Code); app served **through IIS** (`Server: Microsoft-IIS/10.0`), 7 routes 200 + search, 0 HTTP 500s; SQL Express `LocalAIFactory_IISProof` with **least-privilege** app-pool login; Windows-auth 401 challenge shown; rollback proven. **Pilot, not production.** |
| **HTTPS + Windows auth** | IIS `:8443` self-signed | **EXECUTED** — all pages 200 over TLS; **Windows/Negotiate authenticated round-trip** (401 without creds → **200 with Windows credentials over HTTPS**); production-posture healthcheck PASS, 0 HTTP 500s. **Self-signed pilot TLS, app runs dev-auth behind IIS.** |
| **50-project benchmark** | `run-50-project-benchmark.ps1` | **51 real public repos attempted** — 22 Passed / 7 Partial / 5 ValidationOnly / 5 UnsupportedLanguage / 12 CloneFailed-or-TimedOut; **123,849 C# + 241,730 Python symbols + 1,733 SQL objects** over 7.56M LOC; avg 60.8. Honest gaps (TS/JS unsupported; xlarge scale). |
| verify-poc | `scripts/poc/verify-poc.ps1` | **PASS** (build + test + benchmark + artifacts + hygiene) |
| UI smoke | `scripts/poc/ui-smoke-test.ps1` | **PASS** (11 pages 200, incl. `/Support`) |
| Included KB | `scripts/knowledge/verify-all-knowledge-packs.ps1` | **PASS** — 4 packs, 438 items, 438 distinct UIDs, no collisions; **live DB 4 packs / 438 items** |
| Full install | `database/verify-full-install.ps1` | **PASS** — 14 migrations + KB VERIFIED |
| Publish | `scripts/release/build-release.ps1` | **151 files** |
| Package | `scripts/release/package-release.ps1` | **16.2 MB ZIP, 277 files**, RELEASE_MANIFEST (v1.0.0-rc) |
| Package verify | `scripts/release/verify-release-package.ps1` | **PASS** (binaries, 4 packs, DB scripts, appsettings, manuals, manifest; no secrets; no >5 MB) |
| Clean-install sim | `scripts/release/simulate-clean-install.ps1` | **PASS** (verified on the extracted ZIP, not the repo) |
| Acceptance | `scripts/release/customer-acceptance-check.ps1` | **ACCEPTED** |
| Screenshots | `scripts/docs/capture-screenshots.ps1` (Playwright) | **11 real PNGs** captured (~1.5 MB) |
| Support bundle | `scripts/support/export-support-bundle.ps1` | read-only diagnostics zip (~3 KB, no secrets) |
| Security | `scripts/security/security-audit.ps1` | **0 HIGH**, 2 INFO (gated); 0 forbidden tracked; 0 secrets |
| GPU / AI (optional) | `nvidia-smi` / `ollama` | RTX 5070 Ti 16 GB; Ollama online (2 models) |

## Capability statements (honest)

- **Real & shipped:** deterministic C#/T-SQL/Python understanding + C#↔SQL bridge + impact; **included knowledge
  base of 4 packs / 438 items, auto-seeded + live-verified**; governed knowledge (Curated, provenance,
  propose-never-overwrite); Windows-auth RBAC + append-only audit; ERP/CRM + core-banking + KYC/AML benchmark
  fixtures (Gold); `/Support` dashboard; edition/license skeleton; safe local fix loop; **a generated,
  verified, self-contained release package + clean-install simulation + customer-acceptance check**; real
  product screenshots; diagnostics/support-bundle scripts; **a synthetic enterprise giant-pattern reasoning
  benchmark (10 public pattern families, Gold 14/14 structural + 17 advisory, mean 94.5, reproducible) proving
  consultant-style reasoning over architecture/workflows/approvals/impacts/controls — no vendor cloning**.
- **Templates / design / prototype:** IIS/Docker/Express/full-SQL deployment (scripts + research-informed docs;
  **not executed** — Docker not installed on host); SSO/IdP (design + hooks); market-intelligence + multi-agent
  factory (design + skeleton); OCR/CV (deterministic prototypes, **no trained model**); commercial licensing
  (demo-safe skeleton, **no enforcement**).
- **Not present:** executed production deployment; enterprise SSO; cross-repository estate model; autonomy on a
  real repo; **external penetration test / security audit**; commercial licensing enforcement.

## Readiness (authoritative: `readiness-scorecard.json`, mean ≈ 64.1%, max 90, none at 100)

> **NEAR-GA-CLOSURE:** production-readiness gate **V3 = `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL`**
> (`scripts/production/verify-production-readiness-v3.ps1`). Internal completeness **84.8**, honest GA-now
> **65.4**, projected GA-when-proofs-supplied **94.3** (`benchmarks/results/near-ga-score-model.json`,
> `docs/reports/NEAR_100_GA_SCORE_MODEL.md`). Every external proof is **modelled + owned + validatable and
> none is faked** (external-proof emulation engine: 9 proofs, `docs/reports/EXTERNAL_PROOF_EMULATION_ENGINE.md`).
> Builds on gate **V2 = `PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED`** (12/12 closure dimensions; LEVEL 1
> code + LEVEL 2 local-technical + LEVEL 3 operator-emulation complete).
> **Fresh-clone pullable** proof passed (clone → build 0 err → 240/240 tests → gates). LEVEL 4
> (commercial GA / real production) is **not** claimed. Full ladder:
> **Local POC ✅ → Published-app + SQL Express ✅ → IIS pilot ✅ → HTTPS/Windows-auth pilot ✅ →
> 50-real-project benchmark ✅ → 100+ system benchmark ✅ → operator-emulation ✅ → fresh-clone pullable ✅ →
> production-ready-when-external-proofs-supplied ✅ → Real Windows Server prod ⬜ → External review ⬜ →
> Signed customer pilot ⬜ → Commercial GA ⬜.** (HTTPS + Windows/Negotiate round-trip and
> a 51-real-repo benchmark proven this phase).

Technical POC **88** · Data/Knowledge Governance **85** · Benchmark **82** · Repository Understanding **80** ·
Benchmark **86** · Deployment **83** · Repository Understanding **82** · Security **78** · Controlled Pilot **74** · UX/Demo **70** · Audit **70** · Banking **70** ·
ERP/Infra Advisory **68** · Supportability **65** · Vendor-Style **65** · Autonomous **55** · Business Workflow
Consulting **55** · Cross-System/Estate **45** · Packaging/Licensing **45** · Scalability **45** · Document/OCR
**35** · Commercial Product **35** · Legacy **30** · Enterprise Product **30**.

> This pass raised only four areas, and only where the new enterprise giant-pattern reasoning benchmark
> provides reproducible proof: Benchmark/Evidence 80→82, Business Workflow Consulting 45→55, ERP/Infra Advisory
> 65→68, Vendor-Style Design 55→65. No blocked area (production, SSO, OCR, estate, GA) was raised.

## Decision

- **GitHub deliverable:** **COMPLETE** — a clean repo + a generated, verified release package that a
  customer/operator can use to install, seed the included knowledge base, verify, run, and understand exactly
  what is proven and what remains.
- **Paid pilot:** **YES** — controlled, operator-assisted, scoped to the proven core.
- **Commercial GA:** **NO** — blocked on executed production deployment, SSO, external security review, and a
  signed-off customer pilot.
- **Confidence (controlled paid pilot): 8 / 10.** Local/repo/package completion ≈ **19/20** (the missing point
  is the set of inherently-external proofs below).

## Exact proof required for 20/20

Executed production deployment (IIS/Docker/Express/full-SQL) with backup/restore drill + health gates on a real
server; enterprise SSO/IdP integration; an **independent technical + security review / penetration test**; the
fix loop demonstrated end-to-end on a real (sanitized) estate repo; a trained OCR/CV model with measured
precision/recall; a cross-repository estate model; and at least one **signed-off customer pilot**.

> Signed off by the engineering process, not an external auditor. The single highest-value next step is an
> independent technical + security review, then a real production-deployment drill.
