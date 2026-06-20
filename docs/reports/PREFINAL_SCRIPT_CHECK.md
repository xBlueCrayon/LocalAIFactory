# PREFINAL — Script Hygiene (Phase 5)

**Date:** 2026-06-21 · **Scope:** `scripts/`, `database/`, `deploy/`, `release-template/`

## PowerShell parse check

All **78** `.ps1` files (excluding `bin/obj/.vs/.git`) parsed with the PowerShell AST parser:
**0 parse errors.**

## Trackability

- No script under `scripts/`, `database/`, `deploy/`, `release-template/` is accidentally ignored by
  `.gitignore`. (`scripts/release/**` is explicitly un-ignored from the `[Rr]elease/` rule; verified.)

## Safety / staleness scans

| Check | Result |
|---|---|
| Stale milestone names (`KE-00x` as current) in scripts | none |
| Hardcoded passwords / secrets / API-key literals | none (placeholders + Integrated Security only) |
| Unguarded destructive defaults (`DROP DATABASE`, `git reset --hard`, `git clean -fdx`, `rm -rf /`) | none unguarded |

### Destructive-pattern review (INFO only)

`security-audit.ps1` flags two scripts for human review — both are **correctly gated**, not defects:

- `database/reset-derived-indexes.ps1` — **dry-run by default** (`-Execute` required); never touches
  KnowledgeItems / curated knowledge / audit / provenance / imported raw; header explicitly states *no
  `DROP DATABASE`* (the scanner matched that comment text).
- `scripts/auto/plan-change.ps1` — planning script; the matched token is in guidance text, not an executed
  destructive command.

## Key scripts spot-checked

`scripts/poc/verify-poc.ps1` (read-only gate), `scripts/poc/ui-smoke-test.ps1` (starts/stops its own app, no
destructive actions), `scripts/release/*` (build/package/install/verify/uninstall-dry-run),
`scripts/docs/capture-screenshots.ps1` (read-only GETs; honest Node-absent fallback),
`scripts/security/security-audit.ps1` (static read-only scans), `scripts/diagnostics/*` (read-only snapshots),
`database/*.ps1` (create-if-absent, never drop), `deploy/**/*.ps1` (compression opt-in, operator-gated).

**Verdict: scripts are syntactically clean, safe-by-default, and release-ready.**
