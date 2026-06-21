# ERP Gold — Local Production Hardening Proof

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21

## Build & test status

| Target | Result |
|--------|--------|
| Generator build | 0 errors |
| Gold build | 0 errors |
| GoldGenerated-Hardened build | 0 errors |
| `LocalAIFactory.sln` build | 0 errors |
| Gold xUnit | 210 PASS |
| Gold Playwright | 38 PASS |

## Three local blockers closed

| # | Blocker | Status |
|---|---------|--------|
| 1 | No committed EF migration history | CLOSED — InitialCreate (65 tables) + snapshot + design-time factory + apply/SQL-script/SQL-Express scripts + `MigrationsTests` |
| 2 | Create/list/read only; no edit/delete UI | CLOSED for masters — `CatalogCrudService<T>` + edit/deactivate UI + tests; posted documents immutable by design |
| 3 | Auth lacked lockout/password-policy/anti-forgery/secure-cookie | CLOSED — lockout, password policy, anti-forgery, hardened cookie, session timeout, audited events |

## Deployment (Phase 7 PASS)

Scripts under `scripts/`: `publish-local-production.ps1`, `run-production-smoke.ps1`, `backup-db.ps1`, `restore-db.ps1`, `backup-restore-health.ps1`, plus `docs/DEPLOYMENT.md`.

**Production smoke proof** (`deployment-evidence/production-smoke-proof.json`):

| Check | Result |
|-------|--------|
| loginPage | 200 |
| antiForgeryTokenPresent | true |
| wrongPassword | rejected (not 302) |
| goodPassword | 302 + `LafErpCookie` set |
| noHttp500 | true |
| **pass** | **true** |

## Score movement (`benchmarks/results/erp-gold-hardening-score.json`)

| Metric | Before | After |
|--------|--------|-------|
| xUnit tests | 138 | 210 |
| Playwright tests | 16 | 38 |
| Mean local-production score | 67 | 78 |
| Local blockers open | 3 | 0 |

## Classification

- **BEFORE:** high `ERP_PILOT_READY`.
- **AFTER:** `ERP_LOCAL_PRODUCTION_READY` (core local criteria met).

## Honest limitations

- **No 100% claim, no ERPNext parity claim, no external certification.**
- EF migration **not applied on a live SQL Express** in this environment.
- Manufacturing / HR / POS are CRUD skeletons; posted documents are immutable by design.
- **External gates remain:** real SSO/OIDC, CA-signed TLS, independent external security review, signed customer acceptance.
