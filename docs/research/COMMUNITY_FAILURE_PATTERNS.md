# Community Failure Patterns

Real-world failure patterns drawn from community sources (Stack Overflow tags such as
`asp.net-core`, `iis`, `ef-core`, `sql-server`, `sqlcmd`, `powershell`, `openid-connect`, `docker`,
`playwright`; GitHub issue trackers `dotnet/aspnetcore`, `dotnet/efcore`, `microsoft/playwright`,
`ollama/ollama`, `qdrant/qdrant`).

> **Label / honesty note:** these are **failure-pattern signals**, not authoritative truth. Each has
> been verified against the official documentation cited in
> `OFFICIAL_DEPLOYMENT_SECURITY_REFERENCES.md`. Where web search was unavailable during this pass, a
> pattern is included only when it is independently confirmed by the official doc that explains the
> same mechanism; such items are marked *(confirmed via official doc; community quote not re-fetched)*.

---

## 1. IIS: app pool identity can't log in to SQL — "Login failed for user 'IIS APPPOOL\\<pool>'"

- **Pattern:** App runs fine under `dotnet run` (developer identity) but throws a SQL login failure
  once deployed to IIS.
- **Why it happens:** Under IIS the process runs as the **app-pool identity**, a different Windows
  principal than the developer. With Integrated Security / trusted connection that identity has no SQL
  login until one is created and granted a db role. *(Confirmed via official IIS host/deploy doc.)*
- **Exposed?** **Yes** — LocalAIFactory uses Windows-auth trusted connections, so the IIS deploy path
  depends entirely on the app-pool identity having a least-privilege SQL login.
- **Mitigation:** Document/automate creating a SQL login for the chosen app-pool identity with
  `db_datareader` + `db_datawriter` (and schema rights only if startup `Migrate()` runs in that
  environment). Verify with `scripts/diagnostics/sql-health-check.ps1` run **as that identity**.

## 2. IIS: 502.5 / 500.31 — "specified framework was not found"

- **Pattern:** Site returns 500.31 or 502.5 immediately on first request after deploy.
- **Why it happens:** The published app targets a .NET shared-framework version not installed on the
  server, or the **Hosting Bundle** is missing/corrupt, or x86/x64 bitness mismatch (500.32).
  *(Confirmed via official troubleshoot-azure-iis doc.)*
- **Exposed?** **Yes** for the IIS path; not for local Kestrel.
- **Mitigation:** Ensure the .NET 10 Hosting Bundle is installed before binding the site; keep publish
  bitness aligned with the app pool ("Enable 32-Bit Applications" = False for x64). Covered by
  `scripts/release/install-windows-server-iis-dryrun.ps1` + `post-install-healthcheck.ps1`.

## 3. IIS: 500.34/500.35 — mixed or multiple in-process apps in one pool

- **Pattern:** A second ANCM app added to an existing app pool breaks both.
- **Why it happens:** ANCM does not allow mixed hosting models, nor multiple in-process apps, in a
  single worker process. *(Confirmed via official doc.)*
- **Exposed?** **Partial** — only if someone co-hosts LocalAIFactory with another ASP.NET Core app in
  the same pool.
- **Mitigation:** Give LocalAIFactory its **own** dedicated app pool.

## 4. EF Core: `Database.Migrate()` at startup race on multi-instance deploys

- **Pattern:** Two app instances starting together both run migrations; one fails with
  "There is already an object named ..." or leaves the schema half-applied.
- **Why it happens:** Pre-EF-Core-9 there was no cross-instance lock. *(Confirmed via official EF
  "Applying Migrations" doc, which also documents the EF Core 9+ automatic DB-wide lock.)*
- **Exposed?** **Low/partial.** LocalAIFactory calls `db.Database.Migrate()` at startup
  (`src/LocalAIFactory.Web/Program.cs:82`) **but** is single-instance/local-first **and** runs on
  EF Core 9+, which auto-acquires a database-wide migration lock. The classic race is largely mitigated.
- **Mitigation:** Keep single-instance for the local install; for any scaled-out environment, switch to
  an out-of-band **idempotent script** or **migration bundle** (`dotnet ef migrations script
  --idempotent` / `dotnet ef migrations bundle`).

## 5. EF Core: `EnsureCreated()` then `Migrate()` — migrations silently broken

- **Pattern:** Schema created but `__EFMigrationsHistory` empty; later `Migrate()` fails.
- **Why it happens:** `EnsureCreated()` bypasses migrations entirely; calling it before `Migrate()` is
  documented to fail. *(Confirmed via official doc.)*
- **Exposed?** **No** — the Web project uses `Database.Migrate()` only; no `EnsureCreated()` on the
  request/startup path.
- **Mitigation:** None needed; keep the current pattern.

## 6. SQL Express: `BACKUP ... WITH COMPRESSION` not supported

