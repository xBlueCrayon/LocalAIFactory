# LAF Software Reasoning Engine — Architecture (V1)

Status: V1 delivered. Classification: `LAF_SOFTWARE_REASONING_ENGINE_PILOT_READY`.

This document describes the architecture of the LAF Software Reasoning Engine, a local-first
software-engineering reasoning layer added to LocalAIFactory. It explains how the six components fit
together, what they are built on, and where the boundaries (and limits) are. Every claim here is
backed by code under `src/LocalAIFactory.Reasoning` and tests under
`tests/LocalAIFactory.Reasoning.Tests`.

---

## 1. Position in the solution

The engine is delivered as a new project, `src/LocalAIFactory.Reasoning`, added to
`LocalAIFactory.sln`, with a dedicated test project `tests/LocalAIFactory.Reasoning.Tests`. It is
**built on the existing Roslyn symbol foundation** — the deterministic, syntax-only
`CSharpSymbolExtractor` in `src/LocalAIFactory.Ingestion/Symbols` (delivered in KE-008/KE-010). The
reasoning engine adds a graph, retrieval, safety, experience, model-routing, and an isolated patch
runner on top of that extractor; it does not re-implement parsing.

Design principles, consistent with the LocalAIFactory non-negotiables:

- **Local-first and deterministic core.** The graph, retrieval, safety classification, and
  experience memory require no network, no GPU, no Ollama, and no Qdrant.
- **Optional model.** A local model (Ollama) can enrich explanations and reviews, but every code
  path degrades gracefully when it is absent. All tests pass without Ollama.
- **No request-path blocking.** Web integration uses a lazily-built, cached graph provider that
  degrades to an empty graph when source is absent and never blocks startup or page load.

---

## 2. Component map

```
CSharpSymbolExtractor (Ingestion/Symbols, KE-008/KE-010)  ← deterministic, syntax-only
        │
        ▼
CodeGraphBuilder ──► CodeGraphModel (CodeNode + CodeEdge)         [CodeGraph]
        │
        ▼
SoftwareReasoningService  +  KnowledgeIndex                       [Retrieval]
        │                         (deterministic keyword index over pack JSON)
        ├──────────────► ExperienceMemory                         [Experience]
        │
        ├──────────────► LocalModelRouter (optional, role roster) [LocalModels]
        │
        └──────────────► SafeToolGateway (default-deny classifier)[Safety]
                                  │
                                  ▼
                         IsolatedPatchRunner                       [AgentRunner]
                         (worktree-only, build+test, rollback, NO commit/push)

Web: ReasoningController + Views/Reasoning/Index.cshtml + ReasoningGraphProvider (singleton, lazy)
```

---

## 3. CodeGraph

`src/LocalAIFactory.Reasoning/CodeGraph/`

An in-memory, queryable code graph built deterministically from the syntax-only extractor — no DB,
no external services.

- `CodeNode` — a file, type, or member, with stable `Id` (`{kind}:{fullName}[:{signature}]`),
  `FilePath`, line span, visibility, and a set of inferred semantic **roles**:
  `controller`, `service`, `dbcontext`, `entity`, `test` (also `apiroute`, `view`).
- `CodeEdge` — a typed relationship. Edge kinds used in V1 include `Contains`, `References`,
  `Inherits`, `Implements`, `UsesEntity`, `UsesDbSet`, and `TestCovers`
  (the `CodeEdgeKind` enum declares a wider superset).
- `CodeGraphModel` — indexes nodes by id and name and edges by source and target, giving:
  - `FindByName` / `Search` (exact and substring symbol lookup),
  - `WithRole` (all controllers, all services, …),
  - `OutgoingFrom` / `IncomingTo` / `ReferencersOf`,
  - `ImpactOf(id, maxDepth)` — the **transitive impact set**: every node that transitively references
    a seed, following dependency edges and deliberately skipping structural `Contains` edges.
