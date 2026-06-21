# ERP Gold — Fresh-Clone Regeneration Proof (Phase 19)

**Date:** 2026-06-21

Proves the committed repository is **self-sufficient**: the LocalAIFactory generator reproduces the
ERP Gold engine (including the new real-auth + deployment layer) from a clean checkout, with no
dependency on any uncommitted local state.

## Procedure

1. `git clone --depth 1 file://D:/AI/Repositories/LocalAIFactory/.git <temp>` — clone of commit
   `86aa4b5` (the ERP-GOLD commit).
2. Confirmed the gold templates/spec are present in the clone (`PasswordHasher.cs`,
   `specs/erp-gold-reference.json`, `playwright/tests/login.spec.ts`).
3. `dotnet build tools/LocalAIFactory.Generator` → **Build succeeded**.
4. `dotnet run ... -- --mode erp-gold --target generated-products/FreshClone-GoldGenerated` →
   **23 spec modules, autonomy 100%, 79 product files emitted**.
5. `dotnet build` the emitted solution → **0 errors**.
6. `dotnet test` the emitted suite → **128 passed, 0 failed**.

## Result

**PASS.** A clean clone regenerates a building, fully-tested ERP — including the PBKDF2 auth layer,
cookie login, role claims, audit-on-login, create UI, double-entry GL, maker/checker, RBAC, and the
deployment scripts — all from the committed generator templates and the `erp-gold` reference spec.

This is the deterministic surface (the 5 non-deterministic local-LLM catalog modules are not part of a
pure regeneration, by design — see `benchmarks/results/erp-gold-reproduction-comparison.json`). The
temporary clone was discarded after the run and is not committed.
