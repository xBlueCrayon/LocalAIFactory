# ERP/CRM Industrial — Capability Scenario

> **Original synthetic scenario.** Everything here is inspired-by, not cloned. This is **not** a
> Microsoft Dynamics, SAP, Sage, Odoo, or any other vendor product, and it makes **no claim** of
> compatibility, equivalence, interoperability, or certification with any commercial ERP/CRM.

## What this scenario tests

It tests whether LocalAIFactory can reason about an industrial ERP/CRM estate the way a senior
solution consultant would: given a schema change or a "what touches X?" question, it must answer
with **evidence from the actual code graph**, and it must be honest about where an answer is
*derived* from the graph versus *advised* from design and governance docs.

The hard proof is the deterministic **C#↔SQL bridge**, exercised by the committed benchmark fixture
at `benchmarks/fixtures/erp-crm-industrial/` (`schema.sql` + `ErpCrmServices.cs`). The fixture scores
**Gold, 6/6 proofs** in the standard benchmark suite.

## Entity model

Master and transaction tables in `dbo` (see `schema.sql`):

| Group | Tables |
|---|---|
| CRM master | `Customer`, `Contact`, `Lead`, `Opportunity` |
| Pricing | `PriceList` |
| Order-to-cash | `SalesOrder`, `SalesOrderLine`, `Invoice` |
| Procure | `PurchaseOrder` |
| Inventory | `InventoryItem`, `InventoryMovement` |
| Governance | `DiscountApproval` |
| Routine | proc `dbo.usp_PostInvoice` (posts an invoice) |

Key foreign keys: `Contact→Customer`, `Opportunity→Customer`, `SalesOrder→Customer`,
`SalesOrderLine→SalesOrder`, `Invoice→SalesOrder`, `InventoryMovement→InventoryItem`. `Customer` is
the hub: changes propagate transitively to `SalesOrder` and onward.

C# service layer (`namespace ErpCrm.Services`, see `ErpCrmServices.cs`): `CustomerService`,
`OpportunityService`, `SalesOrderService`, `InvoiceService`, `InventoryService`, `ApprovalService`,
`ReportingService`. Each method names its SQL objects in its query text so the bridge links
service method → table deterministically (no LLM guessing).

## How the C#↔SQL bridge answers ERP/CRM impact questions

The bridge builds a graph of `table ↔ service-method` edges from the raw SQL each method executes,
then answers two query modes:

- **`dependents("dbo.X")`** — which C# code reads/writes table `X` directly.
- **`impact("dbo.X")`** — `dependents(X)` plus everything reachable through FK relationships
  (transitive blast radius).

The six benchmark proofs:

| Mode / target | Result |
|---|---|
| `dependents("dbo.SalesOrder")` | `SalesOrderService.GetSalesOrders`, `SalesOrderService.CreateSalesOrder` |
| `impact("dbo.Customer")` | `CustomerService.GetCustomer` + `SalesOrderService.GetSalesOrders` (transitive via FK) |
| `dependents("dbo.Opportunity")` | `OpportunityService.AdvanceStage` |
| `dependents("dbo.Invoice")` | `InvoiceService.GenerateInvoice` |
| `dependents("dbo.DiscountApproval")` | `ApprovalService.ApproveDiscount` |
| `dependents("dbo.InventoryMovement")` | `InventoryService.RecordMovement` + `ReportingService.InventoryReport` |

These are mechanical, reproducible, and survive refactors — they are not opinions.

## How to run validation

```powershell
# From this folder (or pass -RepoRoot explicitly):
./validation-script.ps1
```

It runs the benchmark harness in-memory and asserts the fixture reaches Gold 6/6:

```powershell
# What the script does under the hood:
cd tools/LocalAIFactory.Benchmark
dotnet run -c Release -- --inmemory --suite standard
# look for:  ErpCrmIndustrial   Gold   ... pov=6/6
```

Exit code `0` and a green "ERP/CRM: Gold 6/6 — capability proven" line means the capability holds.

## Graph-derived vs advisory (the honest line)

- **Graph-derived (provable):** "what code touches table X", direct dependents, and FK-transitive
  impact. Backed by the bridge and the 6/6 benchmark. See `expected-questions.md` items marked
  **GRAPH-DERIVED**.
- **Advisory (design/governance, not the code graph):**
  - **Roles / who may approve a discount.** The graph proves `ApprovalService.ApproveDiscount`
    writes `dbo.DiscountApproval` with an `ApproverRole` column. *Which* roles are permitted is an
    RBAC/governance matrix in design docs, not something the code graph decides.
  - **Migration path** for a new CRM field is an additive-migration playbook (design guidance), not
    a graph output.
  These appear in `expected-questions.md` marked **ADVISORY** and are answered honestly in
  `expected-answers.md`.

## Files in this scenario

- `README.md` — this file.
- `integration-contracts.md` — original integration/API/migration/extensibility/audit patterns.
- `expected-questions.md` — the 7 proof questions, each tagged GRAPH-DERIVED or ADVISORY.
- `expected-answers.md` — strong consultant-grade answers, honest about limits.
- `validation-script.ps1` — runs the benchmark fixture as the live proof.
