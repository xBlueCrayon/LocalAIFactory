# Deployment Package Proof (Phase 3)

**Date:** 2026-06-21 · Commit `3d5523a`

A fresh build + test + publish + package was produced for this deployment proof.

| Step | Command | Result |
|---|---|---|
| Build | `dotnet build LocalAIFactory.sln -c Release` | **0 errors** |
| Test | `dotnet test` | **240 / 240 pass** |
| Publish | `scripts/release/build-release.ps1` | **151 files** → `./.tmp-publish` (git-ignored), framework-dependent |
| Package | `scripts/release/package-release.ps1` | **`LocalAIFactory-release-20260621-093133.zip`**, 281 files |
| Package verify | `scripts/release/verify-release-package.ps1` | **PASS** |

## Artifact

| Field | Value |
|---|---|
| Publish folder | `./.tmp-publish` (git-ignored; runnable `LocalAIFactory.Web.dll` + deps + `knowledge-packs/` + appsettings) |
| Package | `./.tmp-release/LocalAIFactory-release-20260621-093133.zip` (git-ignored — never committed) |
| Size | **16.22 MB** (17,009,578 bytes) |
| SHA256 | `0C1805D91FFD1574A4CD0BF62A6071E0C85196294E3A5360084F9BECAD71F043` |

## Package contents (verified)

- **App binaries** ✅ (`LocalAIFactory.Web.dll` + framework-dependent deps; runs on the installed ASP.NET Core 10.0.9 runtime)
- **Knowledge packs** ✅ (4 pack directories under `knowledge-packs/`)
- **Database scripts** ✅ (`database/`)
- **Docs + scripts** ✅ (`docs/`, `scripts/`, `install/`, `checks/`)
- **Manifest + release notes** ✅ (`RELEASE_MANIFEST.{json,md}`, `RELEASE_NOTES.md`)
- No secrets, DB backups, model files, or oversized assets (asserted by `verify-release-package.ps1`)

The publish output in `./.tmp-publish` is the exact artifact run in the Mode C deployment proof
(`DEPLOYMENT_PUBLISHED_APP_PROOF.md`).
