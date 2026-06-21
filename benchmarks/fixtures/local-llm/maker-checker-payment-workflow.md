# Workflow: Maker / Checker Payment Release (synthetic fixture)

A bank operations team releases outbound payments under a maker/checker control.

## Roles
- **Maker** — an operations clerk who captures and submits a payment instruction.
- **Checker** — a supervisor who reviews and approves or rejects; must be a different person from the maker.
- **Approver** — a senior officer who authorizes release for high-value payments (> 1,000,000).

## States
`Draft` → `Submitted` → `Screened` → `PendingApproval` → `Released` (terminal) or `Rejected` (terminal).

## Transitions
- Maker submits: `Draft → Submitted`.
- Sanctions screening runs: `Submitted → Screened` (a clear screen is required to proceed).
- Checker decision: `Screened → PendingApproval` (approve) or `Screened → Rejected` (with a rejection reason).
- Approver authorizes high-value: `PendingApproval → Released`.

## Required data
Payment reference, debtor account, creditor account, amount, currency, maker user, checker user, approver
user, sanctions screening result, rejection reason (when rejected).

## Validations
- Amount > 0; debtor ≠ creditor; currency is a known ISO code.
- Checker must differ from maker (segregation of duties).
- Sanctions screening result must be "Clear" before release.

## Audit
Every state change writes an audit event with actor, action, timestamp, and the from/to state. Rejections
record a reason.

## Exceptions
- Sanctions hit → route to a compliance exception queue (not auto-rejected).
- Approver timeout (no decision in 4h) → SLA escalation to the duty manager.
