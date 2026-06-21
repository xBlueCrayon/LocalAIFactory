# Deployment Database Proof (Phase 2)

**Date:** 2026-06-21 · **Mode:** C (published app + SQL Express)

The deployment used a **separate, fresh database on SQL Server Express** — the existing LocalDB
`LocalAIFactory` database was **not** touched.

| Item | Value |
|---|---|
| SQL target | **SQL Server Express 2022 (16.0.1)** — instance `.\SQLEXPRESS` (service `MSSQL$SQLEXPRESS`) |
| Database | **`LocalAIFactory_DeploymentProof`** (created fresh; absent before this run) |
| Connection string (redacted) | `Server=.\SQLEXPRESS;Database=LocalAIFactory_DeploymentProof;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True` — Windows (trusted) auth, **no credentials** |
| How created | The **published app** created + migrated + seeded the database on first startup (auto-migrate + auto-install-all-packs), pointed at SQL Express via the `ConnectionStrings__DefaultConnection` environment override |
| Migrations applied | **14** (latest `20260620210822_AddBridgeEvidence`) — `SELECT COUNT(*) FROM __EFMigrationsHistory` |
| Installed packs | **4** (`SELECT COUNT(*) FROM KnowledgePacks`) |
| Pack items | **438** baseline, **438** distinct Uids (`KnowledgeItems WHERE KnowledgePackId IS NOT NULL`) |
| Per-pack | Professional Base 390 (v1.2.0), Financial Institution Ops 16, KYC/AML 16, Market Intelligence 16 |
| Verification command | `scripts/deployment-drill/09-post-deploy-healthcheck.ps1 -Server .\SQLEXPRESS -Database LocalAIFactory_DeploymentProof` → **PASS** |

## Live query output (SQL Express)

```
Name                                    Version  ItemCount
Financial Institution Operations v1     1.0.0    16
KYC AML Transaction Approval v1         1.0.0    16
Market Intelligence and Forecasting v1  1.0.0    16
Professional Base Knowledge Pack        1.2.0    390
TotalPackItems = 438   DistinctUids = 438
Migrations = 14
```

## Safety / limitations

- **Non-destructive to existing data.** The main LocalDB `LocalAIFactory` database was never modified;
  this proof used an isolated SQL Express database with a distinct name.
- Backup/restore on the deployment DB was **not** exercised in this pass (the LocalDB
  backup/restore-verify proof is recorded in earlier phases). The disposable deployment DB can be
  dropped at any time with `DROP DATABASE LocalAIFactory_DeploymentProof` (operator-approved, isolated).
- This is a **SQL Express pilot proof**, not a full-SQL-Server or production database proof.
