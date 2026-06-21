# Workflow Model

LAF Enterprise ERP governs financial and stock documents with a generic **submit / approve / reject**
engine (`WorkflowService`) layered over a three-state document lifecycle. The engine is doctype-driven
by a `WorkflowDefinition`, so the same code drives sales invoices, purchase invoices, payments, and
journal entries.

## Two state concepts

There are **two distinct state values**, which is important to understand:

1. **`DocStatus`** (on the document itself, from `DocumentBase`):

   ```
   Draft = 0   →   Submitted = 1   →   Cancelled = 2
   ```

   This is the clean-room equivalent of the common ERP `docstatus` 0/1/2 lifecycle. A `Submitted`
   document has posted to the ledgers and is immutable.

2. **`WorkflowInstance.CurrentState`** (a string on the workflow instance):

   ```
   Draft → PendingApproval → Approved
                          ↘ Rejected (→ document back to Draft)
   ```

   This tracks the approval routing. When the instance reaches `Approved`, the document's `DocStatus`
   becomes `Submitted` and posting runs.

## The lifecycle

```
        ┌─────────┐   Submit (maker, holds SubmitRole)
        │  Draft  │ ───────────────────────────────────────┐
        └─────────┘                                         │
             ▲                                              ▼
             │                              amount ≤ threshold?
             │                               ┌──────────────┴──────────────┐
             │ Reject (reason required)      │ yes: auto-approve            │ no
             │                               ▼                              ▼
        ┌────┴───────────┐            ┌────────────┐              ┌──────────────────┐
        │ (back to Draft)│◀───────────│  Approved  │              │ PendingApproval  │
        └────────────────┘            │ DocStatus= │              │ (awaits checker) │
                                      │ Submitted   │              └────────┬─────────┘
                                      │ + posts GL/ │      Approve (checker, │ holds ApproverRole,
                                      │   stock     │       maker ≠ checker) │ not the submitter)
                                      └─────────────┘◀──────────────────────┘
                                                          Reject → back to Draft
```

### Submit

`WorkflowService.Submit(doc, docType, amount, onPost)`:

1. Rejects the call unless the document is in `Draft`.
2. Requires the acting user to hold the definition's `SubmitRole`.
3. Creates a `WorkflowInstance` (`SubmittedBy = current user`, `Amount = amount`).
4. **Threshold decision:** if `amount ≤ ApprovalThreshold` (or there is no definition), the document
   **auto-approves on submit** — `CurrentState = Approved`, `DocStatus = Submitted`, `onPost()` runs,
   and an `Approve` audit event ("auto-approved within threshold") is recorded. Otherwise the
   instance goes to `PendingApproval` and the document stays in `Draft` until a separate approver acts.
5. Records a `Submit` audit event in all cases.

### Approve

`WorkflowService.Approve(doc, docType, onPost)`:

1. Requires the instance to be in `PendingApproval`.
2. Requires the acting user to hold `ApproverRole`.
3. **Maker/checker:** if `MakerCannotApprove` (default true), the submitter may not approve their own
   document — throws `DomainException("Maker/checker violation …")`.
4. Sets `CurrentState = Approved`, `DocStatus = Submitted`, runs `onPost()`, records an `Approve`
   audit event.

### Reject

`WorkflowService.Reject(doc, docType, reason)`:

1. **A non-blank reason is mandatory** (throws otherwise).
2. Requires `PendingApproval` state and the `ApproverRole`.
3. Sets `CurrentState = Rejected`, returns the document to `Draft`, records the reason on the
   `WorkflowApproval` row and a `Reject` audit event. The document can be edited and re-submitted.

## Amount threshold (seeded values)

The threshold is the amount **at or below which a document auto-approves on submit**; above it, a
separate approver is required.

| DocType | SubmitRole | ApproverRole | Threshold |
| --- | --- | --- | --- |
| SalesInvoice | Sales User | Accounts Manager | 1000 |
| PurchaseInvoice | Purchase User | Accounts Manager | 1000 |
| PaymentEntry | Accounts User | Accounts Manager | 500 |
| JournalEntry | Accounts User | Accounts Manager | 0 |

A `JournalEntry` threshold of `0` means **every** journal entry requires a separate approver (nothing
auto-approves). The amount passed in is the document's driving value: `GrandTotal` for invoices,
`Amount` for payments, `TotalDebit` for journal entries.

## Posting happens only on Submitted

GL and stock posting are passed to the engine as an `onPost` delegate and execute **only** at the
transition to `Submitted` (auto-approve or explicit approve). Concretely:

- `SalesService.Submit` → `WorkflowService.Submit(si, "SalesInvoice", si.GrandTotal, () => acc.PostSalesInvoice(si))`.
- `PaymentService.Submit` posts the payment GL **and** applies the invoice allocation only on submit/approve.

A document sitting in `PendingApproval` has **no** ledger impact yet.

## Immutability and reversal (not edit)

- A **`Submitted` document is immutable.** `SalesService.EditRate` throws
  `DomainException("A submitted invoice is immutable; cancel and reissue instead.")`; only `Draft`
  documents can be edited.
- **Corrections go through cancel, never silent edits.** `Cancel` (allowed only on a `Submitted`
  document) calls `AccountingService.ReverseVoucher`, which posts **reversing GL entries**
  (debit/credit swapped, `IsReversal = true`) and reverses the stock movements
  (`StockService.ReverseVoucher`, voucher suffix `-REV`). The document's `DocStatus` becomes
  `Cancelled`. History is preserved: the original entries remain, and the reversal nets them to zero.

## Audit on every transition

`WorkflowService` records an `AuditEvent` for **every** transition — `Submit`, `Approve` (including
the auto-approve case), and `Reject` (with the reason) — via `AuditService`. The document orchestrators
also record a `Create` event and a `Cancel` event. The result is a complete, append-only trail of who
did what and when.
