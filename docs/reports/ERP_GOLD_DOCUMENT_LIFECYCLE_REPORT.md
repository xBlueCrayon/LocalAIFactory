# ERP-GOLD-DEPTH — Document Lifecycle Report

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-document-lifecycle-coverage.json`

## Existing maker/checker lifecycle (financial documents)

`src/LafErp.Services/WorkflowService.cs` is a generic submit/approve/reject engine enforcing the
core business controls. It governs **SalesInvoice, PurchaseInvoice, PaymentEntry, JournalEntry**.

Controls:

- **Maker/checker separation** — a submitter may not approve their own document.
- **Amount-threshold approval** — amounts over the definition's threshold require a separate
  approver role; within the threshold the document auto-posts.
- **Mandatory rejection reason** on reject.
- **Audit event for every transition.**
- **GL/stock posting happens only on the transition to Submitted**, via a caller-supplied delegate.
- **Posted documents are immutable** — corrections go through cancel/reversal, never silent edits.

States: `Draft -> PendingApproval -> Submitted (posted)`; `Reject (with reason)`; `Cancel/reversal`.

## New production-order lifecycle (this sprint)

`src/LafErp.Services/ManufacturingService.cs` adds an audited production lifecycle:

```
Draft -> MaterialsIssued -> QualityPassed | QualityFailed -> Completed (immutable)
```

`Complete` requires a quality pass and receives the finished good into stock; the order is then
immutable. See the Manufacturing Depth Report for full rules.

## Tests

Lifecycle behaviour is covered across `WorkflowTests.cs`, `AccountingTests.cs`,
`ManufacturingTests.cs` and the scenario libraries (part of the 255-test xUnit total).

## Honest limitations / not done

The following document chains were **NOT** added this sprint:

- **Delivery-note chain** — `DeliveryNote` exists only as a CRUD skeleton. There is no
  Sales-Order -> Delivery-Note -> Sales-Invoice fulfilment chain with stock relief.
- **Sales/Purchase return chains** — `CreditNote` and `DebitNote` exist only as CRUD skeletons;
  they do **not** reverse stock or GL.
- **Partial delivery / partial receipt** against an order.
- **Purchase-receipt -> purchase-invoice** matching chain.

The maker/checker lifecycle (four financial documents) and the new production-order lifecycle are
real and tested; the delivery/return document chains are explicitly outstanding.
