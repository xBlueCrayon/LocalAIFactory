# LAF Software Reasoning Engine V2 — Architecture

**Stamp:** 2026-06-21
**Baseline:** commit `5e70877` (V1)
**Classification reached:** `LAF_SOFTWARE_REASONING_ENGINE_V2_LOCAL_CORE_READY`

This document describes the architecture added in the Software Reasoning Engine V2 sprint. It is
factual: every component below exists in the repository, and every honest gap is stated in the
limitations section.

## 1. New projects

Three product projects and their test projects were added to `LocalAIFactory.sln`. The full
solution builds with **0 errors**.

| Project | Purpose |
| --- | --- |
| `LocalAIFactory.CodeBlocks` | Reusable code building blocks ("bricks") + deterministic composer + extractor |
| `LocalAIFactory.PythonBridge` | Safe, allowlisted Python worker runner (JSON I/O, timeout, no arbitrary scripts) |
| `LocalAIFactory.KnowledgeGrowth` | Offline, clean-room knowledge-growth scraper with allowlist + citations |

Additional V2 work lives in the existing `LocalAIFactory.Reasoning` project
(`AgentRunner/ModelDrivenPlanPatchVerifyRunner`, `LocalModels/GpuAwareOrchestration`) and in
`LocalAIFactory.Web` (`Controllers/ReasoningV2Controller`, `Views/ReasoningV2/Blocks.cshtml`).

## 2. Code building blocks (the "2 + 2" proof)

`LocalAIFactory.CodeBlocks` models an engineering pattern as a `CodeBuildingBlock`: `BlockId`,
`Name`, `Purpose`, `ProblemSolved`, `RequiredInputs`, `GeneratedFiles`, `CodePatternSummary`,
`Dependencies`, `SecurityRules`, `ValidationRules`, `TestPattern`, `PlaywrightPattern`,
`FailureModes`, links to knowledge / generator templates / experiences, `Keywords`, and
`Confidence`.

- **`CodeBlockCatalog`** seeds **16 real blocks**: password-hashing, audit-event, anti-forgery,
  secure-login, login-lockout, maker-checker, ef-migration, crud-module, report-endpoint,
  stock-movement, accounting-posting, document-lifecycle, manufacturing-order, import-export,
  playwright-proof, production-smoke. Matching is deterministic keyword scoring.
- **`BlockComposer`** matches blocks to a requirement, pulls in **transitive dependencies**, and
  aggregates files, tests, Playwright proofs, security rules, migration impact, and knowledge ids.
  It **honestly flags uncovered capabilities as missing bricks** and lowers confidence accordingly
  (honesty penalty of up to 70%).
- **`BlockExtractor`** detects which blocks are already present in a given file set.

24 tests pass. No model is required — composition is fully deterministic.

## 3. Model-driven plan → patch → verify

`LocalAIFactory.Reasoning/AgentRunner/ModelDrivenPlanPatchVerifyRunner`:

1. An `IPatchPlanner` (a model or a deterministic stub) **proposes** a `PatchPlan`. Planners must
   not execute anything.
2. `AssessRisk` checks every target path is inside the worktree sandbox, blocks a forbidden command
   smuggled into a `.ps1` patch, and flags an empty plan. Sandbox-escape or forbidden-command →
   `Blocked`.
3. The plan is applied **only** in an isolated worktree via the V1 `IsolatedPatchRunner` (build/test
   gated), **rolls back on failure**, records experience, and emits a `KnowledgeProposal` with
   `Approved=false`.

There is **no commit/push capability** and the main repo is never edited. Covered scenarios: safe
accepted; build-fail rejected+rollback; test-fail rejected+rollback; hallucinated path blocked
before any write; forbidden-in-script blocked; empty plan flagged. Tested with a **fake planner**.

## 4. GPU-aware orchestration

`LocalAIFactory.Reasoning/LocalModels/GpuAwareOrchestration`:

- **`GpuCapabilityDetector`** reads CUDA / HIP / Ollama-GPU env signals with no hard dependency;
  an absent signal means "assume CPU".
