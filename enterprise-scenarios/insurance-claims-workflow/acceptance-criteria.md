# Acceptance Criteria — Measurable Checklist

> Fictional scenario (Zephyr Mutual Assurance). Awareness-only on FSC/Mauritius.
> Not legal or insurance advice; no compliance or certification claim.

Each item is binary (pass/fail) and observable. A build is acceptable only when all
"Must" items pass.

## Workflow and state

- [ ] A claim can be registered against an existing policy and stores a cover snapshot. (Must)
- [ ] A claim can traverse every state from FNOL to closure without manual DB edits. (Must)
- [ ] Illegal transitions (e.g. settle before assessment) are rejected with a typed error. (Must)
- [ ] Every transition records actor, role, timestamp, and reason. (Must)

## Separation of duties and authority

- [ ] The actor who raises a settlement cannot approve the same settlement. (Must)
- [ ] The approver of a settlement cannot also release its payment. (Must)
- [ ] Approval above an actor's authority threshold is blocked server-side. (Must)
- [ ] Authority thresholds and role mappings are configurable without a migration. (Must)

## Reserves

- [ ] Setting or revising a reserve creates a new history row; no in-place edit. (Must)
- [ ] Every reserve change carries a reason code and set-by / set-at. (Must)
- [ ] Concurrent reserve edits are blocked by optimistic concurrency. (Must)
- [ ] Reserve history for a claim is retrievable in chronological order. (Must)

## Payments

- [ ] A payment instruction requires raise, approve, and release by distinct actors. (Must)
- [ ] A rejected/failed payment instruction is recoverable without double payment. (Must)
- [ ] No funds are moved by the platform; only an instruction file is produced. (Must)

## Audit

- [ ] An append-only audit event is written in the same transaction as each change. (Must)
- [ ] Audit records cannot be edited or deleted through the application. (Must)
- [ ] A full ordered audit extract for any claim is produced on demand. (Must)

## Resilience and local-first

- [ ] Core pages render with MSSQL only (no internet, GPU, or external AI). (Must)
- [ ] Claim registration works when the policy feed is unavailable (manual fallback). (Must)
- [ ] No blocking external-service call occurs on the request path. (Must)
- [ ] Core pages return in well under one second on SQL Express. (Should)

## Reporting

- [ ] Open-claims-by-status and reserve-movement reports return correct totals. (Must)
- [ ] Cycle-time (FNOL→payment) report computes from logged transitions. (Should)
- [ ] An SLA-breach exception list is produced. (Should)

## Awareness and scope guards

- [ ] No screen or document asserts regulatory compliance or FSC conformance. (Must)
- [ ] KYC/AML features are clearly labelled awareness-only, not a screening product. (Must)
- [ ] Schema changes in the build are additive and backward-compatible. (Must)
