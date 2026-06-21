# LAF Reasoning V2 — UI / API Report

**Stamp:** 2026-06-21
**Component:** `LocalAIFactory.Web/Controllers/ReasoningV2Controller` +
`LocalAIFactory.Web/Views/ReasoningV2/Blocks.cshtml`

## Purpose

Expose the V2 reasoning core through a thin, **deterministic, dependency-light** web surface that
responds without Ollama, Python, or network access.

## Surface

| Route | Method | Behaviour |
| --- | --- | --- |
| `/Reasoning/Blocks` | GET | Building-blocks page (shows block count) |
| `/api/reasoning/blocks/search` | GET | Search blocks by query; empty query lists all (id/name/purpose) |
| `/api/reasoning/blocks/compose` | POST | Compose a feature plan: blocks, missing bricks, files, tests, playwright, security/migration risks, knowledge, templates, confidence |
| `/api/reasoning/agent/plan` | POST | SAFE plan: would-edit files, would-add tests, missing bricks, risks, confidence — **`executed=false`** |
| `/api/reasoning/python/status` | GET | Python availability + the 9 approved entrypoints |

## Safety properties

- **Compose-only.** `/api/reasoning/agent/plan` returns a plan and explicitly carries
  `executed=false`; applying a patch requires the isolated worktree runner with build/test gates.
- **No external dependency on the request path.** The controller uses a static deterministic
  `CodeBlockCatalog`; it does not call Ollama, Python, or the network to respond, consistent with
  the project's "pages must always load" rule.

## Tests

| Metric | Value |
| --- | --- |
| Controller tests | 6 (in the factory project) |

## Honest limitations / not met

- The agent endpoint is **plan-only**: it never executes a patch. There is intentionally **no UI
  path to apply a change** — that is reserved for the gated isolated runner.
- `/api/reasoning/python/status` reports availability; with Python absent it reports
  `available=false`. The page therefore reflects a degraded-but-working state rather than a failure.
- The UI surfaces the deterministic composer; it does **not** expose a live model or the
  knowledge-growth fetch.
