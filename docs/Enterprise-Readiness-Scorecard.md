# Enterprise Readiness Scorecard (human-readable)

Companion to [`readiness-scorecard.json`](readiness-scorecard.json) (the authoritative source) and the live
`/Readiness` page. Scores are conservative; 100% means implemented + tested + demonstrated + documented +
reviewable. See [`Readiness-Maturity-Model.md`](Readiness-Maturity-Model.md) for the scoring discipline.

**As of:** R2-ACC-CAPABILITY-MAX (capability sprint). **Reviewed:** 2026-06-21. The table below is the
pre-sprint baseline; the **authoritative current scores** live in `readiness-scorecard.json` and render at
`/Readiness`. The capability sprint raised specific scores — see the update note at the foot of this file.

| # | Area | Score | Confidence | Headline evidence | Top blocker |
|---|---|---:|---|---|---|
| 1 | Technical POC Readiness | 75 | High | build+132 tests+benchmark+live | no external review |
| 2 | Controlled Pilot Readiness | 55 | Med | auth+audit+ingestion+sizing | no backup/restore, supportability |
| 3 | Enterprise Product Readiness | 30 | Med | hardened core | no SSO, estate, scale |
| 4 | Commercial Product Readiness | 15 | High | comparison+demo | no licensing/packaging |
| 5 | Autonomous Engineering Readiness | 20 | High | guarded sandbox scaffold | no fix/test loop |
| 6 | Security & Access Control | 75 | High | RBAC+project access+audit+IDOR test | no SSO/pen-test |
| 7 | Audit & Governance | 70 | High | append-only audit+provenance+registry | no tamper-evidence |
| 8 | Data & Knowledge Governance | 70 | High | permanence+versioning+source registry | no PII/retention policy |
| 9 | Deployment Readiness | 40 | Med | MSSQL-only+sizing+auto-migrate | no installer/backup |
| 10 | Supportability & Operations | 35 | Med | health cache+timing+logs | no support dashboard |
| 11 | Benchmark & Evidence Credibility | 70 | High | 4 pinned repos+golden+exit codes | only 4 repos, no tiers wired |
| 12 | Business Workflow Consulting | 45 | Med | 21 playbooks+14 scenarios+rubric | no executed engagements |
| 13 | Banking / Finance Domain | 45 | Med | deep knowledge+banking scenarios | no delivered component |
| 14 | Document Intelligence / OCR | 20 | High | knowledge+design+research notes | no shipped engine |
| 15 | Legacy Modernization | 25 | High | VB6 playbook+scenario | no legacy extractor |
| 16 | Repository Understanding | 70 | High | C#/T-SQL symbols+graph+impact, Gold | C#/SQL only, syntactic |
| 17 | Cross-System / Estate Understanding | 30 | Med | object-scoped identity | no estate model yet |
| 18 | User Experience / Demo | 60 | High | working UI+smoke+demo script | no Playwright/UX review |
| 19 | Scalability & Performance | 40 | Med | bounded queries, <1s pages | no load testing |
| 20 | Commercial Packaging & Licensing | 15 | High | clean repo | no licensing/distribution |
| 21 | Enterprise ERP / Infra Advisory | 40 | Med | ERP/infra scenarios+comparison | advisory only |
| 22 | Vendor-Style Solution Design | 40 | Med | playbooks+scenarios+rubric | designs not yet delivered |

**Overall mean ≈ 45%** (pre-sprint) → **≈ 49%** after R2-ACC-CAPABILITY-MAX — a strong technical POC / early
pilot with a clear, honest path forward. Core engineering (security, repository understanding, ingestion,
audit, governance, benchmark) is the most mature; domain *implementation*, deployment/ops, and
commercial/autonomous areas remain deliberately low until shipped and proven. The per-area
`proofRequiredFor100` in the JSON defines exactly what raises each score.

## R2-ACC-CAPABILITY-MAX update (scores raised only where shipped, tested evidence improved)

