# ERP Default Knowledge Base — Upgrade Report

**Date:** 2026-06-21
**Validator:** `scripts/knowledge/verify-all-knowledge-packs.ps1` — **PASS**

## What changed

Three **new default knowledge packs** were added under `knowledge-packs/` (the directory that is installed by default), and a pre-existing item-count bug was fixed.

### New packs

| Pack | Items |
|------|------:|
| `production-grade-erp-controls-v1` | 22 |
| `erp-testing-and-scenarios-v1` | 24 |
| `laf-erp-generation-lessons-v1` | 22 |

- **production-grade-erp-controls-v1 (22):** controls expected of a production-grade ERP (double-entry, maker/checker, audit, RBAC, etc.). Files: `erp-controls.json`, `manifest.json`, `source-registry.json`.
- **erp-testing-and-scenarios-v1 (24):** testing patterns + business scenarios for an ERP. Files: `testing-patterns.json`, `business-scenarios.json`, `manifest.json`, `source-registry.json`.
- **laf-erp-generation-lessons-v1 (22):** generator failure patterns, LLM hallucination patterns, collision guards, module-spec rules, validation gates (detailed in `LAF_ERP_GENERATION_LESSONS_INSTALLED_REPORT.md`). Files: `generation-lessons.json`, `manifest.json`, `source-registry.json`.

### Bug fix

`engineering-leadership-and-innovation-v1` had a stale `itemCount` (26 declared vs 25 actual). The manifest was corrected to **25**, matching the real item count.

## Default installation

The packs live in `knowledge-packs/`, the directory installed by default with the product, so these three packs ship as part of the default knowledge base — no manual import step.

## Validation

`verify-all-knowledge-packs.ps1` — **PASS**:

- **10 packs**
- **648 items**
- **648 distinct UIDs**
- **no collisions**

Per-pack item counts (sum = 648):

| Pack | Items |
|------|------:|
| professional-base-v1 | 390 |
| production-issue-fixes-v1 | 77 |
| enterprise-workflows-v1 | 40 |
| engineering-leadership-and-innovation-v1 | 25 |
| erp-testing-and-scenarios-v1 | 24 |
| production-grade-erp-controls-v1 | 22 |
| laf-erp-generation-lessons-v1 | 22 |
| financial-institution-operations-v1 | 16 |
| kyc-aml-transaction-approval-v1 | 16 |
| market-intelligence-forecasting-v1 | 16 |
| **Total** | **648** |
