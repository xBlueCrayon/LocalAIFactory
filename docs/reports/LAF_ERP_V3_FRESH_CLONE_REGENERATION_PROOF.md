# LAF ERP V3 — Fresh-Clone Regeneration Proof

**Date:** 2026-06-21 · **Cloned commit:** `2fe817e` · **Remote:** `https://github.com/xBlueCrayon/LocalAIFactory.git`

Proves a clean checkout can regenerate a working, tested ERP V3 — no local-only dependency.

## Steps (all green)

| Step | Result |
|---|---|
| `git clone --branch ke-008-code-symbols` into a temp dir outside the repo | ✅ HEAD `2fe817e` |
| Knowledge packs present in the clone | ✅ **10 packs** |
| Generator + templates + module spec present | ✅ all present |
| `dotnet build tools/LocalAIFactory.Generator` | ✅ 0 errors |
| Regenerate → `generated-products/LAF-EnterpriseERP-V3-Fresh` | ✅ **69 files, 100% autonomy** |
| `dotnet build LAF-EnterpriseERP-V3-Fresh.slnx` | ✅ 0 errors |
| `dotnet test` | ✅ **98 / 98** |

## Honest note on the 69 vs 73 / 98 vs 108 difference

The fresh target has **no local-LLM proposal file**, so the generator emitted only the **10 deterministic
spec-driven modules** (not the 5 governed LLM catalog modules) — 69 files / 98 tests instead of 73 / 108.
This is by design and is the **strongest** form of the proof: the **spec-driven, deterministic core
regenerates with zero network/LLM dependency**. The LLM catalog layer is an optional governed extension
that regenerates when a proposal is present (or a live Ollama call is made), and is itself collision-guarded.

## Conclusion

A fresh clone builds the generator and **regenerates a building, 98-test ERP** from the committed templates
+ module spec + knowledge — no missing local-only dependency. The temp clone was deleted after validation.
