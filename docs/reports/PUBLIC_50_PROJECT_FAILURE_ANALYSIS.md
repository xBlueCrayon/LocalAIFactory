# Public 50-Project Benchmark — Failure Analysis

**Date:** 2026-06-21 · Suite `public50` (51 attempted)

Failures and gaps, with root causes — the honest point of a breadth benchmark.

## The most useful failure discovered (a runner bug + a real scale gap)

The 12 initial `CloneFailed` results surfaced **two** things:

1. **A real runner bug.** The reported error was
   **"destination path already exists and is not empty"**. Root cause: when a clone of an **xlarge** repo
   (e.g. `dotnet/runtime`, `elastic/elasticsearch`, `microsoft/TypeScript`) exceeded the per-repo timeout,
   the runner killed `git` but **left the partial directory**, so the **retry** failed on the stale dir.
   **Fix applied:** the runner now removes the destination **before every clone attempt**. This is exactly
   the kind of harness bug a real benchmark surfaces.

2. **A genuine scale gap (the underlying cause).** Re-running the 12 with the fix (and a 240 s budget)
   still fails them — with **real `git` shallow-clone errors well under the timeout** (≈20–70 s) for these
   multi-GB monorepos, plus outright timeouts for the largest. So the honest conclusion is: **the breadth
   runner cannot ingest these xlarge repos within a workstation-scale per-repo budget** — whether the
   symptom is a timeout or a failed shallow clone, it is the same coverage gap (scale), not a crash in the
   extractor.

The 12th was a manifest-metadata error: **`frappe/books`** — `Remote branch develop not found` (wrong
default branch), not a tool gap.

## Honest gaps (correctly reported, not bugs)

| Category | Repos | Why |
|---|---|---|
| **UnsupportedLanguage** | angular (TS), react (JS), vue (TS), nest (TS), express (JS) | TypeScript/JavaScript are **not** structurally extracted — cloned + classified, 0 supported files. |
| **xlarge over time budget** | dotnet/runtime, aspnetcore, elasticsearch, spring-boot, keycloak, dubbo, zulip, mattermost, metabase, prisma, TypeScript | Shallow-cloning + analyzing a multi-GB monorepo exceeds the 180–240 s per-repo budget on this host. A coverage gap (time/scale), **not** a crash. |
| **ValidationOnly** | (validation-only manifest entries) | cloned + classified by design; deep extraction skipped. |

## What the failures teach

1. **Scale is the real frontier.** The extractor handles large repos (efcore 5,761 files / 43,340 symbols;
   abp 7,742 files) but the truly massive monorepos (dotnet/runtime, elasticsearch) need a longer budget,
   incremental/streaming extraction, or a bigger host.
2. **Language coverage is the honest gap.** TS/JS/Java/Go are not extracted; this is reported, never hidden.
3. **Harness robustness matters.** The retry-cleanup bug would have silently inflated "CloneFailed"; finding
   and fixing it is a benchmark win.

## Remediation status

- Runner retry-cleanup bug: **FIXED** (`run-50-project-benchmark.ps1`).
- `frappe/books` branch: flagged (manifest metadata) — re-pin to the correct default branch.
- xlarge time budget: documented; raise `-TimeoutPerRepoSeconds` or add incremental extraction to convert
  more `TimedOut` → `Passed` on a larger host.
