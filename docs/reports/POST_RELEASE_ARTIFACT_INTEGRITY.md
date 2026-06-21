# Post-Release: Release Artifact Integrity

**Date:** 2026-06-21

## Artifact

| Field | Value |
|---|---|
| Path | `.tmp-release/LocalAIFactory-release-20260621-040519.zip` (git-ignored, by design) |
| Size | 16,997,982 bytes (16.21 MB) |
| Entries | **293** |
| SHA256 | `eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b` |

## Checksum

`checksums/LocalAIFactory-v1.0.0-rc.sha256` (tracked in git) contains:

```
eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b  LocalAIFactory-release-20260621-040519.zip
```

Recomputed `Get-FileHash … -Algorithm SHA256` on the ZIP → **matches** the checksum file **and** the
GitHub asset digest. Three independent records agree.

## Package contents (top-level)

`app`, `appsettings`, `checks`, `database`, `docs`, `install`, `knowledge-packs`, `samples`, `scripts`,
plus `README.md`, `RELEASE_NOTES.md`, `RELEASE_MANIFEST.json`, `RELEASE_MANIFEST.md`, `CHANGELOG.md`,
`VERSION.txt`, `LICENSE-PLACEHOLDER.txt`, `THIRD-PARTY-NOTICES.md`.

| Required content | Present |
|---|---|
| Application (`app/`, published binaries + web.config) | ✅ |
| Database scripts (`database/`) | ✅ |
| Knowledge packs (`knowledge-packs/`) | ✅ |
| Docs (`docs/`) | ✅ |
| Scripts (`scripts/`, `install/`, `checks/`) | ✅ |
| Screenshots / screenshot script | ✅ (capture script under `scripts/`; screenshots under `docs/`) |
| Release notes (`RELEASE_NOTES.md`) | ✅ |
| Manifest (`RELEASE_MANIFEST.json` / `.md`) | ✅ |

## Forbidden-content scan

Scanned the 293 entries for `*.bak`, `*.bacpac`, `*.mdf`, `*.ldf`, `*.gguf`, `*.onnx`, `keys/`, and
"secret":

- **No** database backups, model files, or key material.
- The only "secret"-matching entry is `docs/Secrets-Handling.md` — a **documentation file about secrets
  handling**, not a secret. This is a benign false positive.

## Conclusion

The release artifact is **complete, checksum-verified, and free of forbidden content**. The local ZIP,
the tracked checksum, and the uploaded GitHub asset are byte-identical.
