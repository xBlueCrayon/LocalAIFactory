# Workflow: Management-Company Oversight

Original synthetic workflow. Awareness-only; not an operational procedure or compliance process.

## Recurring oversight cycle

1. **Collect proposed changes** — gather the change requests against the managed estate for the
   review window.
2. **Governance triage** — for each change, run `impact(target)` to size the blast radius and list
   the reached services/reports/procs with provenance.
3. **Control-coverage check** — for each controlled object touched, confirm known consumers via
   `dependents(target)` and check the change against `controls-matrix.md`.
4. **Approval-authority assignment** — set the required maker/checker roles from `approval-matrix.md`
   for the change class and risk/amount tier.
5. **Sign-off** — the management company records a governance decision (maker submits, a different
   checker approves) in its change-management system.
6. **Assurance** — run the backing benchmark proofs (`COREBANK`, `KYCAML`, `ERPCRM`) to confirm the
   structural graph did not regress; capture the Gold/PASS result for the management report.
7. **Report** — publish the dashboard views (`operating-manager-dashboard.md`) for the period.

## Exceptions

- Changes touching objects with only advisory control coverage are flagged for human control-owner
  review; the platform marks this honestly and asserts no sufficiency.

## Inputs and outputs

- Input: managed C#/SQL estate, the change list, and the matrices in this folder.
- Output: a governance decision per change, an approval route, and an assurance (no-regression)
  result for the period.
