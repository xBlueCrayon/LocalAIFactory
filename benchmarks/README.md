# LocalAIFactory Validation Harness — benchmarks

The validation harness (`tools/LocalAIFactory.Benchmark`) is the authoritative, reproducible measure of
capability progression. It is deterministic, MSSQL-authoritative (or `--inmemory` for CI), and uses no model
or vectors. It exits non-zero on regression.

## Files

- `benchmarks.json` — the **baseline** manifest. Every repo here is pinned (exact SHA or fixed file set),
  cached locally, and scored on each run. Promotion into this file requires approval.
- `golden/<CODE>.json` — golden snapshots for regression detection.
- `cache/<CODE>/` — local clones (git-ignored content; never vendored into the repo).
- `reports/latest.json` — the most recent run report.
- `repo-candidates.json` — **controlled discovery list** of candidate repos *not yet* in the baseline.

## Running

```powershell
cd tools/LocalAIFactory.Benchmark
dotnet run -c Release -- --inmemory          # CI-friendly, no SQL Server required
dotnet run -c Release --                     # SQL Server / LocalDB
dotnet run -c Release -- --update-golden     # re-baseline (after an intended capability change)
```

Pass criteria: `povFailures == 0 && regressions == 0 && coverageFailures == 0`. A proof-of-vision
regression or a missing coverage report is a **failure**; raw count drift on an unpinned branch is a warning.

## Adding a repo (controlled discovery — no blind scraping)

Candidates live in `repo-candidates.json`. A candidate is promoted to `benchmarks.json` only after:

1. **Find** the candidate and record it (URL, stack, reason).
2. **License review** — confirm the licence permits cloning/analysis; record it.
3. **Categorize** — assign a category and stack.
4. **Pin** an exact commit SHA (never a moving branch for the baseline).
5. **Expected gap behaviour** — what the coverage/gap report should show (incl. unsupported languages).
6. **Proof-of-vision** — add `find` / `dependents` / `impact` questions where possible.
7. **Tier** — assign Smoke / Standard / Extended.
8. **Approval** — only then add the entry to `benchmarks.json`.

### Suite tiers (planned)

- **Smoke** — tiny, fast, runs on every major build.
- **Standard** — the current baseline; runs before release/major commit.
- **Extended** — large / slow / multi-language; manual or nightly. Multi-language repos (Python, VB.NET,
  document-image research) currently run as **gap-only** baselines until the corresponding extractors exist —
  they prove honest unsupported-file reporting, not extraction capability.

> Honesty rule: a gap-only baseline must never be presented as a capability score. Unsupported files are
> reported, never hidden.
