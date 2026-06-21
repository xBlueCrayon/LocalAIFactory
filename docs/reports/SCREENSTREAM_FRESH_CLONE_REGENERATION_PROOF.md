# ScreenStream — Fresh-Clone Regeneration Proof

**Date:** 2026-06-21 · **Cloned commit:** `68a902b`

Proves a clean checkout can regenerate and build the screen-share product with no local-only dependency.

| Step | Result |
|---|---|
| `git clone --branch ke-008-code-symbols` (temp dir outside the repo) | ✅ HEAD `68a902b` |
| Template `tools/.../templates/screen-stream-assist/` present | ✅ |
| `dotnet build tools/LocalAIFactory.Generator` | ✅ 0 errors |
| `dotnet run -- --mode screen-stream-assist --target ...-Fresh` | ✅ **emitted 24 files** |
| `dotnet build LAF-ScreenStreamAssist-Fresh.slnx` | ✅ 0 errors |
| `dotnet test` | ✅ **12 / 12** |

The committed repo carries **source only** (no EXEs); a fresh clone rebuilds the generator, **re-emits the
screen-share solution from the templates, builds it, and passes all 12 tests**. To produce runnable EXEs on
a fresh machine, run `scripts/publish-local-test-folder.ps1` (publishes to `C:\LAFScreenStreamAssist`).
The temp clone was used for validation only.
