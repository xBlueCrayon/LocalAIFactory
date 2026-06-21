# ERP Production-Focus — Start State

**Date:** 2026-06-21 · **Focus:** ERP only (no ScreenStream / no side tracks)

| Check | Result |
|---|---|
| Branch | `ke-008-code-symbols` (not main) |
| Working tree | clean |
| Latest commit | `41eff8a` (ERP V4 fresh-clone proof) |
| .NET SDK | 10.0.301 |
| Ollama | `qwen2.5-coder:14b`, `deepseek-r1:14b` |
| Draft release `v1.0.0-rc` | draft + prerelease |
| Final `v1.0` tag | none |
| Knowledge packs | 18 packs / 804 items |
| ERP versions present | V1, V2(LAFGenerated), V3, V4 |

## Honest target

Push the data-driven generator + knowledge toward **ERP_LOCAL_PRODUCTION_READY** by closing V4's biggest
gap — **create/edit UI forms** (V4 had list/read pages only) — plus more modules, more tests, and local
deployment scripts. **ERPNext-grade *production* parity is not locally achievable**: it requires real
authentication, CA TLS, MSSQL production-load, an external security review, and full module *depth*
(manufacturing MRP, statutory payroll, POS terminal, storefront, EF migrations). Those are external/
operator-owned and will be documented as blockers, not faked. No fake 100%, no fake parity.
