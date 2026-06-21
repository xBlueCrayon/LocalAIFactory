# ERP Gold Hardened — Fresh-Clone Regeneration Proof (Phase 12)

**Date:** 2026-06-21 · **Commit proven:** `1431597`

Proves the committed repository is self-sufficient: a clean clone reproduces the **hardened** ERP
(EF migrations, auth hardening, edit/delete UI, scenarios) with no dependency on uncommitted state.

## Procedure & result

1. `git clone --depth 1` of the ERP-GOLD-HARDENING commit into a temp directory.
2. Confirmed the hardening artifacts are present in the clone: the committed `InitialCreate` migration,
   `ScenarioLibraryTests.cs`, and the 6 new `erp-gold-*` knowledge packs.
3. `dotnet build tools/LocalAIFactory.Generator` → **Build succeeded**.
4. `dotnet run ... --mode erp-gold --target ...GoldGenerated-Hardened-Fresh` → **23 spec modules,
   autonomy 100%, 99 product files emitted**.
5. `dotnet build` the emitted solution → **0 errors**.
6. `dotnet test` → **202 passed, 0 failed**.
7. `verify-all-knowledge-packs.ps1` → **PASS — 28 packs, 939 items, no UID collisions**.

**PASS.** A clean clone regenerates a building, fully-tested **hardened** ERP — committed EF migration
history, PBKDF2 + lockout + anti-forgery auth, generic edit/soft-delete UI, double-entry GL with
maker/checker and audit, and the 13 end-to-end scenarios — entirely from the committed generator
templates and the `erp-gold` reference spec. The 5 non-deterministic local-LLM modules are not part of a
pure regeneration, by design (see `benchmarks/results/erp-gold-hardened-reference-vs-generated.json`).
The temporary clone was discarded after the run and is not committed.
