# LAF GraphRAG (Retrieval) V1 — Report

Component: `src/LocalAIFactory.Reasoning/Retrieval`
Tests for this component: **~13 PASS** (part of the 113 reasoning-engine tests).

## What was built

`SoftwareReasoningService` (`ISoftwareReasoningService`) — the deterministic retrieval/reasoning
surface that combines the code graph, a knowledge index, and experience memory. No model and no
external service are required; an optional model only enriches explanations.

Retrieval operations:

| Method | Purpose |
| --- | --- |
| `FindSymbol(name)` | Exact-name then substring symbol lookup |
| `FindImpact(name, maxDepth=3)` | Transitive blast radius from the seed |
| `FindTestsForChange(name)` | Tests that cover or transitively reach the seed |
| `FindKnowledgeForTask(task, top)` | Top knowledge-pack items by keyword overlap |
| `FindGeneratorTemplateForFile(path)` | Map a generated file to its template source |
| `FindPriorSimilarFix(symptom, top)` | Similar prior experiences |
| `BuildReasoningContext(task)` | One bundle: symbols + impact + tests + knowledge + prior fixes |

`KnowledgeIndex` is a **deterministic keyword index** over installed knowledge-pack category JSON
(`LoadFromPacks`), scoring items by matched-term count. No embeddings, no Qdrant. It is resilient to
malformed pack files (skips them). `FindGeneratorTemplateForFile` is a deterministic path mapping from
`.../src/...` to `tools/LocalAIFactory.Generator/templates/erp-core/...`.

`BuildReasoningContext` extracts capitalised candidate symbol tokens from a task string and seeds the
graph queries, then caps impact (50) and tests (30) for a bounded bundle.

## What it proves

- A code graph plus a keyword knowledge index plus experience memory can answer real engineering
  questions **deterministically, with no model**.
- On the Phase-9 benchmark over **973 knowledge items**, retrieval answered task-knowledge questions
  (manufacturing depth, external blockers, stock-test symptom), located services/controllers/the
  DbContext, mapped a generated controller to its template, and built a non-empty reasoning context
  for an auth task (2 symbols, 26 impact, 10 knowledge).

## Honest limitations / not met

- **Target not met:** the ambitious target of **50+** retrieval tests was not reached (~13 delivered).
- **Keyword retrieval, not semantic.** Scoring is term-overlap; there is no embedding/vector ranking
  and no synonym handling. A question phrased without matching tokens can miss.
- **Symbol seeding is heuristic.** `BuildReasoningContext` keys off capitalised tokens, so tasks that
  reference symbols in lower case or by description may not seed the graph.
- One benchmark task (`What tests protect UserAuthService?`) was **not answered**, exposing a gap in
  test-coverage retrieval for that symbol.
