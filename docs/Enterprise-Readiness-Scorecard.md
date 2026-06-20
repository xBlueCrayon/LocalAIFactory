# Enterprise Readiness Scorecard (human-readable)

Companion to [`readiness-scorecard.json`](readiness-scorecard.json) (the authoritative source) and the live
`/Readiness` page. Scores are conservative; 100% means implemented + tested + demonstrated + documented +
reviewable. See [`Readiness-Maturity-Model.md`](Readiness-Maturity-Model.md) for the scoring discipline.

**As of:** commit `138a59b` (R2-ACC-B3); scorecard ships in R2-ACC-POC-ENTERPRISE. **Reviewed:** 2026-06-21.

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

**Overall mean ≈ 45%** — a strong technical POC / early pilot with a clear, honest path forward. Core
engineering (security, repository understanding, ingestion, audit, governance, benchmark) is the most mature;
domain *implementation*, deployment/ops, and commercial/autonomous areas are deliberately low until shipped
and proven. The per-area `proofRequiredFor100` in the JSON defines exactly what raises each score.
