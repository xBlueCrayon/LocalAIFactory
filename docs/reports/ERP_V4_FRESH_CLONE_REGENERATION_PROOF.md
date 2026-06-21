# ERP V4 — Fresh-Clone Regeneration Proof

**Date:** 2026-06-21 · **Cloned commit:** `b96e5b7`

Proves a clean checkout can build LocalAIFactory, verify the knowledge packs, and regenerate + build + test
ERP V4 with no local-only dependency.

| Step | Result |
|---|---|
| `git clone --branch ke-008-code-symbols` (temp dir outside repo) | ✅ HEAD `b96e5b7`, **18 knowledge packs** |
| `dotnet build tools/LocalAIFactory.Generator` | ✅ 0 errors |
| `verify-all-knowledge-packs.ps1` | ✅ PASS (18 packs, 804 items) |
| `dotnet run -- --module-spec erpnext-grade-modules-v4.json --target ...-V4-Fresh --knowledge-usage` | ✅ emitted (spec-driven) |
| `dotnet build LAF-EnterpriseERP-V4-Fresh.slnx` | ✅ 0 errors |
| `dotnet test` | ✅ **112 / 112** |

## Honest note on 112 vs 122

The fresh target had **no local-LLM proposal**, so the generator emitted only the **17 deterministic
spec-driven modules** (not the 5 governed LLM catalog modules) — 112 tests instead of 122. This is by
design and is the strongest proof: the **spec-driven, deterministic ERP regenerates from the committed
templates + module spec + knowledge with zero network/LLM dependency**, and the knowledge packs verify
on a clean checkout. The LLM catalog layer is the optional governed extension.
