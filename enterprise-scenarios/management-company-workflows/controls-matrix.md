# Controls Matrix — Management-Company Workflows

Original synthetic mapping of management oversight intents to platform evidence. Awareness-only; not
a compliance attestation and not an assertion of regulatory sufficiency.

| Oversight intent (design) | Evidence the platform can derive | Mode |
|---|---|---|
| Every change has a known blast radius | `impact(target)` | Graph-derived |
| Controlled objects have known consumers | `dependents(target)` | Graph-derived |
| Data lineage is traceable for review | `dependencies(target)` + provenance | Graph-derived |
| Structural graph did not regress in the period | benchmark Gold/PASS (COREBANK/KYCAML/ERPCRM) | Graph-derived |
| Approval authorities are defined and maintained | `approval-matrix.md` | Advisory |
| Segregation (maker ≠ checker) | proc/constraint level in fixtures | Partly graph-derived / partly advisory |
| Management reporting reflects current estate | dashboard views + graph queries | Advisory + graph anchors |
| Regulatory / scheme conformance | — | Advisory / awareness-only (not claimed) |

## Notes

- Graph-derived items trace to a node/edge in an imported artifact.
- Advisory items are design references; they assert no compliance guarantee.