- `CodeGraphBuilder` — two-pass build over `CSharpSymbolExtractor` output: emit nodes and containment
  edges, then resolve references into typed edges. Inheritance/implementation to **out-of-corpus base
  types** (e.g. `DbContext`, `EntityBase`) is preserved by creating an **external node**
  (`ext:{name}`) so role inference and edges survive. `dbcontext`/`entity` roles are promoted from
  inheritance edges.

The graph never dangles (edges referencing unknown nodes are dropped) and node insertion is
idempotent on `Id`.

---

## 4. Retrieval

`src/LocalAIFactory.Reasoning/Retrieval/`

`SoftwareReasoningService` (`ISoftwareReasoningService`) is the deterministic question-answering
surface over the graph, the knowledge index, and experience memory. No model is required; an optional
model only enriches explanations. It exposes:

| Method | Answers |
| --- | --- |
| `FindSymbol(name)` | Where is this symbol? |
| `FindImpact(name, maxDepth=3)` | What is the blast radius if this changes? |
| `FindTestsForChange(name)` | What tests protect this symbol? |
| `FindKnowledgeForTask(task, top)` | What approved knowledge applies to this task? |
| `FindGeneratorTemplateForFile(path)` | Which template produced this generated file? |
| `FindPriorSimilarFix(symptom, top)` | Have we fixed something like this before? |
| `BuildReasoningContext(task)` | One bundle: symbols + impact + tests + knowledge + prior fixes. |

`KnowledgeIndex` is a **deterministic keyword index** over installed knowledge-pack category JSON
(`LoadFromPacks`). It scores items by the count of matched query terms — no embeddings, no Qdrant —
and is resilient to malformed pack files.

`FindGeneratorTemplateForFile` is a deterministic path mapping: it maps a generated product file under
`.../src/...` to its template source under `tools/LocalAIFactory.Generator/templates/erp-core/...`.

---

## 5. Safety

`src/LocalAIFactory.Reasoning/Safety/`

`SafeToolGateway` gates every tool call. No model or agent bypasses it.

- `CommandRiskClassifier` — **default-deny**, four tiers:
  - `ReadOnly` (e.g. `git status`, `ls`, `findstr`),
  - `SafeValidation` (e.g. `dotnet build`, `dotnet test`, security/readiness scans),
  - `ControlledWrite` (e.g. `apply-patch`, `git apply`, `write-file`),
  - `Forbidden` (e.g. `git push`, `git commit`, `git reset --hard`, `rm -rf`, network fetches,
    package installs). **Forbidden wins over everything**, and any command not on an allowlist is
    treated as Forbidden.
- `PathSandboxGuard` — blocks absolute paths and `..` traversal that escape a single worktree root.
- Controlled writes require an isolated worktree; without one they are not allowed.
- **Every decision is logged** (`SafeExecutionLogEntry`).

---

## 6. Experience

`src/LocalAIFactory.Reasoning/Experience/`

`ExperienceMemory` (`IExperienceMemory`) is an in-memory store of recorded engineering experiences —
symptom, root cause, fix, reusable lesson — across **10 experience types** (`BuildFailure`,
`TestFailure`, `PlaywrightFailure`, `SecurityFinding`, `BugFix`, `GeneratorImprovement`,
`KnowledgeImprovement`, `DeploymentIssue`, `RuntimeError`, `RegressionPrevented`). It supports:

- `Add`, `OfType`, `All`,
- `FindSimilar` — deterministic keyword similarity over title/symptoms/root-cause/lesson,
- `PromoteToKnowledge` — **idempotent**; a second promotion is a no-op,
- `LinkCodeNode` — link an experience to a graph node,
- `ToJson` / `FromJson` — JSON round-trip persistence (corrupt store → empty memory). No DB required.

---

## 7. LocalModels (optional)

`src/LocalAIFactory.Reasoning/LocalModels/`

`LocalModelRouter` routes review/reasoning requests to a local model **by role**, with a default
roster:

