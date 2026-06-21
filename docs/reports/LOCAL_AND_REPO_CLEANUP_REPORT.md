# Local and Repository Cleanup Report

**Date:** 2026-06-21 · **Commit:** `96fbbc4` · **Branch:** `ke-008-code-symbols`
**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](CURRENT_STATUS.md)

## Summary

The working tree is clean. No forbidden build or binary artifacts are tracked. Local build junk
exists on disk (as it always does after a build) but is git-ignored and never committed. The only
binary files intentionally tracked are the small LocalAIFactory UI screenshots under
`docs/screenshots/`, kept as documentation evidence.

## Local build junk — git-ignored, not removed

Building the solution and the generated products produces the usual local-only output:

- `bin/`, `obj/` (per project)
- `publish/`, `dist-local/` style publish folders
- `node_modules/` (Playwright test tooling)
- transient `*.exe`, `*.dll`, `*.zip`, `*.db`, `.tmp-*` files
- transient Playwright / generated screenshots

All of the above are covered by `.gitignore` and are **not** tracked. We deliberately do **not**
delete them as part of this cleanup — they are expected local state and are reproduced on the next
build. The contract is "never committed," not "never present locally."

## Forbidden tracked files — none

A scan of `git ls-files` for the forbidden patterns
(`bin/`, `obj/`, `node_modules/`, `publish/`, `dist-local/`, `*.exe`, `*.dll`, `*.zip`, `*.db`,
`.tmp-*`) returns **no matches**. No tracked file exceeds 5 MB.

## docs/screenshots — kept intentionally

`docs/screenshots/*.png` are small (each < 5 MB) LocalAIFactory UI screenshots used as
documentation evidence (home dashboard, readiness, knowledge, projects, coverage, graph explorer,
benchmarks, audit, users, etc.). These are **intentional, durable documentation assets** and are
kept tracked.

Transient screenshots produced by Playwright runs or by the product generators are git-ignored and
are **not** part of this set.

## docs/reports

`docs/reports/` holds point-in-time evidence reports plus the authoritative
[`CURRENT_STATUS.md`](CURRENT_STATUS.md). See [`docs/reports/README.md`](README.md) and
[`docs/reports/HISTORICAL_REPORT_INDEX.md`](HISTORICAL_REPORT_INDEX.md). These are markdown text and
small; they are kept as the audit trail of the program.

## Result

- Working tree: clean.
- Forbidden tracked files: none.
- Tracked files > 5 MB: none.
- Git history: not rewritten.
- `docs/screenshots/`: intentional UI evidence, retained.

See also [`docs/reports/GIT_REPOSITORY_CLEANLINESS_REPORT.md`](GIT_REPOSITORY_CLEANLINESS_REPORT.md).
