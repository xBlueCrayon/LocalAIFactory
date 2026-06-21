# LocalAIFactory Improvements from the Self-Generated ERP

**Date:** 2026-06-21

Building and running the generator surfaced concrete improvements to LocalAIFactory's generation
capability. Implemented improvements are in `tools/LocalAIFactory.Generator`; the rest are a backlog.

## Implemented this sprint

1. **A real ERP generator** (`tools/LocalAIFactory.Generator`) — template + knowledge-driven emission with
   a governed local-LLM proposal stage and an attribution manifest (autonomy calculation).
2. **Collision / hallucination guard** on LLM proposals — rejects entities that collide with core engine
   names or fail schema validation. It caught 3 of 6 proposed entities in this run.
3. **Self-healing generation fixes** — three generation bugs were fixed in the generator (not the product)
   and re-emitted, proving the "fix the generator, regenerate" loop:
   - marker placement must be on its own line (templating robustness),
   - minimal-API params for open-generic services need `[FromServices]`/`[FromBody]`,
   - generated MVC controller/view/route names must agree.
4. **A fix-loop harness** (`scripts/generator/run-laf-generated-erp-fix-loop.ps1`) that regenerates →
   builds → tests → (optionally) Playwrights and reports failures for the next generator fix.

## Backlog (honest gaps to close)

- **Data-driven engine, not just templates.** The accounting/stock/workflow engine is currently emitted
  from templates (encoded LocalAIFactory knowledge). A higher-autonomy generator would synthesize the
  engine from a declarative model, not fixed templates.
- **Deeper local-LLM role.** The LLM today drives only the catalog layer. Next: have it propose the full
  module/entity/relationship model and service contracts, all behind stronger validation + tests.
- **Parity-from-tests scoring.** Compute parity automatically from passing tests per feature row rather
  than authoring the score.
- **EF migration emission** instead of `EnsureCreated`.
- **UI form generation** (create/edit), not just list pages.
- **Generator validation gate** that runs build+test+Playwright as part of generation and refuses to emit
  a non-green product.

## Impact on confidence

This run proves LocalAIFactory can **generate, govern, validate, and self-heal** a real, tested ERP at
100% file autonomy — a genuine step toward the platform's purpose. It does **not** yet prove fully
LLM-authored engine code; that is the headline backlog item.
