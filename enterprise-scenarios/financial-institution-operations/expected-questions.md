# Expected Questions — Financial-Institution Operations

The operational questions this scenario expects the platform to support. Graph-derived questions are
answered over the imported estate (proven by the COREBANK / KYCAML / ERPCRM fixtures); advisory
questions are answered from the matrices in this folder.

## Graph-derived

1. What is the blast radius if we change `dbo.Account` (or any controlled table)? — `impact(target)`
2. Which services post a transaction / submit an approval? — `dependents(target)`
3. What data does a given service touch? — `dependencies(target)`
4. Where is a controlled object defined? — `find(target)`

## Advisory

5. Which maker/checker roles are required for a controlled-schema change? — `approval-matrix.md`
6. Which control intents are graph-derived vs advisory? — `controls-matrix.md`
7. What should the operating manager review daily? — `operating-manager-dashboard.md`

The graph-derived answers carry provenance to a file and line span; the advisory answers are design
references and assert no compliance guarantee.
