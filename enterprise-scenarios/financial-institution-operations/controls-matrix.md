# Controls Matrix — Financial-Institution Operations

Original synthetic mapping of operational control intents to where the platform can evidence them.
Awareness-only; not a compliance attestation and not an assertion of regulatory sufficiency.

| Control intent (design) | Evidence the platform can derive | Mode |
|---|---|---|
| Change-impact known before release | `impact(target)` over imported estate | Graph-derived |
| Controlled objects have known consumers | `dependents(target)` | Graph-derived |
| Service → data lineage is traceable | `dependencies(target)` + provenance | Graph-derived |
| Duplicate-posting / duplicate-submit guarded | `UNIQUE` keys + proc guards in fixtures | Graph-derived (where present) |
| Maker/checker segregation | proc guards + `UNIQUE` keys (COREBANK/KYCAML) | Partly graph-derived / partly advisory |
| Approval limits by risk/role | `ApprovalLimit` table (KYCAML) + `approval-matrix.md` | Partly structural / advisory |
| Audit of approver identity | append-only audit fields + archive anchors | Advisory + partial graph anchor |
| Regulatory / scheme conformance | — | Advisory / awareness-only (not claimed) |

## Notes

- "Graph-derived" means the answer is traceable to a node and edge in an imported artifact.
- Advisory items are design references in this folder; they are not proofs and assert no
  compliance guarantee.
