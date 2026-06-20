# PREFINAL — Repository Status (Phase 0)

**Date:** 2026-06-21 · **Pass:** prefinal-cleanup / release-readiness (read-only Git reality check)

## Git reality (detected, not assumed)

| Fact | Value |
|---|---|
| Current branch | `ke-008-code-symbols` (existing historical branch; kept — not renamed) |
| Tracking | up to date with `origin/ke-008-code-symbols` |
| Remote | `origin` → https://github.com/xBlueCrayon/LocalAIFactory.git |
| Latest commit at start | `08f0683` R2-ACC-20X-CG6d: supportability dashboard spec doc |
| Working tree at start | **clean** (nothing to commit) |
| Tracked file count | **674** |
| Untracked files at start | none |
| Modified files at start | none |

## Tracked-artifact hygiene (start)

- bin / obj / publish / `.bak` / `.log` tracked: **0**
- Tracked files > 5 MB: **0**
- Suspicious tracked (`.pfx/.pem/.key/.mdf/.ldf/.gguf/.onnx/.safetensors`, `keys/`, `secrets.json`): **0**
- Largest tracked files: EF Core migration `*.Designer.cs` / `AppDbContextModelSnapshot.cs` (~90–106 KB each) — legitimate generated **source** committed on purpose.

## Ignored generated artifacts (correct)

29 ignored entries, all expected: `.claude/`, `.vs/`, `benchmarks/cache/`, `benchmarks/reports/`, `keys/`,
every `src/*/bin` + `src/*/obj`, and `src/LocalAIFactory.Web/Properties/` (launchSettings, intentionally ignored).

## Issue found (and fixed this pass)

- **Two real source files were being silently ignored** by the `[Cc]overage*/` rule (intended for test-coverage
  *output*): `src/LocalAIFactory.Ingestion/Coverage/ImportCoverageService.cs` and
  `src/LocalAIFactory.Web/Views/Coverage/Index.cshtml`. A fresh clone would have been **missing source** (build
  break / `/Coverage` 404). Fixed by adding scoped `!src/**/[Cc]overage/` negations and tracking both files.
  See `PREFINAL_GITIGNORE_ARTIFACT_CHECK.md`.

## Verdict

Repository was already clean and in sync at start; the only material defect was the ignored `Coverage` source,
now corrected. See `PREFINAL_RELEASE_READINESS_SUMMARY.md` for the ending state.
