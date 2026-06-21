# Public-50 Project Benchmark — Scoring & Methodology

A **breadth** benchmark that attempts ≥50 real public GitHub projects to measure how LocalAIFactory's
deterministic extractor behaves across real-world languages, sizes, and domains — **honestly**, including
the large fraction it does **not** fully support.

> **Breadth vs depth.** This benchmark is the **breadth** layer: clone + language classification +
> supported-file/LOC counts + a **lightweight regex symbol estimate** (declaration counts for C#/Python/SQL).
> It is **not** the full Roslyn/T-SQL graph extraction. The **depth/graph** layer remains the .NET harness
> (`tools/LocalAIFactory.Benchmark`), which builds real symbols + edges + impact on the supported C# repos
> (CleanArchitecture, eShopOnWeb, eShopOnAbp) and the synthetic fixtures. The two are reported separately.

## What is supported

The deterministic extractor fully supports **C#**, **T-SQL**, and **Python**. TypeScript/JavaScript, Java,
Go, Ruby, etc. are **not** structurally extracted — they are cloned and file-counted and reported as a
**gap**, never as a pass.

## Pipeline (per repo)

1. **Shallow clone** (`git clone --depth 1 --single-branch`) with a **per-repo timeout** + one retry.
2. **Resolve HEAD SHA** (`git rev-parse`) — recorded for reproducibility.
3. **Classify files** by extension, honouring exclude patterns (`bin`, `obj`, `node_modules`, `.git`, …).
4. **Count** supported files (`.cs`/`.sql`/`.py`), LOC (capped), and a lightweight symbol estimate
   (C# type/method decls, Python `def`/`class`, SQL `CREATE TABLE/PROC/VIEW/FUNCTION`) over up to
   `maxFilesToAnalyze` files.
5. **Delete the clone** after analysis (disk is bounded; cache stays near 0).

## Statuses

| Status | Meaning |
|---|---|
| `Passed` | supported files found and fully analyzed (within the file cap) |
| `PassedPartial` | supported files analyzed up to the cap (repo larger than cap) |
| `ValidationOnly` | cloned + classified; deep analysis skipped by design (validation-only mode) |
| `UnsupportedLanguage` | no C#/SQL/Python files (e.g. a TS/JS/Java repo) — honest gap |
| `NoSupportedFiles` | full-mode repo with zero supported files |
| `CloneFailed` / `CheckoutFailed` / `TimedOut` | transient or size/network failure |

## Score (per repo)

```
Passed = 100 | PassedPartial = 75 | ValidationOnly = 50 | Unsupported/NoSupported = 25 | Failed/TimedOut = 0
```

The **average score** is deliberately dragged down by unsupported/large repos — that is the honest point:
the benchmark measures real coverage, not a curated win. A high average would mean the repo set was rigged.

## Reproduce

```powershell
pwsh scripts\benchmark\pin-public-projects.ps1 -Manifest benchmarks\public-projects-50.json      # resolve SHAs
pwsh scripts\benchmark\run-50-project-benchmark.ps1 -Manifest benchmarks\public-projects-smoke.json -MaxRepos 5 -Suite public50-smoke
pwsh scripts\benchmark\run-50-project-benchmark.ps1 -Manifest benchmarks\public-projects-50.json  -MaxRepos 51 -Suite public50 -TimeoutPerRepoSeconds 180
pwsh scripts\benchmark\summarize-50-project-results.ps1 -Suite public50
pwsh scripts\benchmark\clean-benchmark-cache.ps1
```

Cloned repos live under the **git-ignored** `.tmp-benchmark-repos/` and are **never committed**. Only the
JSON/markdown **result summaries** are committed.
