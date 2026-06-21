# LAF Software Reasoning Engine — Fresh-Clone Proof (Phase 14)

**Date:** 2026-06-21 · **Commit proven:** `45f10c3`

Proves the committed repository is self-sufficient: a clean clone builds and runs the reasoning engine with
no uncommitted state.

## Procedure & result

1. `git clone --depth 1` of the LAF-REASONING commit into a temp directory.
2. Confirmed artifacts present: `src/LocalAIFactory.Reasoning/CodeGraph/CodeGraph.cs` and all 8 new
   `laf-*` knowledge packs.
3. `dotnet build LocalAIFactory.sln -c Release` → **0 errors**.
4. `dotnet test LocalAIFactory.Reasoning.Tests` → **113 passed, 0 failed**.
5. `dotnet test LocalAIFactory.Tests` (incl. reasoning UI/API) → **251 passed, 0 failed**.
6. `verify-all-knowledge-packs.ps1` → **PASS — 39 packs / 1093 items, no UID collisions**.

**PASS.** A clean clone builds and exercises the entire reasoning engine — CodeGraph, retrieval, safe tool
gateway, experience memory, model router, isolated patch runner, and the Web `/api/reasoning/*` surface —
**without Ollama or Qdrant**. The temporary clone was discarded after the run and is not committed.
