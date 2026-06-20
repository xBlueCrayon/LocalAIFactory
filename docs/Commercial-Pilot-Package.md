# Commercial Pilot Package

This document defines what a paid, industrial **pilot** of LocalAIFactory includes. It is written to be
honest and reproducible: every claim below is something the buyer can verify on their own hardware with the
scripts named here. LocalAIFactory is a **strong technical POC / early pilot** today — this package is scoped
to that reality, not to a finished enterprise product.

---

## 1. What the pilot is

A time-boxed, supervised deployment of LocalAIFactory on the buyer's infrastructure (MSSQL + Windows), loaded
with one or more of the buyer's own (sanitized) repositories, to prove three things on real code:

1. **Deterministic understanding** of C#/.NET, T-SQL, and Python — symbols, references, dependency graph.
2. **Impact analysis across the C#↔SQL and Python↔SQL bridges** — "what breaks if this stored procedure or
   table changes," in both directions.
3. **A governed Professional Base Knowledge Pack** (390 curated items, 22 categories, validated source
   registry) searchable alongside the imported project knowledge.

Everything runs locally and works in **MSSQL-only mode** — no GPU, no internet, no Ollama, no Qdrant required.

---

## 2. Scope — in

- Install on buyer hardware: LocalDB, SQL Express, or full SQL Server (buyer's choice).
- Database create / seed / verify via `database/*` scripts.
- Install + verify the Professional Base Knowledge Pack (`database/verify-knowledge-base.ps1`).
- Import up to **N buyer repositories** (agreed at kickoff) through the import wizard, including imperfect
  repositories (the ingestion pipeline records skips and never crashes on a bad repo).
- Coverage / gap reporting per repository (honest: no silent zeros).
- Impact analysis demonstrations across C#↔SQL and Python↔SQL bridges on the buyer's code.
- Windows authentication, RBAC (Viewer/Analyst/Admin), per-project access grants, append-only audit.
- Admin + user setup, and a knowledge-transfer session for the buyer's admins and analysts.
- Reproducible evidence run (`scripts/poc/verify-poc.ps1`, benchmark suites, `db-verify`).

## 3. Scope — out (honest exclusions)

These are **not** included because they are not yet shipped capability:

- **OCR / cheque / PDF intelligence** — prototypes and design only; no production CV/parser engine.
- **Financial market prediction / forecasting** — analysis framing only; not advice, not a shipped engine.
- **SSO / external IdP** — authentication is Windows (Negotiate) only; no SAML/OIDC.
- **Legacy parsing** beyond C#/T-SQL/Python — VB6, Oracle PL/SQL, Razor, etc. are reported gaps, not failures.
- **Cross-repository "estate" model** — each repo is understood independently; no unified estate graph yet.
- **Autonomous code modification in production** — the controlled workspace runs **dry-run + allowlisted**
  only and executes nothing destructive.
- **Commercial licensing enforcement** — there is no license gating yet; the pilot is governed by contract.
- **Proven production deployment / multi-tenant scale / SLAs** — not demonstrated; pilot is single-instance.
- **Compliance / certification / vendor-equivalence claims** — none are made. Domain knowledge is
  advisory/awareness only, **not legal, regulatory, tax, audit, or financial advice**.

---

## 4. Deliverables

| Deliverable | Form |
|---|---|
| Installed, running instance on buyer hardware | Web app, MSSQL-only, core pages <1s |
| Buyer repositories imported with coverage/gap report | In-app coverage page + import wizard records |
| Impact-analysis walkthroughs (C#↔SQL, Python↔SQL) | Live demo + saved queries on buyer code |
| Professional Base Knowledge Pack installed + verified | `verify-knowledge-base.ps1` PASS output |
| Security setup (users, roles, project grants, audit) | Admin pages + audit trail |
| Reproducible evidence pack | `verify-poc.ps1` + benchmark + `db-verify` transcripts |
| Pilot report | Findings, coverage numbers, readiness scorecard delta |

---

## 5. Environment & prerequisites

- Windows Server or Windows workstation; .NET 10 runtime.
- SQL Server reachable (LocalDB / Express / full) — **the authoritative store**.
- Optional and **not required**: Ollama (verified with `qwen2.5-coder` / `deepseek-r1`) and Qdrant for
  embeddings; a GPU (reference: RTX 5070 Ti) accelerates optional local inference only.
- Buyer provides sanitized repositories and a domain contact for sign-off.

See `docs/Industrial-Installation-Guide.md`, `docs/SQL-Server-Deployment-Guide.md`, and
`deploy/docs/hardware-sizing-guide.md` for sizing and install detail.

---

## 6. Buyer-reproducible evidence

The buyer does not have to trust the vendor's word. Every pilot claim maps to a command the buyer runs:

| To verify… | Run | Expected |
|---|---|---|
| Build is clean, tests + benchmark pass, artifacts present | `scripts/poc/verify-poc.ps1` | `VERIFY-POC: PASS` |
| Core UI works (no 500s, Base Knowledge search works) | `scripts/poc/ui-smoke-test.ps1` | all pages 200 |
| Knowledge pack installed correctly in MSSQL | `database/verify-knowledge-base.ps1` | `KNOWLEDGE-BASE: VERIFIED` |
| Capability holds with no regressions | benchmark `--suite Smoke\|Standard\|Extended` | `Result: PASS` |
| Database created/seeded/restorable | `database/create-*`, `seed-*`, `restore-verify-database.ps1` | scripts exit 0 |
| Instance healthy after install | `scripts/release/post-install-healthcheck.ps1` | health check passes |

The benchmark runs against pinned public fixtures (ERP/CRM + core-banking capability fixtures, **Gold 6/6**)
with golden snapshots, so a regression in the buyer's instance is detectable, not subjective.

---

## 7. Duration & cadence

- **Suggested duration:** 4–8 weeks.
  - Week 1: install, DB setup, pack install + verify, admin/user setup.
  - Weeks 2–3: import buyer repositories, coverage/gap review, fix import gaps.
  - Weeks 4–6: impact-analysis demonstrations on buyer code, knowledge search, analyst enablement.
  - Final week: evidence run, pilot report, readiness scorecard delta, sign-off.
- **Cadence:** weekly checkpoint against the success criteria below.

---

## 8. Success criteria

The pilot is considered successful when, on the buyer's own hardware and code:

1. All core pages load in under one second in MSSQL-only mode.
2. At least the agreed buyer repositories import to **Completed** with a coverage/gap report (imperfect repos
   are tolerated — skips recorded, no crash).
3. Impact analysis returns correct blast radius for an agreed set of "what breaks if X changes" scenarios
   across the C#↔SQL and/or Python↔SQL bridges.
4. The Professional Base Knowledge Pack verifies (`KNOWLEDGE-BASE: VERIFIED`) and is searchable in-app.
5. `verify-poc.ps1` and `db-verify` pass on the buyer's instance.
6. Buyer admins can manage users, roles, and project grants, and review the append-only audit trail.

---

## 9. Support level during the pilot

- **Business-hours support** from the implementing engineer/consultant.
- Triage via `docs/Support-Runbook.md` and `docs/Troubleshooting-Guide.md`; diagnostics collected with the
  health-check script and `RequestTimingMiddleware` logs.
- Backup/restore guidance via `database/backup-database.ps1` / `restore-database.ps1`.
- No 24/7 SLA, no on-call rotation, no guaranteed response time — this is a pilot, not a supported product.

---

## 10. What must be true before this becomes a product sale

Stated honestly so the buyer can judge maturity: before a *commercial* (not pilot) sale, the platform needs
licensing enforcement, hardened packaging/installer, defined support tiers, SSO, a cross-repository estate
model, and a proven production deployment. See `docs/Edition-and-Licensing-Strategy.md` and the live readiness
scorecard at `/Readiness` (≈49% overall and rising) for the honest current state.
