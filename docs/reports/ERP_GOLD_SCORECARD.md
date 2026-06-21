# LAF Enterprise ERP Gold — Honest Scorecard

**Date:** 2026-06-21
**Sprint:** ERP-GOLD — hand-code an ERPNext-grade gold reference and train LocalAIFactory to reproduce it.
**Products:** `generated-products/LAF-EnterpriseERP-Gold` (hand-built reference) and
`generated-products/LAF-EnterpriseERP-GoldGenerated` (pure generator reproduction).

This scorecard is deliberately conservative. It does **not** claim ERPNext parity, full production
certification, or 100% anything. It records exactly what was built, tested, and proven, and names the gaps.

---

## 1. What ERP Gold actually is

A focused **productionization** of the proven LocalAIFactory ERP generator output, adding the genuine
production capabilities the earlier V5 product lacked — captured as **reusable generator templates** so
the generator (and the GoldGenerated reproduction) emit them deterministically.

The defining ERP-GOLD discipline: **no manual code was left as a one-off product edit.** Every hand-coded
upgrade was written into `tools/LocalAIFactory.Generator/templates`, `specs/`, and `Program.cs`, then
re-emitted. That is why the generator can reproduce the reference (see §4).

---

## 2. Genuine production upgrades added this sprint (over V5)

| Capability | Evidence |
|---|---|
| **Real authentication** — PBKDF2 (SHA-256, 100k iters, per-user salt, fixed-time compare) | `templates/.../PasswordHasher.cs`; `AuthTests` round-trip + wrong-password tests pass |
| **Username/password login** against seeded users with hashed passwords | `UserAuthService.Authenticate`; seeded admin/alice/bob with PBKDF2 hashes |
| **Cookie authentication + role claims** (`LafErpCookie`, Name + Role claims) | `Program.cs`, `AccountController`, `HttpCurrentUser` prefers the authenticated principal |
| **Login UI** with stable Playwright hooks | `Views/Account/Login.cshtml` (data-testid login-form/username/password/login-submit/login-error) |
| **Login auditing** | `UserAuthService` records a "Login" `AuditEvent` and persists it; `Login_records_an_audit_event` test passes |
| **Local-production deployment** | `scripts/publish-local-production.ps1` (Release publish, SQLite default / SQL Server via `ConnectionStrings__Default`) |
| **Backup / restore / health** | `scripts/backup-restore-health.ps1` |

**End-to-end deployment proof** (`deployment-evidence/login-deployment-proof.json`): the **published Release
EXE** was run locally; login page returned 200, a **wrong password re-rendered the error**, the **correct
password issued a `LafErpCookie` and a 302 redirect**, and the **authenticated dashboard returned 200**.

---

## 3. Test & build evidence

| Metric | ERP Gold | GoldGenerated |
|---|---|---|
| .NET build | 0 errors | 0 errors |
| xUnit tests | **138 pass** (134 + 4 auth) | **128 pass** |
| Playwright tests | **16 pass** (incl. 2 real-login) | 16 pass |
| Generator autonomy | 100% (LLM proposals governed) | 100% |
| Modules | 28 (23 spec + 5 governed LLM) | 23 (spec only) |

The accounting suite proves double-entry GL (debits = credits), P&L vs balance-sheet classification, stock
ledger, **maker/checker** approval thresholds, RBAC, and audit — all carried over from the validated engine.

---

## 4. Reproduction result (Phase 16 — train the generator to reproduce Gold)

`benchmarks/results/erp-gold-reproduction-comparison.json`. GoldGenerated was emitted with the **new
`--mode erp-gold`** (defaults to `specs/erp-gold-reference.json`), purely deterministically, no LLM proposal.

- **Deterministic engine + auth + deployment surface reproduced: 100%.** The entire hand-coded production
  surface lives in templates/spec, so it regenerates identically (auth layer, login UI, role claims, audit,
  deployment scripts, create UI, 23/23 spec modules, 128 tests).
- **Module reproduction: 23/28 = 82%.** **Test reproduction: 128/138 = 93%.** Both **above the ≥80% target.**
- The only Gold-exclusive content is the **5 non-deterministic local-LLM-proposed catalog modules** and their
  ~10 tests; a deterministic run does not regenerate them, and that is reported honestly rather than hidden.

## 5. Local model eval (Phase 3)

`benchmarks/results/erp-gold-ollama-model-eval.json`. Both local models present and responding (no internet):
`qwen2.5-coder:14b` answered an ERP-accounting reasoning probe in ~13 s; `deepseek-r1:14b` consumed its small
token budget on hidden reasoning (it is a reasoning model). **Role decision:** qwen2.5-coder = primary
code-oriented proposer/reviewer; deepseek-r1 = planner/second-opinion. **Authority:** local models propose and
review only — deterministic templates write every committed line, and every proposal passes the
collision/hallucination/reserved-name guard.

---

## 6. Honest classification — gaps NOT faked

**Classification: high `ERP_PILOT_READY`, meeting most `ERP_LOCAL_PRODUCTION_READY` criteria** (runs and
deploys locally, MSSQL/SQL-Express compatible, **real authentication/authorization**, tested accounting and
stock correctness, maker/checker, auditability, UI CRUD create pages, REST endpoints, reports, backup/restore
+ health scripts, deployment docs, xUnit + Playwright suites).

**Remaining gaps to true `ERP_LOCAL_PRODUCTION_READY` / ERPNext-grade — documented, not hidden:**

1. **Schema is `EnsureCreated`/`Migrate`-ready but no committed EF migration history** for SQL Server. The
   deployment script wires `Database.Migrate()` for SQL Server, but migrations are not generated in this sprint.
2. **CRUD is create + list + read; edit/delete UI is not generated** for the catalog modules.
3. **Module depth is reference-grade, not ERPNext breadth** — ~28 modules vs the 400-test/30-module stretch
   targets, which were stated up front as not achievable in a single session.
4. **Auth is app-level**: no MFA, SSO, account lockout, or password-policy enforcement yet.
5. **5 of 28 modules are non-deterministic** (local-LLM-proposed), so exact regeneration excludes them.

No parity, production-certification, or 100%-completion claim is made. The 400-test / 30-module / full
production targets of the sprint brief are **not** met; this is the honest reference-grade subset, with every
manual upgrade folded back into the generator so LocalAIFactory can reproduce and keep improving it.