- **Pattern:** Backup script that works on Standard/Enterprise errors out on Express.
- **Why it happens:** Backup compression is Enterprise/Standard/Developer only. *(Confirmed via official
  backup-compression doc.)*
- **Exposed?** **Yes** by edition, but **already handled** — the demo installers target Express/LocalDB
  and must not emit `WITH COMPRESSION`.
- **Mitigation:** Keep backup T-SQL compression-free for Express/LocalDB; note that any compressed
  backup taken on a higher edition can still be *restored* onto these.

## 7. sqlcmd: wrong instance / missing tool / `-E` identity mismatch

- **Pattern:** `sqlcmd` reports "server not found" or "Login failed", or the script can't find sqlcmd.
- **Why it happens:** LocalDB is `(localdb)\MSSQLLocalDB`, Express is `.\SQLEXPRESS`; `-E` uses the
  *current* Windows identity which may lack rights; the `sqlcmd` tool may not be installed.
  *(Mechanism confirmed via SQL/sqlcmd docs; repo behavior verified.)*
- **Exposed?** **Partial** — handled gracefully: `sql-health-check.ps1` checks for `sqlcmd` and exits 0
  with guidance if absent, and uses `-b` to surface non-zero exit codes.
- **Mitigation:** Pass the correct `-Server`; run as the identity that owns the SQL login.

## 8. OpenID Connect: redirect-URI mismatch (AADSTS50011)

- **Pattern:** Sign-in fails with "redirect URI specified in the request does not match".
- **Why it happens:** The registered Entra redirect URI must match scheme/host/port/path exactly;
  `http` vs `https`, a port change, or a trailing slash breaks it. *(Confirmed via official Entra
  tutorial — default `/signin-oidc`.)*
- **Exposed?** **Partial / conditional** — LocalAIFactory uses Windows-auth RBAC by default; only
  relevant if an Entra/OIDC option is introduced.
- **Mitigation:** If added, register `https://<host>/signin-oidc` and `/signout-callback-oidc`, keep the
  client secret out of committed config (prefer a certificate).

## 9. Docker Desktop on Windows: "WSL2 not installed" / virtualization disabled

- **Pattern:** Qdrant won't start because Docker won't start.
- **Why it happens:** Docker Desktop needs the WSL2 backend (or Hyper-V) and hardware virtualization
  enabled in BIOS. *(Confirmed via official Docker Windows install doc.)*
- **Exposed?** **Partial** — only the **optional** Qdrant path needs Docker. MSSQL-only mode must work
  with no Docker (CLAUDE.md rule 3).
- **Mitigation:** Keep Qdrant optional and degrade gracefully; surface "Docker/WSL2 required for Qdrant"
  rather than blocking the app. `start-qdrant-docker.ps1` is opt-in.

## 10. Playwright: headless screenshot flakiness / `networkidle` hangs

- **Pattern:** Screenshots intermittently blank or the run hangs waiting for network idle; or browsers
  aren't installed in a clean environment.
- **Why it happens:** `networkidle` is discouraged for screenshots (a long-poll or keep-alive never
  goes idle); browsers must be installed explicitly via `npx playwright install`. *(Confirmed via
  official Playwright CI doc; `networkidle` discouragement is long-standing Playwright guidance.)*
- **Exposed?** **Partial** — `scripts/docs/capture-screenshots.ps1` drives Playwright for docs.
- **Mitigation:** Pin `npx playwright install chromium` as a prereq; wait on an explicit element or
  `load`, not `networkidle`; on Windows headless Chromium needs no Xvfb.

## 11. Ollama: "connection refused" on 11434

- **Pattern:** App can't reach Ollama; calls fail with connection refused.
- **Why it happens:** The Ollama server isn't running, or `OLLAMA_HOST` differs from what the client
  expects. *(Confirmed via official Ollama FAQ.)*
- **Exposed?** **Yes**, but by design Ollama is optional and health is cached off the request path.
- **Mitigation:** Probe with `scripts/diagnostics/ollama-health-check.ps1`; never call Ollama
  synchronously in a controller/view (CLAUDE.md rule 5); MSSQL-only mode works without it.

## 12. Qdrant: probing the wrong endpoint / Docker not up

- **Pattern:** Health probe to `/` or `/health` returns unexpected results, or REST refused.
- **Why it happens:** Qdrant readiness is `/readyz` (health `/healthz`) on 6333; if Docker isn't
  running the port is closed. *(Confirmed via official Qdrant installation doc.)*
- **Exposed?** **Partial** — Qdrant is optional, REST-only, and health is cached.
- **Mitigation:** Probe `http://localhost:6333/readyz`; degrade gracefully to MSSQL-only knowledge when
  Qdrant is absent (`projectId=0` = global knowledge).
