# LAF Enterprise ERP V3 — Production-Grade Review

**Date:** 2026-06-21
**Score file:** `benchmarks/results/laf-erp-v3-production-grade-score.json`
**Gate file:** `benchmarks/erpnext-study/production-grade-erp-gates.json`
**Verdict:** **PILOT-grade, not production-grade. Overall ~48%.**

## Overall

**Overall production-grade: 48%** (up from V1 35% / V2 35%).

The score is **capped by 3 external Critical gates** (real auth, TLS, external pen-test) plus MSSQL production-scale load — all operator/external-owned and not producible locally. **100% is impossible locally for exactly these reasons.**

## Category scores

| Category | Score |
|----------|------:|
| Functional completeness | 46 |
| Accounting correctness | 80 |
| Inventory correctness | 60 |
| Workflow control correctness | 70 |
| RBAC / security | 45 |
| Auditability | 70 |
| API completeness | 62 |
| UI completeness | 35 |
| Test coverage | 78 |
| Playwright coverage | 55 |
| Real-life scenario coverage | 42 |
| Performance | 35 |
| Deployment readiness | 35 |
| Maintainability | 75 |
| Documentation | 72 |
| Generator autonomy | 100 |

## Gate summary (25 gates)

| Outcome | Count |
|---------|------:|
| PASS | 14 |
| PARTIAL | 7 |
| FAIL | 1 |
| BLOCKED (external) | 4 |

**Critical external-blocked gates:** C-13 real auth, C-14 TLS, C-15 external pen-test (plus H-17 MSSQL production-scale load).

## What is solid (PASS)

- **Build green** (C-01) and **108 tests green** (C-02).
- **Double-entry GL balances; unbalanced rejected** (C-03; trial balance 2250 = 2250).
- **Maker/checker enforced** (C-04) and **audit trail on every transition** (C-05).
- **RBAC enforced** (C-06) and **no HTTP 500 on any page** (C-07).
- **Inventory: stock ledger + valuation + negative guard** (H-01).
- **Profit & Loss** (H-02) and **Balance Sheet, balanced** (H-03).
- **REST API coverage of core + 15 catalog modules** (H-04).
- **Playwright browser + login** (H-05) and **real-life business scenario test** (H-06).

## What is partial or failing

- **H-07 UI create/edit CRUD pages — PARTIAL:** list/read pages + catalog POST API only; no general create/edit forms.
- **M-01 Manufacturing (BOM/WorkOrder/Quality) — PARTIAL:** CRUD skeletons, not full MRP.
- **M-02 HR/payroll — PARTIAL** (Employee / SalaryComponent CRUD only).
- **M-03 POS — PARTIAL** (PosProfile CRUD only).
- **M-08 deployment + backup/restore docs — PARTIAL.**
- **M-10 EF migrations — FAIL:** `EnsureCreated` used instead of migrations.
- **L-01 Notifications — PARTIAL** (NotificationRule CRUD); **L-03 Website/eCommerce — PARTIAL** (WebProduct CRUD).

## External-capped gaps (cannot be closed locally)

- **C-13 Real authentication** (Windows/SSO/OIDC) — operator-supplied.
- **C-14 TLS / HTTPS with CA cert** — operator-supplied.
- **C-15 External security review / pen-test** — external firm.
- **H-17 MSSQL production-scale load test** — operator host.

## Verdict

V3 adds real accounting depth (P&L + Balance Sheet, balanced), 15 data-driven CRUD modules (manufacturing/HR/POS/eCommerce/maintenance/customization **skeletons**), and 108 tests, lifting production readiness from ~35% to **~48%**. The ceiling is hard: real auth, TLS, an external security review, and MSSQL production load are operator/external-owned and cap the score until supplied. **PILOT-grade.**
