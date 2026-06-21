# ERP Gold Reference — Start State

**Date:** 2026-06-21 · **Commit:** `7764ea6` · **Branch:** `ke-008-code-symbols`

| Check | Result |
|---|---|
| Working tree | clean |
| Factory build / tests | 0 errors · 240 / 240 (confirmed in REPO-CLEANUP) |
| Gate V3 | `NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` |
| Knowledge packs | 20 packs / 852 items, verify PASS |
| `dotnet-ef` | **10.0.9 installed** (EF migrations feasible) |
| Ollama | `qwen2.5-coder:14b`, `deepseek-r1:14b` |
| Draft release | `v1.0.0-rc` draft; no `v1.0` tag |

## Honest scope statement (read first)

The sprint's stretch targets — **400+ .NET tests, 75+ Playwright, the full 30-module production ERP** — are
**not achievable in a single session** and are **not** claimed. ERP Gold is a focused **productionization
of the proven generated engine** with the genuine production upgrades V5 lacked, captured as reusable
templates/knowledge so LocalAIFactory learns them:

1. **Real authentication + login UI** (PBKDF2 password hashing, seeded admin, cookie auth) — opt-in via
   config so the deterministic test suite stays green; V5 had dev-cookie auth only.
2. **EF migrations** path (SQL Server) + `Database.Migrate()` for production, alongside `EnsureCreated` for
   the portable SQLite test mode.
3. **Deployment scripts** (publish / SQL Express setup / migrate / backup / restore / health).

Every manual addition is placed in `tools/LocalAIFactory.Generator/templates/` so the generator reproduces
it (GoldGenerated). Honest classification target: **approaching ERP_LOCAL_PRODUCTION_READY** (real auth +
migrations + deployment), **not** full production-grade or ERPNext-grade. Gaps documented, not faked.
