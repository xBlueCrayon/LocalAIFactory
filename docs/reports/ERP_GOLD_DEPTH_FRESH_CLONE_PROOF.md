# ERP Gold Depth — Fresh-Clone Regeneration Proof (Phase 16)

**Date:** 2026-06-21 · **Commit proven:** `3ce8d0b`

Proves the committed repository is self-sufficient: a clean clone reproduces the **depth** ERP
(real manufacturing, report depth, incremental migration) with no uncommitted state.

## Procedure & result

1. `git clone --depth 1` of the ERP-GOLD-DEPTH commit into a temp directory.
2. Confirmed depth artifacts present in the clone: `ManufacturingService.cs`, `ReportsService.cs`,
   the `AddManufacturing` migration, and the 3 new `erp-gold-*-depth*` knowledge packs.
3. `dotnet build tools/LocalAIFactory.Generator` → **Build succeeded**.
4. `dotnet run ... --mode erp-gold --target ...GoldGenerated-Depth-Fresh` → **23 spec modules, autonomy
   100%, 108 product files**.
5. `dotnet build` the emitted solution → **0 errors**.
6. `dotnet test` → **235 passed, 0 failed**.
7. `verify-all-knowledge-packs.ps1` → **PASS — 31 packs, 973 items, no UID collisions**.

**PASS.** A clean clone regenerates a building, fully-tested **depth** ERP — real BOM/production-order
manufacturing with costing and quality gating, the report-depth service + REST API, the 26 end-to-end
scenarios, and the committed `InitialCreate` + `AddManufacturing` migration history — entirely from the
committed generator templates and the `erp-gold` reference spec. The 5 non-deterministic local-LLM modules
are not part of a pure regeneration, by design (see `benchmarks/results/erp-gold-depth-reference-vs-generated.json`).
The temporary clone was discarded after the run and is not committed.
