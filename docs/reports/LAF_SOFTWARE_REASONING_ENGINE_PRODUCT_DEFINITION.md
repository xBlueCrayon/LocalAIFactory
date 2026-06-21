# LAF Software Reasoning Engine — Product Definition (V1)

Classification target: **`LAF_SOFTWARE_REASONING_ENGINE_PILOT_READY`** — achieved.

## What it is

The LAF Software Reasoning Engine is a **local-first software-engineering reasoning platform** built
into LocalAIFactory. It turns a C#/.NET codebase plus its approved knowledge packs and recorded
experience into a queryable model that answers concrete engineering questions — **deterministically,
with no network, no GPU, no Ollama, and no Qdrant required**. A local model is optional and only
enriches explanations; the core answers stand without it.

It is built on the existing deterministic Roslyn symbol foundation
(`src/LocalAIFactory.Ingestion/Symbols`, KE-008/KE-010) and ships as
`src/LocalAIFactory.Reasoning` with tests in `tests/LocalAIFactory.Reasoning.Tests`.

## The questions it answers

| Question | Backed by |
| --- | --- |
| **What touches this table / entity?** | `FindImpact` + `UsesEntity`/`UsesDbSet` edges |
| **What tests protect this service?** | `FindTestsForChange` (`TestCovers` + impact-reaching tests) |
| **What breaks if X changes?** (blast radius) | `FindImpact` transitive impact set |
| **What is the safest fix?** | `SafeToolGateway` default-deny classification + prior-fix experience |
| **What approved knowledge applies to this task?** | `FindKnowledgeForTask` over the knowledge index |
| **Have we fixed something like this before?** | `FindPriorSimilarFix` over experience memory |
| **Which template produced this generated file?** | `FindGeneratorTemplateForFile` |
| **What controllers / services / DbContext exist?** | role queries (`controller`/`service`/`dbcontext`) |

A single call, `BuildReasoningContext(task)`, assembles symbols + impact + tests + knowledge + prior
fixes into one bundle for a task.

## How it stays safe

- Every tool/command is classified **default-deny** into ReadOnly / SafeValidation / ControlledWrite
  / Forbidden; unknown commands are Forbidden.
- Writes are allowed **only inside an isolated worktree**; absolute paths and `..` traversal are
  blocked; every decision is logged.
- The agent runner validates write paths first, snapshots, applies in a worktree, builds + tests,
  **rolls back on failure**, and **cannot commit or push**.

## Local-first guarantees

- Deterministic core: graph, retrieval, safety, experience — **no external service**.
- Optional model via `LocalModelRouter`; `ReviewAsync` **never throws** when Ollama is absent.
- Web integration via a lazy, cached `ReasoningGraphProvider` that degrades to an empty graph and
  **never blocks startup or page load**.

## Evidence

- **124 tests pass without Ollama** (113 reasoning engine + 11 UI/API). Main app build: 0 errors.
- Real-repo benchmark: **1308 nodes, 1699 edges, 973 knowledge items; 14 of 15 tasks = 93.3% with no
  model** (`benchmarks/results/laf-software-reasoning-benchmark.json`;
  tasks in `benchmarks/software-reasoning/tasks.json`).

## Why PILOT_READY (and not GA)

The engine is a cohesive, tested, local-first product that answers the target questions on a real
banking-style ERP corpus with no model. It is classified **PILOT_READY** rather than GA because:

- the benchmark answers 14 of 15 tasks (one test-coverage question unanswered);
- per-component test targets were not individually met (delivered at lower counts — see below);
- retrieval is keyword-based, not semantic;
- the model-enriched path is optional and not validated end-to-end against a live Ollama this sprint.

## Honest limitations / not met

- **Not GA.** One benchmark task unanswered (`What tests protect UserAuthService?`); 93.3%, not 100%.
- **Per-component test targets not met.** CodeGraph 50+, Retrieval 50+, ModelRouter 30+, Safety 60+,
  AgentRunner 50+, Experience 40+, UI/API 40+ were **not** reached. Delivered: 124 tests total.
- **Keyword retrieval, not embeddings.** No vector ranking in the deterministic core.
- **Syntax-only graph.** No full semantic type resolution; name-based reference resolution.
- **Model path optional and not e2e-validated** against a live model this sprint.
- **AgentRunner cannot commit/push by design**, and was validated via an abstracted executor rather
  than real `dotnet` over a large repo in CI.
