# ERP Gold — Official Study & Requirement Decomposition (Phases 1–2)

**Date:** 2026-06-21 · **Sprint:** ERP-GOLD · **Source policy:** clean-room. No ERPNext/Frappe source
is copied or vendored; only structure and widely-established ERP/accounting principles are studied and
re-expressed as original summaries. No internet is used after the study phase.

## Phase 1 — Official study (built on existing inventories)

The authoritative ERP study for this codebase was completed in earlier sprints and is reused here rather
than duplicated. The gold reference is measured against these committed artifacts:

| Inventory / target | File |
|---|---|
| DocType (entity) inventory | `benchmarks/erpnext-study/erpnext-doctype-inventory.json` |
| API inventory | `benchmarks/erpnext-study/erpnext-api-inventory.json` |
| Feature inventory | `benchmarks/erpnext-study/erpnext-feature-inventory.json` |
| Report inventory | `benchmarks/erpnext-study/erpnext-report-inventory.json` |
| Role/permission inventory | `benchmarks/erpnext-study/erpnext-role-permission-inventory.json` |
| Workflow inventory | `benchmarks/erpnext-study/erpnext-workflow-inventory.json` |
| Parity target matrix | `benchmarks/erpnext-study/erpnext-parity-target-matrix.json` |
| Grade target / gates | `benchmarks/erpnext-study/erpnext-grade-target-gates.json` |
| Official source registry | `benchmarks/erpnext-study/erpnext-official-source-registry.json` |
| Narrative study | `docs/reports/ERPNEXT_FEATURE_AND_ARCHITECTURE_STUDY.md`, `ERPNEXT_GRADE_TARGET_DEFINITION.md` |

**Gold-relevant findings carried into this sprint:** (1) a real ERP needs **enforced authentication and
role-based authorization**, not a dev-cookie identity — this was V5's largest gap and is the primary Gold
upgrade; (2) double-entry GL with debits=credits, typed chart of accounts, and maker/checker are the
correctness core (already implemented and tested); (3) production needs a **repeatable local deployment +
backup/restore/health** story (added this sprint as scripts).

## Phase 2 — LAF chatbot requirement decomposition

The Gold target `ERP_LOCAL_PRODUCTION_READY` decomposes into capability buckets; this sprint's honest
coverage is recorded against each (full detail in `ERP_GOLD_SCORECARD.md`):

| Capability bucket | Gold coverage |
|---|---|
| Runs & deploys locally (MSSQL/SQL Express + SQLite portable) | ✅ publish + run proven on Release EXE |
| Real authentication / authorization | ✅ PBKDF2 + cookie auth + role claims + seeded users |
| Tested accounting & stock correctness | ✅ double-entry GL, P&L/BS, stock ledger tests |
| Maker/checker + auditability | ✅ approval thresholds + audit events (incl. Login) |
| UI CRUD + APIs + reports | ◑ create/list/read + REST + reports (edit/delete UI not generated) |
| Backup/restore + deployment docs | ✅ scripts + README emitted with the product |
| Tests (xUnit + Playwright) | ✅ 138 xUnit + 16 Playwright pass |
| EF migration history (SQL Server) | ◑ `Database.Migrate()` wired; no committed migrations this sprint |
| Module breadth (stretch: 30 modules / 400 tests) | ✗ reference-grade subset only — stated up front as not single-session achievable |

**Decomposition decision:** prioritize the capabilities that move V5 from demo-grade to genuinely
deployable (real auth, deployment, audit-on-login) and fold every one into the **generator templates** so
LocalAIFactory reproduces them — over chasing module breadth that cannot be made correct and tested in one
session. Reproduction is proven in `benchmarks/results/erp-gold-reproduction-comparison.json` (≥80% target met).
