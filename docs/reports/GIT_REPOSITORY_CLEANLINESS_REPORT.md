# Git Repository Cleanliness Report

**Date:** 2026-06-21 · **Commit:** `96fbbc4` · **Branch:** `ke-008-code-symbols`
**Authoritative current status:** [`CURRENT_STATUS.md`](CURRENT_STATUS.md)

## Result: clean

| Check | Result |
|---|---|
| Working tree | ✅ clean |
| Forbidden tracked files | ✅ **none** |
| Tracked files > 5 MB | ✅ **none** |
| Git history rewritten | ✅ **no** |
| `docs/screenshots/` | ✅ kept intentionally (small UI evidence) |

## Forbidden tracked files — none

`git ls-files` was scanned for the forbidden patterns and returned **no matches**:

- `bin/`, `obj/`
- `publish/`, `dist-local/`
- `node_modules/`
- `*.exe`, `*.dll`, `*.zip`, `*.db`
- `.tmp-*`

These are all produced by local builds and are covered by `.gitignore`; none are tracked.

## File size — none over 5 MB

No tracked file exceeds 5 MB. The largest intentionally tracked binaries are the LocalAIFactory UI
screenshots under `docs/screenshots/*.png`, each well under 5 MB.

## docs/screenshots — intentional, kept

`docs/screenshots/*.png` are small LocalAIFactory UI screenshots used as documentation evidence
(home dashboard, readiness, knowledge, projects, coverage, graph explorer, benchmarks, audit, users,
etc.). They are deliberately tracked. Transient Playwright/generated screenshots are git-ignored and
are not part of this set.

## History not rewritten

This cleanup added documentation only. **No git history was rewritten** — no `filter-branch`, no
force-push of rewritten history, no removal of previously committed objects. The repository's commit
trail and the point-in-time evidence reports under `docs/reports/` remain intact.

## Related

- [`LOCAL_AND_REPO_CLEANUP_REPORT.md`](LOCAL_AND_REPO_CLEANUP_REPORT.md) — local junk vs tracked files.
- [`REPOSITORY_STRUCTURE_DECISION.md`](REPOSITORY_STRUCTURE_DECISION.md) — why historical trees stay
  in place.
- [`POST_CLEANUP_VALIDATION_REPORT.md`](POST_CLEANUP_VALIDATION_REPORT.md) — gate results after cleanup.
