# Draft Release Refresh Decision

**Date:** 2026-06-21

## Decision: **DO NOT refresh / re-upload the draft asset** this pass.

| Factor | Assessment |
|---|---|
| App source code changed? | **No** — this pass added scripts, manifests, JSON summaries, knowledge packs, docs, and emulation/integration packs. The deployable **binaries are unchanged.** |
| Existing asset still valid? | **Yes** — `LocalAIFactory-release-20260621-040519.zip` (16.21 MB) still represents the app; digest matches `checksums/`. |
| Knowledge packs in the ZIP? | The 2 new packs are **source-controlled** and install at app startup (idempotent); they do not require a new ZIP to be usable. |
| Risk of re-upload | Unnecessary churn on a **draft** that is already review-ready; re-packaging adds no app-behaviour change. |

## What an operator can do (optional)

To bundle the new docs/knowledge-packs into the release ZIP for distribution convenience:

```powershell
pwsh scripts/release/build-release.ps1
pwsh scripts/release/package-release.ps1
pwsh scripts/release/verify-release-package.ps1
# then optionally: gh release upload v1.0.0-rc <new-zip> --clobber   (draft stays draft)
```

This is **optional** and an operator choice. The draft remains **draft + prerelease**; **not published**; **no
final v1.0 tag**.

## Status

- Draft `v1.0.0-rc`: **draft = true, prerelease = true** (unchanged).
- Asset: unchanged, checksum-verified.
- **Safe to review: yes. Safe to publish: no** (human decision; not performed).
