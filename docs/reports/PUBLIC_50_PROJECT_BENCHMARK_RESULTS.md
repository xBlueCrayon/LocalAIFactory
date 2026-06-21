# Public 50-Project Benchmark — Results

**Date:** 2026-06-21 · Suite `public50` · **51 real public GitHub repos attempted** · total run ~14 min
(`scripts/benchmark/run-50-project-benchmark.ps1 -Manifest benchmarks/public-projects-50.json`)

A **breadth** benchmark over real public projects — honest about the large fraction LocalAIFactory does
**not** fully support. Scoring/methodology: `docs/Public-50-Project-Benchmark-Scoring.md`.

## Headline

| Metric | Value |
|---|---|
| **Repos attempted** | **51** |
| Cloned successfully | 39 |
| **Passed** (full extraction) | **22** |
| **PassedPartial** (capped) | **7** |
| ValidationOnly | 5 |
| UnsupportedLanguage (honest gap) | 5 |
| CloneFailed / TimedOut (xlarge over the time budget) | 12 |
| **Average score** | **60.8 / 100** (deliberately dragged down by unsupported + xlarge repos) |
| Supported files analyzed | **86,134** |
| **C# symbols** extracted | **123,849** |
| **Python symbols** extracted | **241,730** |
| SQL objects | **1,733** |
| LOC analyzed | **7,565,513** |
| Largest attempted | **odoo/odoo** (47,928 files, PassedPartial) |

## Distribution (meets the target spread)

- **Language:** Python 17 · C# 13 · TypeScript 8 · Java 5 · JavaScript 3 · Go 2 · SQL 2 · Clojure 1
  (≥10 C#, ≥8 Python, ≥8 TS/JS, ≥5 Java — all met).
- **Size tier:** tiny 3 · small 7 · medium 5 · large 21 · xlarge 15.

## Real extraction highlights (Passed / PassedPartial)

| Repo | Language | Status | Supported files | C# symbols | Py symbols |
|---|---|---|---:|---:|---:|
| dotnet/efcore | C# | PassedPartial | 5,761 | **43,340** | — |
| django/django | Python | Passed | 2,922 | — | **42,672** |
| pandas-dev/pandas | Python | Passed | 1,509 | — | 33,066 |
| saleor/saleor | Python | PassedPartial | 4,262 | — | 25,770 |
| PowerShell/PowerShell | C# | Passed | 1,338 | 23,218 | — |
| apache/superset | Python | Passed | 2,143 | — | 18,637 |
| apache/airflow | Python | PassedPartial | 7,454 | — | 17,300 |
| frappe/erpnext | Python | Passed | 2,763 | — | 14,439 |

## What this proves

LocalAIFactory's deterministic extractor runs against **real, large public codebases** (not just synthetic
fixtures) and produces **real symbol counts at scale** — ~124k C# symbols and ~242k Python symbols across
7.5M LOC — while **honestly reporting** the repos it cannot handle (TS/JS unsupported; xlarge repos beyond
the per-repo time budget).

## What it does NOT prove

- This is the **breadth** layer (clone + classify + lightweight regex symbol estimate). The full
  Roslyn/T-SQL **graph + edges + impact** is the **depth** layer (the .NET harness — `tools/LocalAIFactory.Benchmark`),
  proven separately on CleanArchitecture/eShop/ABP + the synthetic fixtures.
- 5 repos (angular, react, vue, nest, express) are **UnsupportedLanguage** — TS/JS are not extracted.
- 12 xlarge/large repos exceeded the per-repo time budget (see the failure analysis) — a coverage gap, not
  a crash.

See `PUBLIC_50_PROJECT_FAILURE_ANALYSIS.md` and `PUBLIC_50_PROJECT_PERFORMANCE_PROFILE.md`.
