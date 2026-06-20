# Expected Answers — ERP/CRM Industrial

Strong, consultant-grade answers to the seven proof questions. Graph-derived answers quote the exact
bridge result (reproducible via `validation-script.ps1`, Gold 6/6). Advisory answers are clearly
flagged as design/governance, with honest limits.

---

### A1 — What C# services touch `SalesOrder`? (GRAPH-DERIVED)

`dependents("dbo.SalesOrder")` →
- `SalesOrderService.GetSalesOrders` (reads open orders)
- `SalesOrderService.CreateSalesOrder` (inserts the order **and** its `SalesOrderLine`)

These two methods are the complete code surface over `dbo.SalesOrder`. Note `CreateSalesOrder` also
touches `dbo.SalesOrderLine`, so order and line changes co-locate in one service — a clean seam for a
`POST /api/v1/sales-orders` endpoint.

### A2 — What tables are impacted if `Customer` changes? (GRAPH-DERIVED)

`impact("dbo.Customer")` (transitive over FKs) →
- direct: `CustomerService.GetCustomer`
- transitive: `SalesOrderService.GetSalesOrders` (reached via `SalesOrder.CustomerId → Customer.Id`)

`Customer` is the hub. By FK, a change radiates to `Contact`, `Opportunity`, and `SalesOrder`
(and onward to `SalesOrderLine`/`Invoice`). Practical guidance: a breaking `Customer` change forces
regression of customer reads, contact listing, the pipeline, and the whole order-to-cash chain.
Treat any `Customer` column rename/removal as a **major** change.

### A3 — What workflow depends on `Opportunity`? (GRAPH-DERIVED)

`dependents("dbo.Opportunity")` → `OpportunityService.AdvanceStage` (the pipeline-stage transition,
e.g. → `Won`). This is the workflow that mutates `Opportunity`. The `OnOpportunityStageChange`
automation hook (`integration-contracts.md` §4) is what downstream automations subscribe to; the
graph proves `AdvanceStage` is its source.

### A4 — What API should change if the `Invoice` schema changes? (GRAPH-DERIVED + ADVISORY)

`dependents("dbo.Invoice")` → `InvoiceService.GenerateInvoice` (inserts the invoice, then
`EXEC dbo.usp_PostInvoice`). It is the **only** code touching `dbo.Invoice`.

Therefore the single impacted endpoint is **`POST /api/v1/invoices`**. **Advisory part:** the precise
DTO fields to change come from the integration contract, and `usp_PostInvoice` must be reviewed in
lockstep since it writes `Invoice.Posted`. Limit: the graph names the endpoint and the proc; it does
not author the new contract — that is a design decision.

### A5 — What roles can approve a discount? (ADVISORY — governance/RBAC)

**What the graph proves:** `dependents("dbo.DiscountApproval")` → `ApprovalService.ApproveDiscount`,
which writes `dbo.DiscountApproval` with columns `OpportunityId`, `ApproverRole`, `Approved`. The
fixture happens to write `ApproverRole = 'SalesManager'`.

**What the graph does NOT decide:** the *set of permitted approver roles and the discount thresholds
each may sign off* is an RBAC **governance matrix**, not code. A typical (illustrative, not
prescriptive) matrix:

| Discount band | Permitted approver role |
|---|---|
| ≤ 5% | `SalesRep` (self-serve) |
| > 5% – 15% | `SalesManager` |
| > 15% – 30% | `RegionalDirector` |
| > 30% | `Finance` + `RegionalDirector` (dual) |

**Honest limit:** that matrix lives in the RBAC/governance doc. The code graph can prove *who wrote
an approval* (via audit + `ApproverRole`) and *that approvals flow through `ApprovalService`*, but it
cannot tell you a role is *authorised* — enforce that in policy/RBAC, then audit it.

### A6 — Migration path when adding a CRM field? (ADVISORY — additive playbook)

Follow the **additive, backward-compatible** rule (no destructive change without approval):

1. **Add a nullable column** (or one with a default) to the target table — e.g.
   `ALTER TABLE dbo.Customer ADD Industry NVARCHAR(80) NULL;`. Nullable means existing rows and code
   keep working — no break.
2. **EF migration:** add the property to the entity, then
   `dotnet ef migrations add AddCustomerIndustry` and `database update`. Regenerate the model
   snapshot through EF; never hand-edit it.
3. **Backfill** existing rows out-of-band (staged update or migration data step); leave the column
   nullable until backfill completes.
4. **Extend the contract additively:** add the field to the API DTO and the `stg.*` staging mirror.
   Additive API fields are non-breaking under `/v1`.
5. **Optionally tighten later** (NOT NULL) only after backfill is verified — and only with explicit
   approval, since that is no longer purely additive.

**Pre-flight check (graph-assisted):** run `impact("dbo.<table>")` first to size the blast radius —
which services and endpoints will see the new field. **Limit:** the graph sizes impact; it does not
write the migration.

### A7 — What reports are impacted if `InventoryMovement` changes? (GRAPH-DERIVED)

`dependents("dbo.InventoryMovement")` →
- `InventoryService.RecordMovement` (the writer)
- `ReportingService.InventoryReport` (the report: `SUM(Delta)` net per `ItemId`)

The impacted report is **`ReportingService.InventoryReport`** (`GET /api/v1/reports/inventory`). Any
change to `Delta`, `ItemId`, or `Reason` directly affects that aggregate, and the writer
`RecordMovement` must change in step to keep the report correct.

---

**Bottom line:** Q1–Q4 and Q7 are anchored by the bridge's mechanical proofs (Gold 6/6); Q5 and Q6
are advisory by nature and are answered with design/governance content, with the graph used only for
the part it can honestly prove (the code touchpoint and the impact size).
