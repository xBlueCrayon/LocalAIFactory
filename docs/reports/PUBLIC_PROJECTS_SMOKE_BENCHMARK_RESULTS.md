# Public-Projects Smoke Benchmark — Results

**Date:** 2026-06-21 · Suite `public50-smoke` · 5 small real repos · `scripts/benchmark/run-50-project-benchmark.ps1`

A fast smoke to validate the runner end-to-end before the full 50-repo run.

| Repo | Language | Status | Supported files | C# symbols | Py symbols | SQL objs | LOC |
|---|---|---|---:|---:|---:|---:|---:|
| jasontaylordev/CleanArchitecture | C# | **Passed** | 110 | 342 | 0 | 0 | 3,011 |
| dotnet-architecture/eShopOnWeb | C# | **Passed** | 254 | 688 | 0 | 0 | 8,863 |
| pallets/flask | Python | **Passed** | 85 | 0 | 1,638 | 2 | 14,105 |
| expressjs/express | JavaScript | **UnsupportedLanguage** | 0 | 0 | 0 | 0 | 0 |
| scrapy/scrapy | Python | **Passed** | 442 | 0 | 5,759 | 0 | 65,079 |

**Totals:** 5 attempted, 5 cloned, **4 Passed + 1 UnsupportedLanguage**; 891 supported files; 1,030 C#
symbols; 7,397 Python symbols; 2 SQL objects; 91,058 LOC. Average score **85**.

## Reading

- Real C#/Python symbol extraction works on real public repos (not just synthetic fixtures).
- `expressjs/express` (JavaScript) is honestly reported as **UnsupportedLanguage** — the extractor does not
  cover TS/JS; the repo is cloned and classified but not symbol-extracted.

This confirms the runner clones, classifies, counts, scores, and cleans up correctly. The full 50-repo
run is in `PUBLIC_50_PROJECT_BENCHMARK_RESULTS.md`.
