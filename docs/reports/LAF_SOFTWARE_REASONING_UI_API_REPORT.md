# LAF Software Reasoning UI / API — Report

Component: `src/LocalAIFactory.Web` — `Controllers/ReasoningController.cs`, `Views/Reasoning/Index.cshtml`,
and the singleton `ReasoningGraphProvider`.
Tests for this component: **11 PASS** in the factory web test project (factory total **240 → 251**).

## What was built

A web surface over the reasoning engine. All endpoints are deterministic and degrade gracefully —
**no Ollama or Qdrant required**.

- **`ReasoningGraphProvider`** (singleton): lazy, cached; resolves the repo root by walking up for
  `LocalAIFactory.sln`; degrades to an **empty graph** when source is absent; **never blocks startup
  or page load**.
- **`ReasoningController`** endpoints:

| Endpoint | Purpose |
| --- | --- |
| `GET /Reasoning` | Index view with graph stats |
| `GET /api/reasoning/status` | nodes / edges / knowledge / empty |
| `GET /api/reasoning/symbol-search` | symbol lookup (top 50) |
| `GET /api/reasoning/impact` | blast radius (top 100) |
| `GET /api/reasoning/tests-for-change` | tests covering a symbol |
| `GET /api/reasoning/knowledge-for-task` | knowledge for a task |
| `GET /api/reasoning/experience/search` | similar prior fixes |
| `GET /api/reasoning/model-router/status` | roster + role mapping (no external call) |
| `POST /api/reasoning/agent/run-dry` | **classification only — never executes** |

The agent dry-run classifies the proposed command via `CommandRiskClassifier` and reports the risk and
whether it *would* run, with `executed: false`. `model-router/status` reports the static roster and
explicitly sets `ollamaRequired: false`; it makes no external call.

## What it proves

- The reasoning engine is reachable from the MVC app without violating the LocalAIFactory rule that
  pages must always load on an MSSQL-only deployment: the provider is lazy/cached and degrades to an
  empty graph, so `/Reasoning` and the APIs return without blocking.
- The agent surface exposed over HTTP is **safe**: the dry-run endpoint classifies but never executes.

## Honest limitations / not met

- **Target not met:** the ambitious target of **40+** UI/API tests was not reached — **11 delivered**
  (factory project moved 240 → 251).
- The dry-run endpoint is **classification only**; there is no HTTP path that actually applies a patch
  or runs the agent (that lives in `IsolatedPatchRunner` and is invoked outside the request path).
- The Index view is a functional console over the APIs, not a full graph-visualisation UI.
- API responses are not paginated beyond fixed `Take` caps (50/100), and there is no auth layer
  specific to these endpoints beyond the app's existing posture.
