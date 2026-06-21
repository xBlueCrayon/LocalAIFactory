# ERP-GOLD-DEPTH — SQL Server (LocalDB / SQL Express) Live Proof

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-sql-express-proof.json`

This is a **real, live** SQL Server migration + end-to-end app proof — not a design assertion.

## Environment

- `(localdb)\MSSQLLocalDB` available (used for this proof).
- `.\SQLEXPRESS` also available.

## What was applied

The committed EF migrations were applied **live** to the LocalDB database `LafErpGoldDepth`:

- `InitialCreate` — 65 tables.
- `AddManufacturing` — 3 tables (BOM + production-order schema).

Migration `20260621151632_InitialCreate` recorded in `__EFMigrationsHistory`.

## Live schema verification

| Check | Result |
|-------|--------|
| Base tables | 66 |
| `__EFMigrationsHistory` rows | 1 |
| Core tables present (Accounts, GLEntries, SalesInvoices, AppUsers, AuditEvents) | 5 / 5 |

## Live app smoke (against LocalDB, SqlServer provider via `Database.Migrate()`)

| Step | Result |
|------|--------|
| Health endpoint | 200 |
| Real login | 302 + auth cookie |
| Authenticated dashboard | 200 |

**Result: PASS** — real live SQL Server (LocalDB) migration + end-to-end app login proven.

## Honest limitations / not done

- The proof was applied against the **deterministic** product
  (`generated-products/LAF-EnterpriseERP-GoldGenerated-Hardened`), whose model matches the
  committed migration snapshot.
- The **Gold** product additionally contains **5 non-deterministic local-LLM catalog modules** that
  are **NOT** in the committed migration; EF reports `PendingModelChangesWarning` for Gold on SQL
  Server. On SQL Server those 5 tables would need a follow-up migration. On SQLite, Gold uses
  `EnsureCreated` for the full model.
- This is the documented, honest boundary of the committed `InitialCreate` — the live proof covers
  the deterministic surface, not the LLM-extra modules.
