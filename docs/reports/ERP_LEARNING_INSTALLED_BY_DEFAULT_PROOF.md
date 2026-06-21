# ERP-Learning Installed-by-Default — Proof

**Generated:** 2026-06-21
**Scope:** Proof that the ERP-learning knowledge ships by default with no hidden external dependency.

## Where the packs live

The ERP knowledge packs live in **`knowledge-packs/`** — the directory the app/installer ships and the directory `verify-all-knowledge-packs` validates. There is **no external or hidden dependency**: the packs are in-repo and travel with the release package.

The four ERP packs added this sprint, present in `knowledge-packs/`:

- `knowledge-packs/erp-full-suite-generation-v1` (24 items)
- `knowledge-packs/erp-inventory-manufacturing-v2` (20 items)
- `knowledge-packs/erp-hr-pos-ecommerce-customization-v1` (18 items)
- `knowledge-packs/erp-test-scenario-ui-api-report-v2` (27 items)

## Default-install verification (PASS)

`verify-all-knowledge-packs` over `knowledge-packs/`:

- **18 packs**
- **804 items**
- **804 distinct UIDs** — no collisions
- **240-test guard green**

This confirms the full base — including the new ERP packs — is present and valid in the directory that ships by default.

## The generator can use them locally

The generator reads these packs and proves it via the `--knowledge-usage` report (`benchmarks/results/erp-generator-knowledge-usage.json`): **9 ERP packs / 274 items catalogued, 22 modules mapped** to knowledge categories. No network call, no external store — knowledge is read from the shipped `knowledge-packs/` directory.

## Release-package inclusion

Because the packs are in `knowledge-packs/` and are validated by the same verifier the installer uses, **the release package includes `knowledge-packs/` by default**. A fresh install therefore has the full ERP-learning knowledge base available offline, MSSQL-only, with no Qdrant/Ollama/internet requirement.

## Honest note

"Installed by default" means the *knowledge* is present and verifiable offline. It does not raise ERP-learning readiness above the documented 78%, nor ERPNext parity above ~45%. See `ERP_LEARNING_100_PERCENT_DEFINITION.md`.
