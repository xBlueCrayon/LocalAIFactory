# ERP V5 Learnings Installed in Default LAF

This sprint's gains are **reusable and installed by default**, so a fresh LAF deploy can
regenerate a V5-grade ERP without manual setup.

## What is now default

### 1. Generator create-UI upgrade

The generator now emits, for every module, a reflection-driven `CatalogController.Create`
(GET form + POST persist with audit), a generated `Create.cshtml`, and create links on
`/Catalog`. This lives in the generator (`tools/LocalAIFactory.Generator`), so it applies to
**any** future generated product, not just V5. Proven by the Playwright create-form test.

### 2. Four new production knowledge packs (default-installed)

- `erp-accounting-production-v1`
- `erp-selling-buying-stock-production-v1`
- `erp-ui-api-workflow-production-v1`
- `erp-deployment-security-production-v1`

These install by default and feed the generator at generation time
(`benchmarks/results/erp-generator-knowledge-usage-v2.json`: 10 packs / 296 items,
29 modules mapped). `verify-all-knowledge-packs` is expected PASS.

## Why this matters

Because both the create-UI capability and the production packs are baked into the default
generator + knowledge base, a **fresh clone/deploy can regenerate a V5-grade ERP** (29 modules,
create-form UI, GL/P&L/Balance Sheet, 134/14 tests) at 100% autonomy with 0 manual
product-source edits — reproducing this sprint's result rather than depending on it.

## Honest scope

"V5-grade" means **ERP_PILOT_READY**, not production-grade and not ERPNext free-grade. The
reusable learnings advance the pilot; they do not close the remaining local gates (EF
migrations, edit/delete UI, backup/restore, load test, deep modules) or the external gates
(auth, CA TLS, security review, customer acceptance).
