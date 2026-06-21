# LAF CodeGraph V1 — Report

Component: `src/LocalAIFactory.Reasoning/CodeGraph`
Tests for this component: **~22 PASS** (part of the 113 reasoning-engine tests).

## What was built

An in-memory, queryable code graph built deterministically from the existing syntax-only Roslyn
extractor (`CSharpSymbolExtractor`, KE-008/KE-010). No DB, no external service.

- **`CodeNode`** — file/type/member with a stable `Id` (`{kind}:{fullName}[:{signature}]`),
  `FilePath`, line span, visibility, and inferred semantic **roles**: `controller`, `service`,
  `dbcontext`, `entity`, `test` (plus `apiroute`).
- **`CodeEdge`** — typed relationships. V1 uses `Contains`, `References`, `Inherits`, `Implements`,
  `UsesEntity`, `UsesDbSet`, and `TestCovers` (the `CodeEdgeKind` enum declares a wider superset).
- **`CodeGraphModel`** — indexes nodes by id/name and edges by source/target; provides `FindByName`,
  `Search`, `WithRole`, `OutgoingFrom`/`IncomingTo`/`ReferencersOf`, and `ImpactOf(id, maxDepth)` —
  the **transitive impact set** that follows dependency edges and skips structural `Contains`.
- **`CodeGraphBuilder`** — two-pass build (nodes + containment, then resolve references to typed
  edges). Out-of-corpus base types/interfaces (e.g. `DbContext`, `EntityBase`) are preserved as
  **external nodes** (`ext:{name}`) so role inference and inheritance edges survive. `dbcontext` and
  `entity` roles are promoted from inheritance edges. Idempotent node insertion; edges never dangle.

## What it proves

- The deterministic extractor can be lifted into a queryable graph that supports symbol lookup, role
  queries, neighbourhood traversal, and transitive blast-radius — with **no model and no DB**.
- On the real repo (Phase-9 benchmark) the builder produced **1308 nodes and 1699 edges** and
  correctly enumerated controllers, services, and the DbContext, and computed non-trivial impact sets
  (e.g. `StockLedgerEntry` → 8 referencers).

## Honest limitations / not met

- **Target not met:** the ambitious target of **50+** CodeGraph tests was not reached (~22 delivered).
- **Syntax-only.** No full semantic/Roslyn-compilation type resolution; references resolve by simple
  name, and the first matching type wins.
- **Unresolved references are dropped** unless they are base-type/interface relationships (which
  become external nodes). Cross-corpus method-call resolution is not modelled.
- Impact is a structural over-approximation bounded by `maxDepth`, not a precise data/control-flow
  analysis.
