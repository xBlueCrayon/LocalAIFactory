# LAF Product Maturity — After the Software Reasoning Engine

**Date:** 2026-06-21 · Data: `benchmarks/results/laf-product-maturity-score.json`

## Headline

| | Before | After |
|---|---|---|
| Overall maturity | **64.55** | **70.6** |
| Classification | knowledge-engine + generator | **`LAF_SOFTWARE_REASONING_ENGINE_PILOT_READY`** |

The 72+ stretch was **not** reached. A genuine, tested reasoning engine was delivered, but the ambitious
per-component test targets and a full autonomous agent loop + rich UI were not completed in one sprint.

## What genuinely improved

| Capability | Δ | Why |
|---|---|---|
| Code understanding | 55 → 74 | Real Roslyn CodeGraph (1308 nodes / 1699 edges over the repo), roles, typed edges, transitive impact |
| Graph retrieval | 40 → 72 | Deterministic hybrid retrieval (symbol/impact/tests/knowledge/experience), no Qdrant |
| Safe agent execution | 20 → 70 | Default-deny tool gateway + path sandbox + isolated patch runner with rollback, **no commit/push capability** |
| Experience memory | 15 → 68 | Record/find-similar/idempotent-promote/link/persist |
| Model routing | 30 → 66 | Role roster + health + **graceful Ollama fallback** + audit log; never blocks core |

## Proof, not claims

- **124 new tests** (113 in the reasoning engine + 11 reasoning UI/API), all green **without Ollama or Qdrant**.
- A benchmark runs the engine over the **real repo**: 1308 nodes, 1699 edges, 973 knowledge items, **14/15
  tasks = 93.3%** with no model (`benchmarks/results/laf-software-reasoning-benchmark.json`).
- Case study proves reasoning over LAF's **own generated ERP** (`laf-reasoning-erp-gold-case-study.json`).

## Honest limitations (not faked)

- The reasoning is **syntax-only**: references are by simple name, so precise call graphs, dynamic dispatch,
  and cross-assembly resolution are heuristic, not exact.
- **Per-component test targets not met** (CodeGraph 22/50, Retrieval 13/50, Safety 40/60, AgentRunner 7/50,
  Experience 16/40, ModelRouter 11/30, UI/API 11/40). A cohesive engine was prioritised over hitting counts.
- The agent runner is a **safe validated-patch core**, not a full autonomous plan→patch→verify loop driven by
  a model; the model layer proposes/reviews only and is optional.
- UI is functional but thin; production-hardening and the external gates (security review, CA TLS, SSO/OIDC,
  customer pilot) are unchanged.
