# Workflow: Financial-Institution Operations

Original synthetic workflow. Awareness-only; not an operational procedure or compliance process.

## Daily operations loop

1. **Intake change** — a developer proposes a schema or service change (e.g. add a column to a
   posting table, alter a screening proc).
2. **Triage impact** — operations runs `impact(target)` over the imported estate to list the C#
   services, reports, and procs the change reaches. Provenance points to each file/line.
3. **Review controls** — cross-reference the touched objects against `controls-matrix.md`: does the
   change weaken idempotency, segregation, or audit anchors?
4. **Route for approval** — use `approval-matrix.md` to determine the maker/checker roles required
   for the change class and the amount/risk tier involved.
5. **Record decision** — the maker submits, a different checker approves; the decision and its
   rationale are recorded outside the graph (change-management system).
6. **Post-change verification** — re-run `dependents(target)` to confirm the intended consumers,
   and re-run the relevant benchmark fixture proof (COREBANK / KYCAML / ERPCRM) to confirm no graph
   regression.

## Escalation

- A change that touches a controlled object with only **advisory** coverage (segregation beyond
  proc level, regulatory sufficiency) is escalated to a human control owner — the platform marks
  these honestly and does not assert sufficiency.

## Inputs and outputs

- Input: imported C#/SQL estate + the matrices in this folder.
- Output: a triaged impact list with provenance, an approval route, and a verification result.
