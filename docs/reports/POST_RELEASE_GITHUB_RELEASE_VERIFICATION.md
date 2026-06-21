# Post-Release: GitHub Release Verification

**Date:** 2026-06-21 · **Repo:** `xBlueCrayon/LocalAIFactory`

## Tooling / auth

- `gh` version **2.95.0**.
- `gh auth status`: logged in to github.com as **xBlueCrayon** (keyring), active account, HTTPS, token
  scopes `gist, read:org, repo, workflow`. Authenticated and able to manage releases.

## Release state (`gh release view v1.0.0-rc --json …`)

The draft prerelease **already exists** — it was created in the interrupted session:

| Field | Value |
|---|---|
| `name` | `LocalAIFactory v1.0.0-rc (Customer Handover Candidate)` |
| `tagName` | `v1.0.0-rc` |
| `isDraft` | **true** |
| `isPrerelease` | **true** |
| `url` | `https://github.com/xBlueCrayon/LocalAIFactory/releases/tag/untagged-61c5cfb2ef502b64d7e1` |

> The URL shows `untagged-…` because a **draft** release does not create the git tag until it is
> published. This is expected for an unpublished draft and is **not** an error.

## Asset

| Field | Value |
|---|---|
| `name` | `LocalAIFactory-release-20260621-040519.zip` |
| `size` | **16,997,982 bytes** (16.21 MB) |
| `state` | `uploaded` |
| `contentType` | `application/zip` |
| `digest` | `sha256:eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b` |
| `downloadCount` | 0 |

**The asset digest matches the local checksum exactly** (`checksums/LocalAIFactory-v1.0.0-rc.sha256` =
`eac98e2c…`), confirming the uploaded asset is byte-identical to the locally-built and verified package.

## Conclusions

- A **draft prerelease** is in place, review-ready, with the correct verified asset attached.
- It is **NOT published** (`isDraft = true`) — consistent with the rule that no final v1.0 / GA release
  may be published in this work.
- **No final `v1.0` tag** exists, and none was created.
- Next operator action for the release: review the draft in the GitHub UI; publish **only** if/when the
  operator decides the prerelease is ready — this remains a human decision, not an automated one.
