# LAF Software Reasoning Engine V2 — Product Definition

**Stamp:** 2026-06-21
**Baseline:** commit `5e70877` (V1)
**Classification reached:** `LAF_SOFTWARE_REASONING_ENGINE_V2_LOCAL_CORE_READY`

## What the V2 product is

The LAF Software Reasoning Engine V2 is a **local-first, deterministic-core** engine that:

1. **Composes software from reusable building blocks.** Given a requirement, it deterministically
   selects the relevant blocks (with transitive dependencies), aggregates the files, tests,
   Playwright proofs, security rules, migration impact, and knowledge it would draw on, and
   **honestly reports any capability it has no block for**.
2. **Proposes, validates, and rolls back patches safely.** A model (or stub) only proposes; the
   runner risk-assesses, applies in an isolated worktree under build/test gates, rolls back on
   failure, and emits a human-approval knowledge proposal. It cannot commit or push.
3. **Grows its knowledge safely.** An allowlisted, clean-room, citation-required knowledge-growth
   path that always requires human approval.
4. **Uses local models when present and degrades when absent.** GPU-aware orchestration with a
   run-queue, token budgets, and bounded retries; everything still works MSSQL-only with no GPU,
   no Ollama, no Python, no internet.

## What the V2 product is NOT

- It is **not** an unattended autonomous agent. The plan→patch→verify loop is a safe validated
  core; a human approves before anything reaches the main repo.
- It is **not** dependent on a live model: the loop is tested with a fake planner.
- It is **not** a complete Python ML stack: the Python side is a stdlib skeleton.

## Scope delivered

| Capability | Project | State |
| --- | --- | --- |
| Code building blocks + composer + extractor | `LocalAIFactory.CodeBlocks` | 16 blocks, 24 tests |
| Model-driven plan→patch→verify | `LocalAIFactory.Reasoning/AgentRunner` | fake-planner tested, 10/10 |
| GPU-aware orchestration | `LocalAIFactory.Reasoning/LocalModels` | tested without GPU/Ollama |
| Safe Python bridge | `LocalAIFactory.PythonBridge` | 9 entrypoints, 9 tests |
| Knowledge-growth scraper | `LocalAIFactory.KnowledgeGrowth` | 13 tests, offline |
| UI/API V2 | `LocalAIFactory.Web` | 5 endpoints + page, 6 tests |
| Knowledge | 6 packs | 102 items |

## Targets — met and not met (honest scorecard)

| Target | Goal | Actual | Met? |
| --- | --- | --- | --- |
| Reasoning-family tests | 220+ | 176 | NO |
| Factory test total | 300+ | 257 | NO |
| Knowledge packs | 45+ | 45 | YES |
| Knowledge items | 1200+ | 1195 | NOT QUITE |
| Composition benchmark | 20 | 20/20 | YES |
| Safe-patch benchmark | 10 | 10/10 | YES |
| Product maturity | 75 (stretch) | 73.6 (70.6 → 73.6) | NO (stretch) |

## Honest limitations / not met

- **Reasoning 220+ and factory 300+ were not met** (176 / 257). Do not report these as met.
- **Knowledge items 1200+ was not met** (1195) — close, but not reached.
- **The 75 product-maturity stretch was not met** (73.6).
- LearningLoop (Phase 7) is composed from existing pieces, **not a standalone module** this sprint.
- The model-driven loop is **fake-planner tested**, not live-model tested.
- The Python workers are a **stdlib skeleton**; full ML/scrape workers need a local venv.
- The stretch classification `LAF_SOFTWARE_DEVELOPMENT_REASONING_AGENT_PILOT_READY` is **not**
  reached.