| Role | Model |
| --- | --- |
| `CodeReviewer` | `qwen2.5-coder:14b` |
| `Planner` | `deepseek-r1:14b` |
| `AgentReviewer` | `deepseek-r1:14b` |
| `Embedding` | `nomic-embed-text` |

It health-probes via `IsHealthyAsync`, reports installed roster models via `AvailableAsync`, and
`ReviewAsync` **degrades gracefully and never throws** when Ollama is absent. The model backend is
abstracted behind the injectable `ILocalModelClient`; `NullModelClient` is the default offline
fallback (every call reports unavailable). Ollama is optional throughout.

---

## 8. AgentRunner

`src/LocalAIFactory.Reasoning/AgentRunner/`

`IsolatedPatchRunner` is the safe patch loop. Given proposed file writes it:

1. **Validates every write path through the `SafeToolGateway` BEFORE touching disk**,
2. **snapshots originals** for rollback,
3. applies the patch **inside the worktree only**,
4. runs **build + test** via the injected `IValidationExecutor`,
5. **rolls back on any failure**,
6. records the outcome as **experience** (success → `RegressionPrevented`; failure → `BuildFailure`
   / `TestFailure`).

It has **no capability to commit, push, or write outside the worktree**. Validation is abstracted via
`IValidationExecutor`, so the runner is unit-testable without invoking `dotnet`.

---

## 9. Web integration

- `ReasoningController` + `Views/Reasoning/Index.cshtml`.
- A **singleton `ReasoningGraphProvider`**: lazy, cached, resolves the repo root by walking up for
  `LocalAIFactory.sln`, degrades to an **empty graph** when source is absent, and **never blocks
  startup or page load**.
- API surface (all deterministic, **no Ollama/Qdrant required**):
  - `GET /Reasoning`
  - `GET /api/reasoning/status`
  - `GET /api/reasoning/symbol-search`
  - `GET /api/reasoning/impact`
  - `GET /api/reasoning/tests-for-change`
  - `GET /api/reasoning/knowledge-for-task`
  - `GET /api/reasoning/experience/search`
  - `GET /api/reasoning/model-router/status`
  - `POST /api/reasoning/agent/run-dry` — **classification only, never executes**.

---

## 10. Test posture

- Reasoning engine tests: **113 PASS** (CodeGraph ~22, Retrieval ~13, Safety ~40 incl. Theory cases,
  Experience ~16, ModelRouter ~11, AgentRunner ~7, Benchmark 1).
- Reasoning UI/API tests in the factory web test project: **11 PASS** (factory total 240 → 251).
- **124 tests pass in total without Ollama.** Main app build: 0 errors.

---

## 11. Honest limitations / not met

- **Per-component test targets were not individually met.** The ambitious targets (CodeGraph 50+,
  Retrieval 50+, ModelRouter 30+, Safety 60+, AgentRunner 50+, Experience 40+, UI/API 40+) were
  **not** reached. What was delivered is a cohesive, tested engine at **lower counts** (113 engine +
  11 UI/API = 124). This document does not claim those targets.
- **Syntax-only graph.** The graph is built from the deterministic, syntax-only extractor. It does
  **not** perform full semantic/Roslyn-compilation type resolution; reference resolution is by simple
  name, and unresolved out-of-corpus references (other than base types/interfaces) are dropped rather
  than modelled.
- **Keyword retrieval, not semantic.** `KnowledgeIndex` and experience similarity use term-overlap
  scoring, not embeddings. There is no vector ranking in the deterministic core.
- **Benchmark coverage is partial.** The repo benchmark answers **14 of 15** tasks (93.3%); one task
  (`What tests protect UserAuthService?`) is unanswered. The engine is **pilot-ready, not GA**.
- **Model path is optional and largely untested end-to-end.** The router degrades gracefully and is
  unit-tested for absence; rich model-enriched reasoning has not been validated against a live Ollama
  in this sprint.
- **AgentRunner is deliberately limited.** It cannot commit or push by design. End-to-end runs were
  validated through the abstracted `IValidationExecutor`, not by driving real `dotnet` over a large
  repo in CI.
