# Post-Release: Repository Cleanliness

**Date:** 2026-06-21 · **Branch:** `ke-008-code-symbols`

## Tracked-file hygiene

`git ls-files` → **743** tracked files.

**Forbidden artifacts scan** (pattern `bin/`, `obj/`, `.tmp`, `/publish/`, `*.bak`, `*.log`,
`node_modules`, `*.mdf`, `*.ldf`, `*.gguf`, `*.onnx`, `release*.zip`): **NONE tracked.**

**Large-file scan** (tracked files > 5 MB): **NONE.**

## Ignored artifacts (correctly excluded)

`git status --ignored` confirms these are present on disk but **git-ignored**, never committed:

- Build output: every `src/**/bin`, `src/**/obj`, `tests/**`, `tools/**`.
- Temp/release working dirs: `.tmp-release`, `.tmp-publish`, `.tmp-playwright`, `.tmp-clean-install`.
- Secrets/keys: `keys/`, `src/LocalAIFactory.Web/keys/` (Data Protection keys).
- Local tooling: `.vs/`, `.claude/`, `benchmarks/cache/`, `benchmarks/reports/`.

The release ZIP lives under the ignored `.tmp-release/` and is **delivered as a GitHub release asset**,
not committed to git — as required.

## Untracked files staged for this commit (recovery work only)

- `checksums/LocalAIFactory-v1.0.0-rc.sha256`
- `docs/CUSTOMER_HANDOVER_WALKTHROUGH.md`
- `scripts/deployment-drill/` (10 files)
- `scripts/sso/` (3 files)
- `docs/ROADMAP_TO_TRUE_20_OF_20.md`, `docs/SSO_ENTRA_ID_PROOF_PACK.md`, `docs/Production-Deployment-Drill-Pack.md`
- `docs/reports/POST_RELEASE_*.md`, `docs/reports/INTERRUPTION_RECOVERY_REPORT.md`

All are documentation, checksums, or safe operator scripts — no source deletions, no secrets, no
binaries.

## Conclusion

The repository is clean: no forbidden or oversized artifacts are tracked; build output, secrets, temp
dirs, and the release ZIP are all correctly ignored. Only recovery documentation, the checksum, and
operator-safe scripts are added by this commit.