- **`GpuAwareOrchestrator`** serialises heavy model calls through a `SemaphoreSlim(1,1)` run-queue,
  enforces a per-run token budget and timeout, splits an over-large prompt and **retries SMALLER**
  on failure with **bounded** retries (never infinite), records telemetry, and degrades gracefully
  to "unavailable" when no Ollama/GPU is present.

Tested without a GPU or Ollama. Core behaviour requires no GPU.

## 5. Python bridge

`LocalAIFactory.PythonBridge/SafePythonWorkerRunner` runs **only 9 approved entrypoints**:
code-mine, pattern-mine, doc-extract, web-scrape, embed-text, rerank, build-dataset, graph-enrich,
extract-knowledge. JSON in/out over stdin/stdout, hard timeout + kill, fixed working dir, **no
arbitrary scripts**. `IsAvailable` probes `python --version` and reports false gracefully when
Python is absent — every run then returns `Available=false` and never throws.

The Python skeleton in `tools/python/laf_python_worker` (main.py dispatcher, safety.py allowlist,
requirements.txt, README) is **stdlib-only** and runs when Python is present. 9 bridge tests pass
**without Python installed**.

## 6. Knowledge-growth scraper

`LocalAIFactory.KnowledgeGrowth`:

- **`ScrapeAllowlist`** is https-only, default-deny: learn.microsoft.com, docs.python.org,
  docs.ollama.com, modelcontextprotocol.io, plus official GitHub docs.
- **`KnowledgeGrowthService.Ingest(FetchedDocument)`** checks the allowlist, caches + dedups by
  content hash, **clean-room summarises** (facts capped at 300 chars × 20, never vendoring raw
  HTML), requires `CitationMetadata` (url + title + fetchDate + sourceHash), and emits a
  `GrowthProposal` with `Approved=false`.

13 tests pass, fully offline. The caller supplies fetched documents; the actual network fetch is
the allowlisted Python worker's job.

## 7. UI / API V2

`LocalAIFactory.Web/Controllers/ReasoningV2Controller` + `Views/ReasoningV2/Blocks.cshtml`:

- `/Reasoning/Blocks` page
- `GET /api/reasoning/blocks/search`
- `POST /api/reasoning/blocks/compose`
- `POST /api/reasoning/agent/plan` (compose-only, `executed=false`)
- `GET /api/reasoning/python/status`

6 controller tests in the factory project. The surface is deterministic and dependency-light — no
Ollama, no Python, no network required to respond.

## 8. Knowledge

Six new V2 packs (102 items): laf-high-memory-software-definitions-v1, laf-code-building-blocks-v1,
laf-gradual-self-improvement-v1, laf-python-worker-patterns-v1, laf-web-scraper-knowledge-growth-v1,
laf-local-gpu-model-usage-v1. Verify PASS: **45 packs / 1195 items**.

## 9. Test totals (honest)

| Suite | Tests |
| --- | --- |
| Reasoning | 130 |
| CodeBlocks | 24 |
| PythonBridge | 9 |
| KnowledgeGrowth | 13 |
| **Reasoning family** | **176** |
| Factory total | 257 |

## Honest limitations / not met

- **LearningLoop (Phase 7) was NOT built as a standalone project this sprint.** Its flow is
  *composed* from existing pieces (the model-driven runner already proposes knowledge + records
  experience; require-approval discipline holds). See the gradual-self-improvement report.
- The model-driven loop is tested with a **fake planner, not a live model**.
- The Python ML/scrape workers are a **stdlib skeleton**; full workers need a local venv.
- **Targets not met:** reasoning 220+ (actual 176); factory 300+ (actual 257); items 1200+ (actual
  1195). Targets met: packs 45+ (45), composition 20/20, safe-patch 10/10. Product maturity moved
  70.6 → 73.6 (the 75 stretch was **not** met).
- Classification reached is `LAF_SOFTWARE_REASONING_ENGINE_V2_LOCAL_CORE_READY`; the stretch
  `LAF_SOFTWARE_DEVELOPMENT_REASONING_AGENT_PILOT_READY` is **not** reached — this is a safe
  validated core, not an unattended autonomous agent.
