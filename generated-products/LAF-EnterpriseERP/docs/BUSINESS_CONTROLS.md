# LAF Enterprise ERP — Business Controls

Each control below is enforced in code and proven by a named test.

| Control | Where enforced | Proven by test |
|---|---|---|
| Maker cannot approve own document | `WorkflowService.Approve` (MakerCannotApprove) | `WorkflowTests.Maker_cannot_approve_own_document` |
| Amount over threshold requires a separate approver | `WorkflowService.Submit` (auto-approve only ≤ threshold) | `WorkflowTests.Amount_over_threshold_requires_separate_approver` |
| Submit/approve restricted to authorized roles | `WorkflowService` (SubmitRole/ApproverRole) + `RbacService` | `WorkflowTests.Submitter_without_submit_role_is_blocked`, `Approver_without_role_is_blocked` |
| Rejection requires a reason | `WorkflowService.Reject` | `WorkflowTests.Reject_requires_a_reason` |
| Posted documents are immutable | `SalesService.EditRate` (Draft-only) | `ImmutabilityTests.Submitted_invoice_cannot_be_edited` |
| Corrections via reversal, not silent edit | `AccountingService.ReverseVoucher` + `Cancel` | `ImmutabilityTests.Cancel_reverses_gl_to_zero_net`, `StockTests.Cancelling_a_submitted_invoice_restores_stock` |
| Double-entry must balance | `AccountingService.PostJournalEntry` | `AccountingTests.JournalEntry_must_balance_or_is_rejected` |
| Stock cannot go negative | `StockService.MoveOut` | `StockTests.Outward_movement_cannot_drive_stock_negative` |
| Every state change is audited | `AuditService` called by each service | `WorkflowTests.Every_transition_records_an_audit_event`, `AuditAndImportTests.Create_records_audit_event` |
| Cannot submit twice | `WorkflowService.Submit` (Draft-only) | `ImmutabilityTests.Cannot_submit_twice` |

These are the load-bearing financial/inventory controls. They are real, not decorative: removing any one
of them makes a test fail.
