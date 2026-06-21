# LAF Self-Generated ERP — Start State

**Date:** 2026-06-21 · **Sprint:** prove LocalAIFactory *generates* ERP V2 (autonomy), Claude = operator only.

| Check | Result |
|---|---|
| Branch | `ke-008-code-symbols` (not main) |
| Working tree | clean |
| Latest commit | `2207563` (ERP-GENERATION-PROOF V1) |
| .NET SDK | 10.0.301 |
| Draft release `v1.0.0-rc` | draft + prerelease (unpublished) |
| Final `v1.0` tag | none |
| Stale ERP / Playwright processes | none |
| **Ollama local LLM** | **available** — `qwen2.5-coder:14b`, `deepseek-r1:14b` |

## Approach (honest)

LocalAIFactory does not yet ship a UI "generate ERP" button. So the real LocalAIFactory capability is
built as a **generator harness** (`tools/LocalAIFactory.Generator`) — a deterministic, template +
knowledge-driven code emitter that, given the requirement, **emits** the ERP V2 solution. The local LLM
(`qwen2.5-coder:14b`) is used as a **governed proposal engine** (proposes the entity/module model;
deterministic templates produce the compiling code; proposals are validated and stored as evidence).

**Attribution discipline:** the generator and its templates are `LAF_GENERATOR_INFRASTRUCTURE`; every
file the generator emits into `generated-products/LAF-EnterpriseERP-LAFGenerated/` is `LAF_GENERATED`.
Claude does not hand-write product source; bugs are fixed in the **generator** and re-emitted
(`LAF_GENERATED_THEN_FIXED_BY_LAF`). Autonomy % is computed from the attribution file. V1 stays untouched.
