# LAF Ollama Model Router V1 — Report

Component: `src/LocalAIFactory.Reasoning/LocalModels`
Tests for this component: **~11 PASS** (part of the 113 reasoning-engine tests).

## What was built

`LocalModelRouter` routes review/reasoning requests to a local model **by role**, with a default
role roster and graceful degradation when Ollama is absent.

Default roster:

| Role | Model |
| --- | --- |
| `CodeReviewer` | `qwen2.5-coder:14b` |
| `Planner` | `deepseek-r1:14b` |
| `AgentReviewer` | `deepseek-r1:14b` |
| `Embedding` | `nomic-embed-text` |

- `IsHealthyAsync` — health probe.
- `AvailableAsync` — returns which roster models are actually installed (empty when Ollama is absent).
- `SelectModel(role)` — role → model lookup.
- `ReviewAsync(role, prompt, …)` — **degrades gracefully and NEVER throws**; on absence/timeout/
  failure it returns an unavailable `ModelResponse` so the caller falls back to deterministic
  reasoning. Honours a token budget and timeout, and logs every call.
- The backend is abstracted behind the injectable **`ILocalModelClient`**; **`NullModelClient`** is
  the default offline fallback (every call reports unavailable).

## What it proves

- The engine can use a local model **when present** and **keep working without one** — the optional-
  model contract of LocalAIFactory holds. All 124 tests pass with **no Ollama**.
- Role-based routing, health probing, availability, and graceful-degradation behaviour are unit-tested
  via the injectable client without requiring a live model.

## Honest limitations / not met

- **Target not met:** the ambitious target of **30+** router tests was not reached (~11 delivered).
- **Optional path not validated end-to-end.** Behaviour was tested against `NullModelClient` / a stub
  client, not against a live Ollama serving `qwen2.5-coder:14b` / `deepseek-r1:14b` this sprint.
- The roster is a static default mapping; there is no automatic model discovery, capability probing,
  or quality scoring of model outputs.
- No streaming or multi-turn orchestration; `ReviewAsync` is a single bounded call.
