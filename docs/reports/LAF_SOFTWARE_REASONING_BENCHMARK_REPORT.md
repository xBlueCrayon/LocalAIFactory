# LAF Software Reasoning Benchmark — Report (Phase 9)

Result file: `benchmarks/results/laf-software-reasoning-benchmark.json`
Task suite: `benchmarks/software-reasoning/tasks.json`
Driver: `tests/LocalAIFactory.Reasoning.Tests/BenchmarkTests.cs` (1 test, part of the 113 engine tests).

## What the benchmark does

`BenchmarkTests` builds the code graph over the **real repository** —
`generated-products/LAF-EnterpriseERP-Gold/src` plus `src/LocalAIFactory.Reasoning` and
`Ingestion/Symbols` — loads the knowledge index, and answers a 15-task suite of concrete software-
engineering questions. It runs **with no model** (deterministic core only).

## Results

| Metric | Value |
| --- | --- |
| Graph nodes | **1308** |
| Graph edges | **1699** |
| Knowledge items | **973** |
| Tasks | **15** |
| Answered | **14** |
| Score | **93.3%** |
| Model required | **No** |

Answered tasks include: what code touches `AppUser`; impact of `StockLedgerEntry` (8 referencers);
which template produced `CatalogController`; knowledge for manufacturing depth; enumerate controllers
(3), services (15), and the DbContext (2); why the LocalDB migration failed; building a reasoning
context for an auth task (2 symbols / 26 impact / 10 knowledge); entity count (74); locating
`ReportsService`; and knowledge for a stock-test symptom.

## What it proves

- On a realistic banking-style ERP corpus of **1308 nodes / 1699 edges / 973 knowledge items**, the
  deterministic engine answers **14 of 15** engineering questions **with no model and no external
  service** — covering symbol location, blast radius, role enumeration, template provenance,
  task-knowledge retrieval, and reasoning-context assembly.

## Honest limitations / not met

- **Not 100%.** **1 of 15** tasks is unanswered: *"What tests protect UserAuthService?"* — a gap in
  test-coverage retrieval for that symbol. Score is **93.3%**, not 100%.
- A single, repo-specific benchmark; results depend on the current state of the Gold ERP product and
  the installed knowledge packs and are not a general-purpose accuracy guarantee.
- "Answered" means the engine returned a non-empty, on-topic result (deterministically), not that the
  answer was independently graded for completeness against ground truth for every task.
