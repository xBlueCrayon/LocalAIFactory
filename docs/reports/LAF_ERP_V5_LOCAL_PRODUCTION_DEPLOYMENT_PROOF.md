# LAF ERP V5 Local Production Deployment Proof

## Publish

- **Script:** `scripts/erp-v5/publish-local-production.ps1`
- **Build:** Release
- **Target:** `C:\LAFEnterpriseERP-V5`
- **Artifacts produced:** `LafErp.Web.dll`, `LafErp.Web.exe`,
  `README-LOCAL-PRODUCTION.txt`.

## Run proof

- The published app **ran on SQLite**.
- **Health: ok.**
- **Trial balance: 7 seeded rows** returned.

## Database support

- **SQLite** out of the box.
- **MSSQL / SQL Express** supported via `ConnectionStrings:Default` (documented in the
  published `README-LOCAL-PRODUCTION.txt`).

## Constraints (documented, honest)

- EXEs are **git-ignored** (local-only artifacts; not committed).
- Schema is created via `EnsureCreated`, **not EF migrations** — a remaining local gate.
- No backup/restore drill and no load test performed against this deployment.

## External blockers (not addressed by local publish)

- Real authentication (Windows / SSO / OIDC).
- CA-signed TLS.
- External security review.
- Customer acceptance.

## Honest verdict

This proves V5 **publishes and runs as a real local deployment** on SQLite, with MSSQL
documented. It does **not** make V5 production-grade — it remains **ERP_PILOT_READY**, with
the local and external gates above still open.
