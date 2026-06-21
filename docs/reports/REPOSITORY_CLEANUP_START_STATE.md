# Repository Cleanup — Start State

**Date:** 2026-06-21 · **Commit:** `96fbbc4` · **Branch:** `ke-008-code-symbols`

## Inventory

| Area | State |
|---|---|
| Working tree | clean (0 changes) |
| Forbidden tracked (bin/obj/.tmp-/publish/node_modules/.exe/.dll/.zip/db/dist-local) | **none** |
| Tracked screenshots | `docs/screenshots/*.png` — pre-existing, **small (<5 MB) intentional LocalAIFactory UI documentation**, kept as evidence (not transient Playwright/generated output, which is git-ignored) |
| Large tracked files (> 5 MB) | none |
| `generated-products/` | LAF-EnterpriseERP (V1), LAF-EnterpriseERP-LAFGenerated (V2), V3, V4, **V5 (current)**, **LAF-ScreenStreamAssist (current)** |
| Root `README.md` | present (to be refreshed) |
| `docs/reports/` | 171 reports (point-in-time evidence; to be indexed) |
| Knowledge packs | 20 packs / 852 items |
| Draft release | `v1.0.0-rc` draft + prerelease; no `v1.0` tag |

## Cleanup approach (safe)

- **Do NOT move** V1-V4 generated products or break script paths / fresh-clone proofs. Instead mark
  current-vs-historical in `generated-products/README.md`.
- **Do NOT delete** knowledge packs, generated-product source, templates, specs, or docs/reports.
- Add an authoritative `docs/reports/CURRENT_STATUS.md` + a historical index so stale older reports do not
  misrepresent current progress.
- Local build junk (bin/obj/dist-local/temp clones) is already git-ignored; nothing forbidden is tracked.
