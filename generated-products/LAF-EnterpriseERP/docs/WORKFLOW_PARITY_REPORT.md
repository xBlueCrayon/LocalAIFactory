# LAF Enterprise ERP — Workflow Parity Report

The brief asked for 10 approval workflows. Status against what is implemented as **real maker/checker**:

| # | Requested workflow | Status | Note |
|---|---|---|---|
| 1 | Sales order / invoice approval | **Implemented + tested** | `SalesInvoice` workflow (threshold 1000, Accounts Manager approver) |
| 2 | Purchase order / invoice approval | **Implemented + tested** | `PurchaseInvoice` workflow (threshold 1000) |
| 3 | Payment approval | **Implemented + tested** | `PaymentEntry` workflow (threshold 500) |
| 4 | Journal entry approval | **Implemented + tested** | `JournalEntry` workflow (threshold 0 → always needs a checker) |
| 5 | Stock adjustment approval | Partial | Stock moves via invoices use the same engine; no standalone Stock Entry doc yet |
| 6 | Supplier onboarding approval | Not implemented | Supplier is created directly; engine is reusable for it |
| 7 | Customer onboarding approval | Not implemented | As above |
| 8 | Support escalation | Implemented (status flow) | `SupportService.Escalate/Resolve` (not the maker/checker engine) |
| 9 | Asset maintenance approval | Partial | `AssetService.ScheduleMaintenance`; no approval step yet |
| 10 | Project task approval | Implemented | `ProjectService.ApproveTask` gating completion |

## Engine capability

The `WorkflowService` engine is **generic** over any `DocumentBase` + a posting delegate, so adding a new
approval workflow is configuration (a `WorkflowDefinition` row) + wiring, not new control logic. Four
document types use it as real maker/checker today; the rest are honestly marked partial/not-implemented.
