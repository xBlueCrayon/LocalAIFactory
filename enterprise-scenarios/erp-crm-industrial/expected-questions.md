# Expected Questions — ERP/CRM Industrial

The seven required proof questions. Each is tagged **GRAPH-DERIVED** (mechanically provable by the
C#↔SQL bridge and the Gold 6/6 benchmark fixture) or **ADVISORY** (answered from design/governance
docs, not the code graph). The exact bridge **target** and **mode** are given for graph-derived
questions. Strong answers are in `expected-answers.md`.

---

### Q1. What C# services touch `SalesOrder`?

- **Type:** GRAPH-DERIVED
- **Bridge mode:** `dependents`
- **Target:** `dbo.SalesOrder`
- **Why provable:** the bridge links `SalesOrderService.GetSalesOrders` and
  `SalesOrderService.CreateSalesOrder` directly from their SQL text. Benchmark proof #1.

### Q2. What tables are impacted if `Customer` changes?

- **Type:** GRAPH-DERIVED
- **Bridge mode:** `impact` (transitive, follows FKs)
- **Target:** `dbo.Customer`
- **Why provable:** `Customer` is the FK hub; `impact` returns `CustomerService.GetCustomer` plus
  `SalesOrderService.GetSalesOrders` reached transitively through `SalesOrder→Customer`. Benchmark
  proof #2. (FK fan-out also reaches `Contact`, `Opportunity`, `SalesOrder`.)

### Q3. What workflow depends on `Opportunity`?

- **Type:** GRAPH-DERIVED
- **Bridge mode:** `dependents`
- **Target:** `dbo.Opportunity`
- **Why provable:** the bridge links `OpportunityService.AdvanceStage` (the pipeline-stage workflow)
  to `dbo.Opportunity`. Benchmark proof #3.

### Q4. What API should change if the `Invoice` schema changes?

- **Type:** GRAPH-DERIVED (for the code touchpoint) + ADVISORY (for the API contract surface)
- **Bridge mode:** `dependents`
- **Target:** `dbo.Invoice`
- **Why mixed:** the bridge proves `InvoiceService.GenerateInvoice` is the only code touching
  `dbo.Invoice` (benchmark proof #4), so the impacted endpoint is `POST /api/v1/invoices`. *Which
  fields* of the API DTO change is read off the integration contract (`integration-contracts.md`),
  which is design, not graph.

### Q5. What roles can approve a discount?

- **Type:** ADVISORY (governance / RBAC)
- **Bridge mode (partial proof):** `dependents` on `dbo.DiscountApproval`
- **Target:** `dbo.DiscountApproval`
- **Why advisory:** the bridge proves `ApprovalService.ApproveDiscount` writes `dbo.DiscountApproval`
  including an `ApproverRole` column (benchmark proof #5). But the *set of permitted roles and
  thresholds* is an RBAC governance matrix in design docs — the code graph does not encode policy.

### Q6. What is the migration path when adding a CRM field?

- **Type:** ADVISORY (additive-migration playbook)
- **Bridge mode:** n/a (impact of an existing field can be checked via `impact`, but the migration
  *procedure* is design guidance)
- **Target:** the table receiving the field (e.g. `dbo.Customer` or `dbo.Opportunity`)
- **Why advisory:** the bridge can show current dependents of the table, but adding a column is a
  forward-looking change governed by the additive-migration rules, not something derivable from the
  graph.

### Q7. What reports are impacted if `InventoryMovement` changes?

- **Type:** GRAPH-DERIVED
- **Bridge mode:** `dependents`
- **Target:** `dbo.InventoryMovement`
- **Why provable:** the bridge links both `InventoryService.RecordMovement` and
  `ReportingService.InventoryReport` to `dbo.InventoryMovement`; the report is the
  `ReportingService.InventoryReport` aggregate. Benchmark proof #6.

---

**Summary:** 4 fully graph-derived (Q1, Q2, Q3, Q7), 1 mixed (Q4), 2 advisory (Q5, Q6). All six
graph claims are backed by the standard-suite Gold 6/6 result for `ErpCrmIndustrial`.
