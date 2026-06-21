# GitHub Release Instructions

How to publish a LocalAIFactory release. The **source repository** is the deliverable; the **release ZIP** is a
build artifact attached to a GitHub Release (it is git-ignored and never committed to the repo).

## Prerequisites

- `gh` (GitHub CLI) authenticated with `repo` scope: `gh auth status`.
- A generated package (git-ignored) under `.tmp-release/`:

```powershell
scripts/release/build-release.ps1        # dotnet publish -> .tmp-publish (151 files)
scripts/release/package-release.ps1      # -> .tmp-release/LocalAIFactory-release-<stamp>.zip (16.2 MB, 277 files)
scripts/release/verify-release-package.ps1
scripts/release/simulate-clean-install.ps1
scripts/release/customer-acceptance-check.ps1   # -> ACCEPTED
```

## Create a DRAFT pre-release (recommended — reversible, not public until published)

```powershell
$zip = (Get-ChildItem .tmp-release/LocalAIFactory-release-*.zip | Sort-Object LastWriteTime -Desc | Select-Object -First 1).FullName
gh release create v1.0.0-rc "$zip" `
  --draft --prerelease `
  --title "LocalAIFactory v1.0.0-rc (Customer Handover Candidate)" `
  --notes-file RELEASE_NOTES.md `
  --target ke-008-code-symbols
```

- `--draft` keeps it private to repo collaborators and **does not create/push the tag** until you publish — fully
  reversible (`gh release delete v1.0.0-rc`).
- `--prerelease` marks it as a release candidate, not a final release.
- The ZIP is uploaded as a **release asset**, not committed to the repo.

## Publish (only when the operator approves a final release)

```powershell
gh release edit v1.0.0-rc --draft=false           # publishes the RC (creates/pushes the tag)
# For the FINAL v1.0 (after production deployment + external review + a signed-off pilot):
gh release create v1.0.0 "$zip" --title "LocalAIFactory v1.0" --notes-file RELEASE_NOTES.md --target <commit>
```

> Do **not** tag final `v1.0` until the release is genuinely complete (executed production deployment, external
> security review, and a signed-off customer pilot) and the operator approves. Until then, `v1.0.0-rc` (draft /
> prerelease) is the correct artifact. See `docs/FINAL_RELEASE_CERTIFICATE.md` for the exact remaining proof.

## What ships in the ZIP

`app/` (published binaries) · `knowledge-packs/` (4 packs / 438 items) · `database/` (setup/verify/backup
scripts) · `docs/` + `docs/screenshots/` (manuals + guides + 11 screenshots) · `scripts/`
(knowledge/diagnostics/security/release/poc/support) · `release-template` files (README, LICENSE placeholder,
THIRD-PARTY-NOTICES, VERSION) · `RELEASE_NOTES.md` · `RELEASE_MANIFEST.json`. **Excluded:** secrets, keys,
database files, backups, model files, logs.
