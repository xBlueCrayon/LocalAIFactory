# Operating-Manager Dashboard — Management-Company Workflows

Original synthetic dashboard sketch for a management-company operating manager. Awareness-only;
illustrative views, not live KPIs.

## Panels

1. **Governance queue** — proposed changes for the period by change class, each with its
   `impact(target)` blast-radius size.
2. **Control-coverage heatmap** — controlled objects with their consumer count from
   `dependents(target)`; flag zero-consumer or advisory-only objects.
3. **Assurance status** — latest Gold/PASS of the backing fixtures (`COREBANK`, `KYCAML`, `ERPCRM`):
   the structural graph did not regress.
4. **Approval-authority register** — current maker/checker authorities (`approval-matrix.md`) and
   any pending authority changes.
5. **Escalations** — changes routed to control owner / risk committee this period.

## Manager actions supported by the platform

- "Size the blast radius of every change this period." → `impact(target)` per change.
- "Which controlled objects lack a known consumer?" → `dependents(target)` sweep.
- "Did the graph regress?" → re-run the backing benchmark fixtures.

Graph-derived panels carry provenance. Approval-authority and escalation panels are advisory.

## Caveats

Panels mixing graph data with external registers are illustrative; they are not operational or
regulatory status.
