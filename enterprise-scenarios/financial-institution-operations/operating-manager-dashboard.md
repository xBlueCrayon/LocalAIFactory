# Operating-Manager Dashboard — Financial-Institution Operations

Original synthetic dashboard sketch. Awareness-only; illustrative views, not live KPIs.

## Panels

1. **Pending changes by class** — count of in-flight changes grouped by change class
   (`approval-matrix.md`), with the impact-list size from `impact(target)`.
2. **Controlled-object coverage** — for each controlled object, the number of known consumers from
   `dependents(target)`; flag objects with zero known consumers for review.
3. **Advisory-only exposure** — changes that touch objects with only advisory control coverage
   (escalation candidates).
4. **Benchmark health** — last Gold/PASS status of the `COREBANK`, `KYCAML`, and `ERPCRM` fixtures
   (graph did not regress).
5. **Approval throughput** — maker/checker decisions over a window (from the change-management
   system, referenced here).

## Manager actions supported by the platform

- "What is the blast radius of this change?" → `impact(target)`.
- "Who consumes this controlled object?" → `dependents(target)`.
- "Show me service → data lineage for this method." → `dependencies(target)`.

Answers are graph-derived with provenance. Approval and risk figures are advisory.

## Caveats

Panels 1, 3, and 5 mix graph-derived data with references to external systems and matrices; they
are illustrative and must not be read as operational or regulatory status.
