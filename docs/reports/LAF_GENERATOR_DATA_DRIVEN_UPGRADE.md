# LAF Generator — Data-Driven Upgrade (V2 → V3)

**Date:** 2026-06-21
**Component:** `tools/LocalAIFactory.Generator`

## Summary

In V2 the generator produced CRUD modules by **template-copy** (a fixed, hand-curated set of 3 catalog modules). In V3 the generator became **data-driven**: it reads a **module-spec JSON** and emits one CRUD module per spec entry, plus new accounting depth in the engine templates. This is the structural win of V3; the parity gain is modest (+5%), but the *way* the product is produced changed materially.

## What changed

### 1. Module-spec JSON + schema

- New spec file: `tools/LocalAIFactory.Generator/specs/erpnext-grade-modules.json`.
- New schema: `tools/LocalAIFactory.Generator/specs/erp-module-spec.schema.json`.
- The spec declares modules and their fields; the generator reads it instead of carrying a hard-coded module list.

### 2. `LoadModuleSpec` validation + collision guard

- The generator validates each spec/proposed entity before emitting.
- A **collision guard** rejects any proposed entity whose name overlaps a core engine entity. In V3 this rejected the LLM-proposed `Supplier` entity (collision with a core engine entity / hallucination-overlap guard).

### 3. Per-entity CRUD emission

For each accepted entity the generator emits:

- an entity record + `DbSet` on `ErpDbContext`,
- a generic `CatalogCrudService<T>` registration,
- REST `GET`/`POST` endpoints,
- a `/Catalog` list page (Razor),
- 2 tests.

Result: **15 CRUD modules** in V3 (10 spec-driven + 5 from the governed LLM proposal) vs **3** in V2.

### 4. Accounting depth in the engine templates

V3 added two real financial reports to the templated engine:

- **Profit & Loss** — `AccountingService.ProfitAndLoss`, `/api/reports/profit-and-loss`.
- **Balance Sheet** — `AccountingService.BalanceSheet`, `/api/reports/balance-sheet`.

Verified live:

- P&L: income **500** / expense **300** / **netProfit 200**.
- Balance Sheet: assets **1450** = liabilities **1250** + retained **200**, **balanced = true**.

## V2 template-copy vs V3 data-driven

| Aspect | V2 | V3 |
|--------|----|----|
| CRUD source | hard-coded template copy | module-spec JSON (data-driven) |
| CRUD modules | 3 | 15 (10 spec + 5 LLM) |
| Validation / collision guard | n/a | `LoadModuleSpec` validation + collision guard (rejected `Supplier`) |
| Accounting reports | GL, TB, AR, AP | GL, TB, AR, AP, **P&L, Balance Sheet** |
| Autonomy | 100% | 100% |
| Manual product-source edits | 0% | 0% |

## Honest note

The generator is now genuinely data-driven, but the emitted CRUD modules are still **list/read skeletons** (no general create/edit forms — gate H-07 is PARTIAL). The data-driven mechanism is real; the modules it produces are not yet full ERP modules.