| # | Area | Before → After | Why (shipped + tested) |
|---|---|---|---|
| 16 | Repository Understanding | 70 → **80** | C#↔SQL bridge (AccessesSql edges, both directions) + Python extractor; benchmark fixtures Gold |
| 11 | Benchmark & Evidence | 70 → **80** | Smoke/Standard/Extended tiers + `--suite` + 2 bridge fixtures (Gold 4/4) + governance metadata |
| 17 | Cross-System / Estate | 30 → **45** | code↔schema linking within a repo (C#/Python methods → SQL objects with evidence) |
| 5 | Autonomous Engineering | 20 → **35** | command allow/deny policy + dry-run planner (tested: dangerous commands denied, commit/push gated, executes nothing) |
| 9 | Deployment | 40 → **50** | docker-compose (cpu/gpu), Dockerfile, `.env.example`, backup/restore-verify/health/smoke/windows-deploy scripts (validated) |
| 14 | Document Intelligence / OCR | 20 → **30** | PDF analyzer (hash/classify/provenance) + extractive summarizer + cheque risk-triage engine (all tested) |
| 13 | Banking / Finance | 45 → **50** | "stored proc → C# blast radius" now demonstrable via the bridge fixture |
| 15 | Legacy Modernization | 25 → **30** | Python added as a supported language (VB6/Oracle still gap-only) |
| 10 | Supportability & Ops | 35 → **40** | health-check + deploy-smoke scripts |
| 1 | Technical POC | 75 → **80** | 199 tests, bridge UI evidence, two more benchmark fixtures |

No area reached 100. Nothing was raised without shipped, tested evidence. See `Gap-Closure-Roadmap-To-100.md`.

## R2-ACC-INDUSTRIAL-SHIP update (overall mean ≈ 49% → ≈ 55%)

Raised only where shipped + live-verified: Technical POC 80→**85** (207 tests, 6 benchmark items, publish
proven), Controlled Pilot 55→**70** (DB create/seed/verify + backup/restore live-OK + install/onboarding/admin
guides), Data/Knowledge Governance 70→**80** (KB install/verify scripts, 390 items), Deployment 50→**65**
(compose + DB scripts + backup/restore + publish + install/IIS dry-run + runbooks), Banking/Finance 50→**65**
(core-banking fixture Gold 6/6), ERP/Infra Advisory 40→**65** (ERP/CRM fixture Gold 6/6), Vendor-Style Design
40→**55**, Autonomous 35→**45** (ControlledExecutor tested), Supportability 40→**50**, Document/OCR 30→**35**,
Commercial 15→**25**. See `Industrial-Ship-Readiness-Certificate.md` for the aggregated evidence and the
paid-pilot decision. Authoritative scores live in `readiness-scorecard.json` (`/Readiness`). None at 100.

## R2-ACC-20X-FINAL-COMPLETION update (overall mean ≈ 55% → ≈ 57.6%)

Raised only where shipped + tested + live-verified: Technical POC 85→**87** (235 tests, KYC/AML fixture Gold
7/7, /Support in UI smoke, publish 151 files), Autonomous 45→**55** (LocalFixLoop: isolated workspace +
rollback proven + never-commit, 5 tests + operator script), Supportability 50→**60** (read-only /Support
dashboard live + diagnostics suite run live), Commercial Product 25→**30** and Packaging/Licensing 15→**30**
(edition/license model tested + edition matrix), Banking/Finance 65→**70** (KYC/AML→transaction-approval fixture
Gold 7/7), Data/Knowledge Governance 80→**82** (3 new installable packs real-installer-tested + chat-learning
extractor), Scalability 40→**45** (load/reliability smoke live: 0 failures), Security 75→**76** (security-audit
live: 0 HIGH), UX/Demo 60→**62** (Support dashboard + manuals + screenshot script). Honest non-raises:
Document/OCR **35**, Cross-System/Estate **45**, Enterprise Product **30**; screenshots NOT generated
(Node/Playwright absent — documented). See `Final-20X-Completion-Report.md`. **None at 100.**

## FINAL-ENTERPRISE-REASONING update (overall mean ≈ 59.5% → ≈ 60.6%)

Added a synthetic enterprise giant-solution reasoning benchmark (`benchmarks/fixtures/enterprise-giant-patterns`,
manifest `ENTGIANT`) over 10 **public** pattern families (CRM/Dynamics, Salesforce, ERP/SAP, Oracle/NetSuite,
ServiceNow ITSM, Temenos/Finastra/FIS core banking, Jira/Confluence workflow, Copilot/Sourcegraph
code-intelligence, Power BI/Tableau reporting, operating-manager workflow) — **NOT a vendor clone and NOT a
certified-compatibility claim**. The real structural harness scores it **Gold** (329 symbols, 79 edges, **14/14
POV**, golden-guarded); `scripts/benchmark/run-enterprise-reasoning-benchmark.ps1` scores **31 questions** (14
structural graph-proven @100 + 17 advisory design @90), **mean 94.5/100, PASS**. Raised only where this gives
reproducible proof: Benchmark/Evidence 80→**82**, Business Workflow Consulting 45→**55**, ERP/Infra Advisory
65→**68**, Vendor-Style Design 55→**65**. Blocked areas (production, SSO, OCR, estate, GA) **unchanged**. See
`Enterprise-Giant-Solution-Reasoning-Benchmark.md` and `reports/ENTERPRISE_REASONING_BENCHMARK_RESULTS.md`.
**None at 100.**

## DEPLOYMENT-HARDENING update (overall mean ≈ 60.6% → ≈ 60.8%)

IIS is not installed on this host, so the strongest **truthful** deployment was executed: **Mode C —
published-app binaries run against SQL Server Express 2022** with a fresh `LocalAIFactory_DeploymentProof`
database (app auto-migrated **14** + seeded **4 packs / 438 items**), **13 HTTP routes 200 / 0 HTTP 500s**,
new `scripts/deployment-drill/09-post-deploy-healthcheck.ps1` **PASS**, support bundle + rollback proven.
Honest **modest** raise (published-app fallback, **not IIS, not production**): **Deployment Readiness 70 →
73**. NOT raised: Controlled Pilot 70 (no real-estate-data pilot + sign-off), Enterprise Product 30 (no
IIS/SSO/production), SSO, external review, OCR/CNN, license enforcement. Deployment proof ladder: **Local
POC → Published-app (done) → IIS pilot → Production → Commercial GA**. See `reports/DEPLOYMENT_*`.
**None at 100.**
