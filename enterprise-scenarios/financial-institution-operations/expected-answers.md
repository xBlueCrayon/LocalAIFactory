# Expected Answers — Financial-Institution Operations

This scenario is an operations narrative over the platform's graph capabilities; its concrete,
graph-derived answers are the ones proven by the backing benchmark fixtures.

| Question | Mode | Where it is proven |
|---|---|---|
| Blast radius of a posting/account change | impact | COREBANK fixture (`impact("dbo.Account")`) |
| Who submits/approves a transaction | dependents | KYCAML fixture (`dependents("dbo.usp_*Transaction")`) |
| What a service touches | dependencies | ERPCRM / KYCAML fixtures |
| Where a controlled object is defined | find | any imported estate |

## Advisory answers (from this folder)

- Approval routing for a change class → `approval-matrix.md`.
- Control coverage (graph-derived vs advisory) → `controls-matrix.md`.
- Daily review surface → `operating-manager-dashboard.md`.

## Honest limits

- This scenario does not ship its own benchmark fixture; it composes proven capabilities.
- Approval thresholds and segregation beyond proc/constraint level are advisory.
- No regulatory sufficiency is asserted.
