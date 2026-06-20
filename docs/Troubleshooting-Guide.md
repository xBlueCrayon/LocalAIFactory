# Troubleshooting Guide

Symptom → cause → fix tables for LocalAIFactory across install, database, auth, import, optional AI, and
deployment. This is the fast lookup; for step-by-step operational scenarios and diagnostics collection see
`docs/Support-Runbook.md`, and for the history of page hangs see `docs/07-Troubleshooting.md`.

**Golden rule:** the platform is local-first and MSSQL-authoritative. MSSQL must be reachable; Ollama and
Qdrant are **optional** and must never block a page. If a page hangs, something is on the request path that
should not be.

---

## Install / startup

| Symptom | Likely cause | Fix |
|---|---|---|
| App won't start | DB unreachable; bad connection string | Verify SQL with `sqlcmd -S <inst> -d LocalAIFactory -E -Q "SELECT 1;"`; fix connection string (env var / git-ignored override). See `docs/Industrial-Installation-Guide.md`. |
| Startup fails with a dev-auth error in prod | `Security:UseDevAuth` set outside Development | Remove it; `SecurityStartup.GuardDevAuth` fails startup on purpose. Dev auth is Development-only. |
| `dotnet build` fails | Wrong SDK / restore not run | `dotnet restore` then `dotnet build LocalAIFactory.sln -c Release`. Requires .NET 10. |
| Health check fails post-install | App not running on expected URL | `./scripts/release/post-install-healthcheck.ps1 -Url http://localhost:8080`; confirm port and that the app is up. |
| Need to confirm a clean install | — | `./scripts/release/verify-installation.ps1` and `./scripts/poc/verify-poc.ps1`. |

## Database

| Symptom | Likely cause | Fix |
|---|---|---|
| `SELECT 1` fails / connection timeout | SQL instance down or unreachable | Start/repair the instance; check firewall and instance name (LocalDB / Express / full). |
| Schema out of date / migration error | Migrations not applied | `./database/apply-migrations.ps1` or `dotnet ef database update --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web`. |
| Fresh DB needed | New environment | `./database/create-localdb.ps1` \| `create-sqlexpress-db.ps1` \| `create-full-mssql-db.ps1`, then `apply-migrations.ps1`. |
| Restore must be validated safely | Verifying a backup | `./database/restore-verify-database.ps1` (non-destructive scratch restore). See `docs/Backup-Restore-Runbook.md`. |
| Tempted to drop/rename a column | Destructive change | **Stop** — requires explicit approval; default to additive, backward-compatible changes. |

## Knowledge pack

| Symptom | Likely cause | Fix |
|---|---|---|
| Base Knowledge search empty | Pack not installed | `./database/seed-professional-knowledge-base.ps1`, then `verify-knowledge-base.ps1` → `KNOWLEDGE-BASE: VERIFIED`. |
| `verify-knowledge-base.ps1` fails on duplicate Uids | Inconsistent pack content | Do not force; obtain a corrected pack. The installer rejects unregistered-source items with **no DB writes**, so existing data is safe. |
| Item missing its sources/limitation note | Expected only if pack incomplete | Re-verify; every curated item carries `src:`/`jur:` tags and a limitation note by design. |
| Install blocked: "unregistered source" | An item references a source not in the registry | Correct the pack's `source-registry.json` references; pack is rejected atomically until fixed. |

## Authentication & access

| Symptom | Likely cause | Fix |
|---|---|---|
| User gets 403 opening a project | Deny-by-default; no grant | Admin grants project access at `/Users`. New users are Viewer with no access until granted. |
| Direct-URL project access denied | IDOR guard working as intended | Expected — server-side enforcement returns 403 and audits `AuthDenied`. Not a bug. |
| First admin can't manage users | Bootstrap admin not set | Set `"Security": { "BootstrapAdmin": "DOMAIN\\admin" }`; provisioned Admin on next login. See `docs/08-Security.md`. |
| Need to know who did what | — | Admin: `/Audit` (append-only; who/what/when/which project/IP/denied). |
| Want SSO / SAML / OIDC | Not supported | Windows (Negotiate) auth only. SSO is a documented gap, not available. |

## Import / ingestion

| Symptom | Likely cause | Fix |
|---|---|---|
| Some files missing from a project | Skipped (binary/oversized/non-UTF-8/malformed) | Open coverage/gap report — skips are bucketed and honest. Not a silent failure. |
| Files show as "gaps" | Unsupported language | Only C#/T-SQL/Python are deeply understood; others are reported gaps, not failures. |
| Import job stuck | Background service or DB issue | Confirm `IngestionBackgroundService` running (log) and DB reachable; re-queue from the wizard. |
| Worried a bad repo will crash the app | — | It won't — robustness pipeline records a skip and continues; the job still reaches Completed. |
| Non-admin can't import | Correct by design | Import is Admin-only and audited (`ImportStarted`/`ImportCompleted`). |

## Optional AI (Ollama / Qdrant)

| Symptom | Likely cause | Fix |
|---|---|---|
| AI features inactive | Ollama/Qdrant not configured or down | **Not an outage** — MSSQL-only mode works fully. To enable: `scripts/ai/check-ollama.ps1`, `scripts/start-qdrant-docker.ps1`, `scripts/setup-ollama.ps1`. |
| Page hangs after enabling AI | Synchronous Qdrant/Ollama call on request path | Revert to cached health (`IServiceHealthCache`); never call external services synchronously in a controller/view. |
| Which models work | — | Optional inference verified with `qwen2.5-coder` / `deepseek-r1`. GPU optional (reference RTX 5070 Ti). |

## Deployment / runtime

| Symptom | Likely cause | Fix |
|---|---|---|
| Core page hangs | External call on request path; heavy query | Find the stall via `RequestTimingMiddleware` (`started` with no `completed`); use cached health + lightweight list queries; never `GroupBy(_ => 1)`. |
| Slow list pages | Materializing large text columns | Project list queries to lightweight `record`s selecting only needed columns (avoid `Content`/`RawText`). |
| Encrypted secrets unreadable after move | `keys/` directory lost | Preserve git-ignored `keys/` across reinstalls; re-enter secrets if lost. |
| Need IIS guidance | — | `docs/Windows-Server-IIS-Deployment-Guide.md`; dry-run install: `scripts/release/install-windows-server-iis-dryrun.ps1`. |
| Need to confirm overall readiness | — | `/Readiness` (≈49% and rising; conservative scoring). See `docs/Readiness-Maturity-Model.md`. |

---

## When in doubt

1. Run the first-response checklist in `docs/Support-Runbook.md`.
2. Read `RequestTimingMiddleware` logs to locate any stall.
3. Confirm MSSQL is reachable and the knowledge base verifies.
4. Remember the honest limits: C#/T-SQL/Python only, syntactic understanding, no SSO, no cross-repo estate
   model, OCR/PDF/forecasting are prototypes, the autonomous workspace is dry-run + allowlisted, and there is
   no proven production deployment or license enforcement yet.
