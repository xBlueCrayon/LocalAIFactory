# Expected Questions — Management-Company Workflows

The oversight questions this scenario expects the platform to support. Graph-derived questions are
answered over the managed estate (proven by the COREBANK / KYCAML / ERPCRM fixtures); advisory
questions are answered from the matrices in this folder.

## Graph-derived

1. What is the blast radius of each proposed change this period? — `impact(target)`
2. Which controlled objects lack a known consumer? — `dependents(target)` sweep
3. What is the data lineage for a flagged service? — `dependencies(target)`
4. Did the structural graph regress this period? — re-run backing benchmark fixtures (Gold/PASS)

## Advisory

5. Who must approve a controlled change of a given class? — `approval-matrix.md`
6. Which oversight intents are graph-derived vs advisory? — `controls-matrix.md`
7. What goes on the period management report? — `operating-manager-dashboard.md`

Graph-derived answers carry provenance; advisory answers are design references and assert no
compliance guarantee.
