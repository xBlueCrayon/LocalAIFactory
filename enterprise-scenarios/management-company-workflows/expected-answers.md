# Expected Answers — Management-Company Workflows

This scenario is an oversight narrative over the platform's graph capabilities; its concrete,
graph-derived answers are the ones proven by the backing benchmark fixtures.

| Question | Mode | Where it is proven |
|---|---|---|
| Blast radius of a proposed change | impact | COREBANK / KYCAML / ERPCRM fixtures |
| Controlled-object consumers | dependents | KYCAML / ERPCRM fixtures |
| Service data lineage | dependencies | ERPCRM / KYCAML fixtures |
| Graph did not regress | benchmark Gold/PASS | harness over all backing fixtures |

## Advisory answers (from this folder)

- Governance approval authorities → `approval-matrix.md`.
- Oversight control coverage (graph-derived vs advisory) → `controls-matrix.md`.
- Period reporting surface → `operating-manager-dashboard.md`.

## Honest limits

- This scenario ships no fixture of its own; it composes proven capabilities.
- Approval authorities and segregation beyond proc/constraint level are advisory.
- No regulatory sufficiency is asserted.
