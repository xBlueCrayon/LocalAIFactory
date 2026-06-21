# LAF ERP V2 — Local-LLM Generation Proof

**Date:** 2026-06-21 · **Model:** `qwen2.5-coder:14b` (Ollama, local, no paid service)
**Evidence:** `generated-products/LAF-EnterpriseERP-LAFGenerated/generation-evidence/`

## Model availability (real)

`GET http://localhost:11434/api/tags` returned `qwen2.5-coder:14b` and `deepseek-r1:14b`. The generation
used **qwen2.5-coder:14b**.

## Prompt (stored)

`generation-evidence/llm-catalog-prompt.txt` — asked for 6 additional ERP master/catalog entities (not in
the accounting/inventory/CRM core) as strict JSON with typed fields.

## Raw response (stored)

`generation-evidence/llm-catalog-raw-response.txt` — 1,889 chars, 471 eval tokens, ~6 s. Proposed 6
entities: Supplier, CustomerSegment, Currency, PaymentTerm, Warehouse, TaxCode.

## Governance (proposal NOT trusted blindly)

The generator validated each entity and applied a **collision/hallucination guard**:

| Entity | Decision | Reason |
|---|---|---|
| Supplier | **REJECTED** | collides with a core engine entity |
| Currency | **REJECTED** | collides with a core engine entity |
| Warehouse | **REJECTED** | collides with a core engine entity |
| CustomerSegment | ACCEPTED | 3 valid fields |
| PaymentTerm | ACCEPTED | 3 valid fields |
| TaxCode | ACCEPTED | 3 valid fields |

The LLM hallucinated 3 entities that already exist in the core; the guard caught and dropped them. Only
validated, non-colliding entities were generated. The local LLM **never overwrote source directly** — it
proposed a model; the deterministic generator produced the compiling code.

## Files influenced by the local LLM

`src/LafErp.Core/CatalogEntities.cs`, the catalog DbSets/endpoints, `Views/Catalog/Index.cshtml`,
`CatalogController.cs`, and `tests/.../CatalogGeneratedTests.cs` (classified `LOCAL_LLM_PROPOSAL_USED`).

## Hallucination checks

- Schema validation (name PascalCase, fields typed from an allow-list, required `Name`).
- Collision guard against 40+ reserved core entity names.
- The generated catalog modules are covered by **passing CRUD + validation tests**, proving the
  LLM-derived code is correct, not just plausible.

## Safety constraints

MSSQL/LocalAIFactory knowledge (the engine templates) remained the source of truth; the LLM only
expanded the catalog layer; all output is governed, validated, and test-covered. If Ollama were
unavailable, the generator falls back to 0 catalog entities (deterministic engine only) — no faking.
