# Support Runbook

Operational scenarios and resolutions for LocalAIFactory during a pilot. The platform is **local-first and
MSSQL-authoritative**: MSSQL must be reachable, but Ollama and Qdrant are **optional** and the platform must
keep working without them. Most "outages" are either a SQL connectivity problem or a page that is waiting on
something it should not be waiting on.

For symptom→cause→fix tables, see `docs/Troubleshooting-Guide.md`. For deep history of page hangs, see
`docs/07-Troubleshooting.md`.

---

## First-response checklist (run this before anything else)

```powershell
# 1. App responds and core pages are 200 (no 500s, no hangs)
./scripts/release/post-install-healthcheck.ps1 -Url http://localhost:8080

# 2. Knowledge base intact in MSSQL
./database/verify-knowledge-base.ps1

# 3. Build/test/benchmark + artifacts still sound (read-only)
./scripts/poc/verify-poc.ps1 -Fast
```

If all three pass, the platform itself is healthy and the issue is environmental (data, access, or a specific
import). If one fails, jump to the matching scenario below.

---

## Scenario 1 — A page won't load / hangs

- **Confirm scope:** does `/` load? `/Projects`? `/Knowledge`? `/Models`? These **must always load**, even on
  an empty DB and in MSSQL-only mode.
- **Locate the stall:** check the app log for `RequestTimingMiddleware`. A `-> {path} started` line with **no**
  matching `<- {path} {status} in {ms} ms` line is the hung endpoint. The dashboard also logs
  `Dashboard build started/completed`.
- **Most common cause:** something on the request path is calling an external service synchronously, or a
  query is materializing a large text column / using a `GroupBy(_ => 1)`-style aggregate. Health must come
  from the **cached snapshot** (`IServiceHealthCache`), never a live Qdrant/Ollama call.
- **Fix:** if a code change introduced it, revert to cached-health and lightweight list queries. If it is data
  volume, confirm list queries select only the columns the view needs (not full entities with large `Content`/
  `RawText` columns).

---

## Scenario 2 — Database unreachable

- **Symptom:** startup fails, or pages error with a SQL connection failure.
- **Check:** is the SQL instance running and reachable? Is the connection string correct (LocalDB / Express /
  full)? Can the app-pool / run identity authenticate to the DB?

  ```powershell
  sqlcmd -S "(localdb)\MSSQLLocalDB" -d LocalAIFactory -E -Q "SELECT 1;"
  ```

- **Fix:** correct the connection string (env var or git-ignored local override, never committed). Ensure the
  run identity has DB access. Start/repair the SQL instance. Re-run the first-response checklist.

---

## Scenario 3 — Migration issue

- **Symptom:** schema out of date, or a migration error on startup.
- **Note:** the app **auto-migrates and seeds on startup**. Most migration problems are an unreachable DB
  (Scenario 2) or an interrupted first run.
- **Fix:**

  ```powershell
  ./database/apply-migrations.ps1
  # or, explicitly:
  dotnet ef database update --project src/LocalAIFactory.Data --startup-project src/LocalAIFactory.Web
  ```

- **Do not** make destructive schema changes (drop/rename/lossy/delete) without explicit approval. Default to
  additive, backward-compatible changes. See `docs/Upgrade-Rollback-Runbook.md`.

---

## Scenario 4 — Knowledge pack not installed / fails verification

- **Symptom:** Base Knowledge search returns nothing, or `verify-knowledge-base.ps1` reports failures.
- **Check / fix:**

  ```powershell
  ./database/seed-professional-knowledge-base.ps1   # idempotent; propose-never-overwrite
  ./database/verify-knowledge-base.ps1              # expect: KNOWLEDGE-BASE: VERIFIED
  ```

- The seeder is idempotent and never overwrites existing curated items. If verification fails on **unique
  Uids** or **unregistered source**, the pack content is inconsistent — the installer rejects an item that
  references an unregistered source **with no DB writes**, so the existing data is safe; obtain a corrected
  pack rather than forcing it.

---

## Scenario 5 — Import failed or partial

- **Expected behaviour:** a bad repository **never crashes** the platform. Bad files are recorded as **skips**
  (binary, oversized, non-UTF-8, malformed, unsupported language) and the job still reaches **Completed**.
- **Check:** open the project's coverage/gap report and the import job status. Skips are bucketed and shown
  honestly — there are no silent zeros. The `IngestionBackgroundService` drains the queue in the background.
- **If the job is stuck:** confirm the background service is running (app log) and the DB is reachable
  (Scenario 2). Re-queue the import from the wizard.
- **If files you expected are "gaps":** confirm they are C#/T-SQL/Python — other languages are **reported
  gaps**, not failures. This is correct behaviour, not a bug.

---

## Scenario 6 — Optional AI unavailable (Ollama / Qdrant)

- **This is not an outage.** Ollama and Qdrant are **optional** and config-gated. The platform must work fully
  in **MSSQL-only mode** — deterministic understanding, impact analysis, coverage, and Base Knowledge all work
  without them.
- **If configured but down:** health status comes from the cached snapshot, so pages still load. AI-assisted
  features degrade gracefully.
- **Check (only if the customer wants optional AI working):**

  ```powershell
  ./scripts/ai/check-ollama.ps1            # Ollama reachable? (verified models: qwen2.5-coder / deepseek-r1)
  ./scripts/start-qdrant-docker.ps1        # bring up Qdrant (REST-only, optional)
  ```

- **Fix:** start the service or correct its config flag. Never reintroduce a synchronous Qdrant/Ollama call on
  the request path to "fix" this — that causes Scenario 1.

---

## Collecting diagnostics

Gather these for any escalation:

1. **App logs** — including `RequestTimingMiddleware` lines around the incident (the `started`/`completed`
   pair, plus any Warning > 1 s) and the `Dashboard build` timing.
2. **Health check output** — `./scripts/release/post-install-healthcheck.ps1 -Url <url>` (wraps
   `deploy/scripts/health-check.ps1`).
3. **Knowledge verification** — `./database/verify-knowledge-base.ps1` output.
4. **POC verification** — `./scripts/poc/verify-poc.ps1` (or `-Fast`) output; add `-AppUrl <url>` for live
   HTTP checks.
5. **Environment facts** — SQL flavour + reachability (`sqlcmd ... SELECT 1`), whether Ollama/Qdrant are
   configured, OS/.NET version.
6. **Audit excerpt** (Admin) — relevant `/Audit` events (who/what/when/which project/denied).

---

## Escalation

- **Tier 0 (admin self-service):** first-response checklist + the matching scenario above + the
  `docs/Troubleshooting-Guide.md` table.
- **Tier 1 (implementing engineer / consultant):** business-hours support during the pilot. Provide the full
  diagnostics bundle above. There is **no 24/7 SLA** in the pilot tier.
- **Tier 2 (engineering):** for reproducible defects — include the failing `verify-poc` / health-check / DB
  output and the exact `RequestTimingMiddleware` evidence of the stall or failure.

> Honest boundary: there is no proven production deployment, no SSO, and no commercial support tier yet. Pilot
> support is best-effort, business-hours, and evidence-driven.
