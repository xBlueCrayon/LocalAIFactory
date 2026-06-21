# LAF Experience Memory V1 — Report

Component: `src/LocalAIFactory.Reasoning/Experience`
Tests for this component: **~16 PASS** (part of the 113 reasoning-engine tests).

## What was built

`ExperienceMemory` (`IExperienceMemory`) — an in-memory store of recorded engineering experiences.
Each `ExperienceEntry` captures a **symptom, root cause, fix, and reusable lesson**, plus affected
files, tests added, confidence, and links to knowledge and code nodes. No DB is required to function
or to test.

Ten **experience types**: `BuildFailure`, `TestFailure`, `PlaywrightFailure`, `SecurityFinding`,
`BugFix`, `GeneratorImprovement`, `KnowledgeImprovement`, `DeploymentIssue`, `RuntimeError`,
`RegressionPrevented`.

Operations:

- `Add`, `All`, `OfType`.
- **`FindSimilar`** — deterministic keyword similarity over title/symptoms/root-cause/lesson.
- **`PromoteToKnowledge`** — **idempotent**: a second promotion of the same entry is a no-op and
  returns `false`, and the promotion records the related knowledge uid.
- **`LinkCodeNode`** — link an experience to a code-graph node (de-duplicated).
- **`ToJson` / `FromJson`** — JSON round-trip persistence; a corrupt store deserialises to empty
  memory rather than throwing.

## What it proves

- The engine can accumulate and recall prior fixes deterministically, feeding
  `FindPriorSimilarFix` in retrieval ("have we fixed something like this before?").
- The promote-to-knowledge path is **idempotent**, so promoting the same experience twice cannot
  create duplicate knowledge — verified by tests.
- Persistence is safe: round-trip is lossless and corrupt input degrades gracefully.

## Honest limitations / not met

- **Target not met:** the ambitious target of **40+** experience tests was not reached (~16 delivered).
- **In-memory + JSON only.** There is no database-backed experience store in V1; persistence is a
  JSON round-trip, and the live engine's memory is process-scoped unless explicitly serialised.
- **Keyword similarity, not semantic.** `FindSimilar` is term-overlap, so it can miss paraphrased
  symptoms.
- Promotion records the link but does not itself write a knowledge-pack file; turning an experience
  into an approved pack item is a separate, governed step.
