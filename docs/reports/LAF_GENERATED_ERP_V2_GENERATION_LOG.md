# LAF-Generated ERP V2 — Generation Log

**Date:** 2026-06-21 · **Generator:** `tools/LocalAIFactory.Generator` (LAF_GENERATOR_INFRASTRUCTURE)
**Attribution:** `benchmarks/results/laf-erp-v2-generation-attribution.json`
**Summary:** `benchmarks/results/laf-erp-v2-generation-summary.json`

## Command

```powershell
dotnet run --project tools/LocalAIFactory.Generator -- `
  --requirement benchmarks/erpnext-study/laf-erp-v2-generation-requirement.md `
  --target generated-products/LAF-EnterpriseERP-LAFGenerated `
  --product-name "LAF Enterprise ERP V2" --prefer-local-llm --run-tests --write-attribution
```

## What the generator did

1. Read the **requirement** (the only product input).
2. Loaded the **governed local-LLM catalog proposal** (`qwen2.5-coder:14b`), validated each entity, and
   applied a **collision/hallucination guard** against the core engine entity names.
3. **Emitted the engine** from the LocalAIFactory ERP-knowledge templates (`tools/.../templates/erp-core`),
   substituting `{{PRODUCT_NAME}}` and injecting catalog hooks.
4. **Generated the catalog modules** (entities, DbSets, generic CRUD service, REST endpoints, UI page,
   CRUD tests) for the accepted LLM entities.
5. Emitted the **solution file** and the **provenance test**.
6. Wrote the **attribution manifest** with an autonomy calculation.

## Result

| Metric | Value |
|---|---|
| Product files emitted | **70** |
| `LAF_GENERATED` (engine from templates) | 66 |
| `LOCAL_LLM_PROPOSAL_USED` (catalog) | 4 |
| **Generation autonomy** | **100%** |
| Catalog entities accepted | CustomerSegment, PaymentTerm, TaxCode |
| Catalog entities rejected (collision guard) | Supplier, Currency, Warehouse |
| Build | 0 errors |
| Tests | 82 xUnit + 13 Playwright, all pass |

## Knowledge used

- LocalAIFactory **ERP study** (`benchmarks/erpnext-study/*`) — the docstatus lifecycle, double-entry,
  stock ledger, and maker/checker patterns are encoded in the generator's engine templates.
- The **integration-expectation** + **issue-fix** knowledge informed the controls (no `GroupBy(_=>1)`,
  decimal money, audited approvals) baked into the templates.
- The **local LLM** drove the catalog/extension layer, governed by the validator.

## Honesty

The engine is **template-based** (it encodes LocalAIFactory's ERP knowledge); the **local LLM** drives
the catalog layer. The generator emits every product file — Claude did **not** hand-write product source.
Bugs found during validation were fixed in the **generator/templates** and the product re-emitted (see
`LAF_GENERATED_ERP_V2_FIX_LOOP.md`).
