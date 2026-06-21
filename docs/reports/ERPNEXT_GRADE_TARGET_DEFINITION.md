# ERPNext-Grade Target Definition (Summary)

A condensed summary of `docs/ERPNext-Grade-Free-ERP-Target.md` plus the gate categories
used to measure LAF-Generated ERP against ERPNext community (free) edition.

**Honest status:** LAF ERP V5 = **ERP_PILOT_READY**. `reachedErpNextFreeGrade=false`.
This is a target definition, not a parity claim.

## The target in one paragraph

An ERPNext-grade free ERP delivers double-entry accounting (GL, P&L, Balance Sheet, trial
balance, AR/AP, credit/debit notes), the selling/buying/stock document chains with masters
and price lists, manufacturing (BOM/work order/job card and MRP), CRM, projects, support,
assets, HR with payroll, POS/e-commerce, and a workflow/approval engine — all on a platform
with schema migrations, backup/restore, performance headroom, role security, a REST API,
and a deployable artifact. LAF V5 covers a real subset of this and runs locally; it is a
pilot, not full grade.

## Gate categories

### Local gates (achievable without internet / external infra)

1. **Schema lifecycle** — EF migrations (V5 currently uses `EnsureCreated`).
2. **CRUD UI depth** — edit/delete + list-detail views (V5 added **CREATE** only).
3. **Operational resilience** — backup/restore drill.
4. **Performance** — load test under representative volume.
5. **Module depth** — MRP, payroll, POS, storefront, returns.

### External gates (require infrastructure, third parties, or customer)

1. **Authentication** — Windows / SSO / OIDC.
2. **Transport security** — CA-signed TLS.
3. **Security assurance** — external security review.
4. **Acceptance** — customer acceptance / sign-off.

## Readiness ladder

`ERP_NOT_READY` -> `ERP_DEMO_READY` -> **`ERP_PILOT_READY` (V5 today)** ->
`ERP_LOCAL_PRODUCTION_READY` (clears all local gates) ->
`ERP_PRODUCTION_READY_WITH_EXTERNAL_PROOFS` -> `ERP_PRODUCTION_READY_FULL`.
