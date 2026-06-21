# Public 50-Project Benchmark — Performance Profile

**Date:** 2026-06-21 · Suite `public50` (51 attempted) · host `DESKTOP-M1HANKN`

| Metric | Value |
|---|---|
| Total wall-clock (51 repos, clone + analyze + cleanup) | **~824 s (~14 min)** |
| Repos cloned + analyzed | 39 |
| Supported files analyzed | 86,134 |
| Total LOC analyzed | 7,565,513 |
| C# + Python symbols extracted | 365,579 |
| Peak disk for clones | **~0 MB held** (each clone deleted immediately after analysis) |

## Slowest analyzed repos (clone + classify + symbol-estimate)

| Repo | Supported files | Duration |
|---|---:|---:|
| abpframework/abp | 7,742 | 87.6 s |
| odoo/odoo | 8,951 | 52.5 s |
| dotnet/efcore | 5,761 | 32.8 s |
| nopSolutions/nopCommerce | 3,631 | 32.3 s |
| OrchardCMS/OrchardCore | 5,340 | 30.8 s |

## Observations

- **Disk is bounded.** Clones are shallow (`--depth 1`) and **deleted immediately after analysis**, so the
  cache never accumulates (peak ~0 MB held between repos) — important for a 51-repo sweep on a workstation.
- **Throughput scales with file count**, as expected: the largest analyzed repos (abp 7.7k, odoo 9k files)
  dominate the time; small repos finish in 1–3 s.
- **The bottleneck is clone time for xlarge repos**, not analysis — the 12 over-budget repos spent their
  whole window cloning, not extracting (see the failure analysis).

## Honest scope

- This profiles the **breadth runner** (clone + lightweight extraction), not the full graph build.
- No memory instrumentation was captured (a `maxFilesToAnalyze` cap bounds per-repo work instead). A
  production-scale run would add memory profiling and a longer per-repo budget on a server-class host.
