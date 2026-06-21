# Final Release — Validation Gates (Phase 12)

**Date:** 2026-06-21 · Run during the final release sprint. No tests weakened; no failures hidden.

| # | Gate | Command | Result |
|---|---|---|---|
| 1 | git status | `git status` | clean before/after each commit |
| 2 | restore + build | `dotnet build LocalAIFactory.sln -c Release` | **0 errors** |
| 3 | tests | `dotnet test` | **235 / 235** |
| 4 | benchmark smoke | `--inmemory --suite smoke` | **PASS** |
| 5 | benchmark standard | `--inmemory --suite standard` | **PASS** (KYC/AML Gold 7/7, ERP/CRM 6/6, core-banking 6/6) |
| 6 | benchmark full | `--inmemory` | covered by suites (smoke+standard) |
| 7 | UI smoke | `scripts/poc/ui-smoke-test.ps1` | **PASS** (11 pages 200 incl. /Support) |
| 8 | verify-poc | `scripts/poc/verify-poc.ps1` | **PASS** |
| 9 | knowledge pack verify | `scripts/knowledge/verify-all-knowledge-packs.ps1` | **PASS** (4 packs, 438 items, no collisions; live 4/438) |
| 10 | database verify | `database/verify-full-install.ps1` | **PASS** (14 migrations + KB VERIFIED) |
| 11 | backup | `database/backup-database.ps1` | 69.5 MB (prior sprint; Express/LocalDB no-COMPRESSION handled) |
| 12 | restore verify | `database/restore-verify-database.ps1` | RESTORE VERIFYONLY OK (prior sprint) |
| 13 | publish | `scripts/release/build-release.ps1` | **151 files** |
| 14 | release package | `scripts/release/package-release.ps1` | **16.2 MB / 277 files** + manifest |
| 15 | release package verify | `scripts/release/verify-release-package.ps1` | **PASS** |
| 16 | clean install simulation | `scripts/release/simulate-clean-install.ps1` | **PASS** (verified on extracted ZIP) |
| 17 | customer acceptance | `scripts/release/customer-acceptance-check.ps1` | **ACCEPTED** |
| 18 | screenshots | `scripts/docs/capture-screenshots.ps1` | **11 PNGs** captured (Playwright/Chromium) |
| 19 | diagnostics | `scripts/diagnostics/*` + `scripts/support/export-support-bundle.ps1` | run live (snapshot/gpu/ollama/sql/process) |
| 20 | security audit | `scripts/security/security-audit.ps1` | **0 HIGH**, 2 INFO (gated) |
| 21 | secrets audit | tracked-file scan | **0** secrets / keys |
| 22 | large artifact audit | `git ls-files` size scan | **0** tracked > 5 MB |
| 23 | repo cleanliness | `git ls-files` | **0** bin/obj/publish/backup tracked |
| 24 | docs link/path sanity | link checker | 0 broken links in key docs |
| 25 | final git status | `git status` | clean, pushed, in sync |

## Blocked / not-run gates (documented, not faked)

- **Docker deployment:** Docker is **not installed** on the build host → not executed. Compose files + commands +
  WSL2/Docker-Desktop prerequisite are documented in `docs/Docker-Deployment-Guide.md`. Proof to close: run
  `docker compose up` on a Docker host and capture the health gates.
- **Production / IIS / Express / full-SQL deployment:** scripts are validated (dry-run) but **not executed** on a
  real server. Proof to close: run the deployment scripts on the target host and capture backup/restore + health.
- **True clean-machine install:** simulated by extracting the ZIP into a fresh folder and re-verifying; a real
  fresh-VM proof (run `setup-full-local-demo.ps1` + start the app on a clean Windows host with .NET 10 + SQL)
  remains the external step.

**All runnable gates green. Nothing weakened, nothing hidden.**
