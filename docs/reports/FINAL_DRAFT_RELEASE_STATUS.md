# Final Draft Release Status

**Date:** 2026-06-21

| Field | Value |
|---|---|
| Tag | `v1.0.0-rc` |
| isDraft | **true** |
| isPrerelease | **true** |
| Published? | **No** (unchanged — deliberately not published) |
| Final `v1.0` tag | **none** |
| Asset | `LocalAIFactory-release-20260621-040519.zip` (16.21 MB), digest matches `checksums/` |

## Regenerate the release package?

This phase added scripts, manifests, result summaries, knowledge packs, and docs — **no application source
changes** that affect the deployable binaries. The existing draft asset remains a valid representation of the
app. **Decision: do NOT regenerate/re-upload the asset this phase** — the new artifacts are docs/scripts/data,
not app code, and re-uploading risks unnecessary churn on a draft that is review-ready.

An operator may regenerate after review with `build-release.ps1` + `package-release.ps1` +
`verify-release-package.ps1` if they want the new docs/knowledge-packs bundled into the ZIP (the packs are
already source-controlled and install at startup regardless).

## Safe to review? **Yes.** Safe to publish? **No** — remains a human decision; not performed.
