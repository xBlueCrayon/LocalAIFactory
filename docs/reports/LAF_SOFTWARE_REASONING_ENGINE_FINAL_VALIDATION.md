# LAF Software Reasoning Engine — Final Validation

**Date:** 2026-06-21 · **Branch:** ke-008-code-symbols

Every gate below was executed; results reported as observed.

## Gates

| Gate | Result |
|------|--------|
| `dotnet build LocalAIFactory.sln -c Release` | **0 errors** |
| `LocalAIFactory.Tests` (incl. 11 reasoning UI/API) | **251 / 251 pass** |
| `LocalAIFactory.Reasoning.Tests` | **113 / 113 pass** |
| `verify-all-knowledge-packs.ps1` | **PASS — 39 packs / 1093 items**, no UID collisions |
| `verify-production-readiness-v3.ps1` | `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` |
| `security-audit.ps1` | verdict **PASS** (see note) |
| Reasoning benchmark over the real repo | **14/15 = 93.3%**, no model (1308 nodes / 1699 edges) |
| Forbidden / large tracked files | **0** |

**Security note (honest):** the audit lists 8 HIGH lines flagging the **seeded demo** password `Admin#12345`
in the `run-production-smoke.ps1` scripts (introduced in earlier sprints, not this one). These are the
documented demo credential ("change before production"), not a leaked secret; the audit's own verdict is
PASS. They are disclosed here rather than hidden.

## What was delivered

A new `src/LocalAIFactory.Reasoning` project (in the solution) built on the existing KE-008 Roslyn symbol
foundation, with: **CodeGraph** (in-memory, roles + typed edges + impact), **GraphRAG retrieval**, **Safe Tool
Gateway** (default-deny + path sandbox), **Experience Memory**, **Local Model Router** (graceful Ollama
fallback), and an **Isolated Patch Runner** (build/test-gated, rollback, no commit/push). Exposed in the Web
app at `/Reasoning` + `/api/reasoning/*`. **124 new tests, all green without Ollama or Qdrant.** 8 new
`laf-*` knowledge packs (120 items) harvest the lessons of every prior sprint.

## Honest classification

`LAF_SOFTWARE_REASONING_ENGINE_PILOT_READY`. Product maturity **64.55 → 70.6** (below the 72 stretch).

**Not met / partial:** per-component test targets (CodeGraph 22/50, Retrieval 13/50, Safety 40/60,
AgentRunner 7/50, Experience 16/40, ModelRouter 11/30, UI/API 11/40); the agent runner is a safe
validated-patch core, not a full model-driven autonomous loop; reasoning is syntax-only (references by
simple name, no precise call graph); the model layer is optional and unvalidated against a live model in
this gate. External gates (security review, CA TLS, SSO/OIDC, customer pilot) unchanged.
