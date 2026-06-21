# LAF ERP V5 — Fresh-Clone Regeneration Proof

**Date:** 2026-06-21 · **Cloned commit:** `d25e4ce`

Proves a clean checkout builds LocalAIFactory, verifies the knowledge packs, and regenerates + builds +
tests ERP V5 with the create-UI generator upgrade — no local-only dependency.

| Step | Result |
|---|---|
| `git clone --branch ke-008-code-symbols` (temp dir outside repo) | ✅ HEAD `d25e4ce`, **20 knowledge packs** |
| `dotnet build tools/LocalAIFactory.Generator` | ✅ 0 errors |
| `verify-all-knowledge-packs.ps1` | ✅ PASS (20 packs, 852 items) |
| `dotnet run -- --module-spec erpnext-production-suite.json --target ...-V5-Fresh --knowledge-usage` | ✅ emitted (spec-driven, incl. create-form UI) |
| `dotnet build LAF-EnterpriseERP-V5-Fresh.slnx` | ✅ 0 errors |
| `dotnet test` | ✅ **124 / 124** |

## Honest note on 124 vs 134

The fresh target had **no local-LLM proposal**, so the generator emitted only the **24 deterministic
spec-driven modules** (not the 5 governed LLM catalog modules) — 124 tests instead of 134. By design: the
spec-driven ERP **with the create-UI upgrade** regenerates from the committed templates + spec + knowledge
with zero network dependency, and the packs verify on a clean checkout. The LLM catalog layer is the
optional governed extension.
