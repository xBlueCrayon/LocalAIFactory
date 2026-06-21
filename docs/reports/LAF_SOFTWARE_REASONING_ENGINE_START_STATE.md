# LAF Software Reasoning Engine — Start State

**Date:** 2026-06-21 · **Branch:** ke-008-code-symbols · **Starting commit:** `df8a964`

## Gates at start

| Gate | Result |
|------|--------|
| `dotnet build LocalAIFactory.sln -c Release` | 0 errors |
| `LocalAIFactory.Tests` | 240 / 240 pass |
| `verify-all-knowledge-packs.ps1` | PASS — 31 packs / 973 items |
| `verify-production-readiness-v3.ps1` | `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` |
| `security-audit.ps1` | PASS — 0 HIGH |

## Why this sprint

ERP Gold Depth proved the next bottleneck is **software-engineering reasoning**, not more generated
modules. ERP parity sits at ~45% with the remaining gaps being breadth + external gates. So this sprint
pivots to making **LocalAIFactory itself** the product it is aimed to be: a local-first AI software
engineering platform that understands codebases structurally and reasons + acts safely over them.

## Foundation already present (the branch's `Symbols` work)

The branch `ke-008-code-symbols` already carries a mature, deterministic **Roslyn symbol foundation**
(`src/LocalAIFactory.Ingestion/Symbols`): `CSharpSymbolExtractor` (syntax-only, error-tolerant),
`CodeSymbolStore` (convergent EF upsert), SQL/Python extractors, and `CodeSymbol`/`CodeSymbolReference`
entities. **This sprint builds the reasoning layer on top of that foundation rather than re-implementing it.**

## Targets (honest)

- Classification target: `LAF_SOFTWARE_REASONING_ENGINE_PILOT_READY`.
- Components: CodeGraph, GraphRAG retrieval, Ollama model router, safe tool gateway, isolated agent runner,
  experience memory, knowledge harvest, reasoning benchmark, UI/API.
- The per-component test targets (50–60+ each) are ambitious for one sprint; this start-state explicitly
  flags that a cohesive, genuinely-tested engine is the goal, and any shortfall against those counts will be
  reported honestly rather than inflated.
