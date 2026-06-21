# Performance & Resource Profile

**Date:** 2026-06-21 · IIS HTTPS/Windows-auth pilot + 113-system benchmark.

## IIS load (local simulation)

| Suite | Requests | RPS | p50 | p95 | p99 | HTTP 500 | App pool |
|---|---:|---:|---:|---:|---:|---:|---|
| smoke (mixed) | 19,300 | 321.7 | 54.8 ms | 81.7 ms | 96.8 ms | 0 | stable |
| search (DB-backed) | 10,240 | 170.7 | 74.5 ms | 116 ms | 128.9 ms | 0 | stable |

- DB-backed search is ~2× the latency of static pages (expected — it hits SQL Express), still **p99 < 130 ms**.
- The app pool stayed `Started` across **29,540** authenticated HTTPS requests — no crash/recycle.

## Benchmark throughput

- 51-repo run: **7.56M LOC** analyzed in ~14 min; clone cache held **~0 MB** (clones deleted after each).
- 113-system manifest: clone budget bounded per repo; xlarge monorepos are the scale ceiling.
- Local-LLM: `qwen2.5-coder:14b` ~79 tok/s after model load (12.5 s first call).

## Resource discipline

- All clones/publish/IIS-folder/docs-cache/proposals are **git-ignored** and deleted/bounded — the working
  tree stays small (tracked content remains a few MB).
- No memory/CPU instrumentation was captured under load beyond app-pool state (a production profile would add
  perf counters / dotnet-counters). This is a **workstation** profile, not a server capacity model.
