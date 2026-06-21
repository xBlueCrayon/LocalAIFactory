# Production Deployment Drill Pack

> **Authoritative source of truth:** `MASTER_VISION.md`. If anything here conflicts with it,
> `MASTER_VISION.md` prevails. This document is operational procedure, not a vision statement.

## Purpose

This is the **executable procedure** for performing a production-shaped deployment of LocalAIFactory
to a Windows Server / IIS + SQL host. It documents, step by step, the operator-gated drill pack under
`scripts/deployment-drill/` so that a real deployment can be run **later**, with an operator present,
on an approved host.

### Honesty banner — read this first

This **production / IIS** procedure has **NOT** been executed on a real production server. The drill
scripts are **dry-run by default** and require `-Execute` for any real change; the read-only steps (`00`,
`01`, `06`, `08`, and the new `09`) change nothing at all. As of this writing:

- No representative production host has been provisioned.
- **Docker is not installed** on the build host, and **IIS is not installed** on the build host.

> **However — a Mode C deployment WAS executed (2026-06-21).** Because IIS is unavailable, the strongest
> truthful deployment was performed: the **published app** running against **SQL Server Express 2022**
> (fresh `LocalAIFactory_DeploymentProof` DB, 14 migrations + 4 packs/438 items, 13 routes 200, 0 HTTP
> 500s, `09-post-deploy-healthcheck` PASS, rollback proven). See `reports/DEPLOYMENT_PUBLISHED_APP_PROOF.md`.
> A new read-only step **`09-post-deploy-healthcheck.ps1`** certifies a deployed endpoint (HTTP + DB +
> pack counts + migrations). The IIS (`-Execute`) path below remains the documented next step (Mode A).

What *has* been verified is that the dry-run pass runs cleanly (see "Verified dry-run facts" below).
That proves the **procedure and the scripts**, not a fresh-server production deployment. Do not read
this document as a claim that a server has been deployed.

### Verified dry-run facts (build host)

These were observed on the build/development host — **not** a production target:

- Host: `DESKTOP-M1HANKN`, Windows 11 Pro, .NET `10.0.301`.
- Steps `00`–`05` and `07` were run as **dry-runs** and each exited `0`.
- `00-prerequisites` reported **6/6 present**.
- `01-check-host` reported: **IIS installed = False**, SQL services **`MSSQL$SQLEXPRESS` present**.
- `05-deploy-package` found the release ZIP `LocalAIFactory-release-20260621-040519.zip`.
- Release ZIP size: **16,997,982 bytes**.
- Release ZIP SHA-256: `eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b`.

`06-run-healthchecks` and the page-health portion of `08-capture-evidence` were not exercised to
green because they require a *running deployed app*, which does not exist on the build host.

---

## 1. Safety contract

The drill pack is designed so that running the wrong thing cannot damage a host:

- **Dry-run by default.** Steps `02`–`05` and `07` require an explicit `-Execute` switch to make any
  change. Without it they only print the plan. Steps `00`, `01`, `06`, `08` are **read-only** and have
  no `-Execute` switch.
- **Operator-gated.** The `-Execute` steps print that they must be run **elevated**, on an
  **approved host**, with an **operator present**. They never silently reconfigure a system.
- **Non-destructive.** No script drops a database, deletes user data, or deletes the current app
  without a verified backup to restore from. The DB restore in `07` is **never automated** — it stays
  manual even under `-Execute`.
- **No DB drops.** `03` calls a create-if-absent script; it never drops the database.
- **Repo-root runnable.** Every script resolves its paths relative to itself (`$PSScriptRoot`), so it
  works from any current directory, including the repo root.
- **Delegates, never duplicates.** Where a real installer or verifier already exists under
  `scripts/release/` or `database/`, the drill scripts **call it** rather than re-implementing it
  (for example `database/create-sqlexpress-db.ps1`, `database/verify-full-install.ps1`,
  `scripts/release/install-windows-server-iis-dryrun.ps1`,
  `scripts/release/post-install-healthcheck.ps1`).

---

## 2. The nine steps (00–08)

Run them in numeric order. The table mirrors `scripts/deployment-drill/README.md`; the prose under
each row expands on it.

| #  | Script | Default mode | What it does (default) | What `-Execute` would do |
|----|--------|--------------|------------------------|--------------------------|
| 00 | `00-prerequisites.ps1` | read-only | Checks tools/permissions a real deploy needs. | (no `-Execute`) |
| 01 | `01-check-host.ps1` | read-only | Reports host facts. | (no `-Execute`) |
| 02 | `02-install-prereqs-dryrun.ps1` | dry-run | Prints the prereq install plan. | Prints operator instructions; installs nothing by design. |
| 03 | `03-setup-sql-express-dryrun.ps1` | dry-run | Prints the DB create + verify plan. | Calls `database/create-sqlexpress-db.ps1` (create-if-absent). |
| 04 | `04-setup-iis-dryrun.ps1` | dry-run | Prints the IIS plan; delegates to the dry-run installer. | Prints the apply instructions; defers destructive site changes to the operator. |
| 05 | `05-deploy-package-dryrun.ps1` | dry-run | Shows the package copy plan. | Extracts the newest release ZIP and copies `app/` to the target. |
| 06 | `06-run-healthchecks.ps1` | read-only | Probes core pages + DB + KB; non-zero on any failure. | (no `-Execute`) |
| 07 | `07-run-rollback-dryrun.ps1` | dry-run | Prints the rollback plan. | Restores the previous `app/` from a timestamped backup; DB restore stays manual. |
| 08 | `08-capture-evidence.ps1` | read-only | Collects host facts, page health, DB/KB verification, support bundle. | (no `-Execute`) |

### 00 — Prerequisites (read-only)

Verifies, without changing anything, that the tools a real deployment needs are present:
`dotnet` (the .NET SDK/runtime), `sqlcmd`, PowerShell 5.1+/7, whether the shell is **elevated**
(needed for the later `-Execute` steps), at least **5 GB free on `C:`**, and the presence of
`Get-WindowsOptionalFeature` (the IIS feature manager). It prints an `OK`/`MISS` line per check and a
final `N present, M missing` summary. It installs nothing.

### 01 — Host facts (read-only)

Captures machine name, OS caption/version, RAM (GB), logical CPU count, the `dotnet --version`,
whether the `W3SVC` (IIS) service exists, the list of `MSSQL*` services, and the primary non-loopback
IPv4 address. This output belongs in the deployment evidence record (step `08`).

### 02 — Install prerequisites (dry-run; `-Execute` is a deliberate no-op)

In dry-run it **prints** what a real run would install and downloads/installs nothing:

1. **ASP.NET Core 10 Runtime + Hosting Bundle** (required for IIS) from the official Microsoft .NET
   site — a missing Hosting Bundle is the classic cause of IIS **500.31 / 502.5** (see
   `docs/research/COMMUNITY_FAILURE_PATTERNS.md`).
2. **SQL Server** (Express for a pilot, or a full instance for production), with TCP / named-pipes
   enabled as required.
3. **`sqlcmd`** command-line tools for verification.
4. *(Optional)* Ollama for local AI; *(optional)* Node + Playwright for screenshot regeneration.

`-Execute` is intentionally a placeholder: prerequisite installation must be done by the operator,
deliberately, from official Microsoft sources, on the approved host. The script never silently
installs anything.

### 03 — SQL Express database (dry-run; `-Execute` creates the DB)

Parameters: `-Server` (default `.\SQLEXPRESS`), `-Database` (default `LocalAIFactory`).
Dry-run shows the plan: it would run `database/create-sqlexpress-db.ps1 -Instance <Server>
-Database <Database>` (**create-if-absent, never drops**); the app then seeds all packs on first
start; verification is via `database/verify-full-install.ps1`. With `-Execute` it actually invokes
`database/create-sqlexpress-db.ps1`. (SQL Express has no backup compression; that is handled with an
opt-in `-Compress` elsewhere.)

### 04 — IIS site + app pool (dry-run; `-Execute` is operator-gated)

Parameters: `-SiteName` (default `LocalAIFactory`), `-Port` (default `8080`), `-PhysicalPath`
(default `C:\inetpub\LocalAIFactory`). Dry-run prints the IIS plan and, when present, delegates to
`scripts/release/install-windows-server-iis-dryrun.ps1`. The plan:

1. Install the **ASP.NET Core 10 Hosting Bundle** (else IIS 500.31/502.5).
2. Create app pool (No Managed Code; identity = a domain/service account with a **least-privilege SQL
   login**).
3. Create the site bound to the chosen port with the published physical path.
4. Grant the app-pool identity read/execute on the path and a SQL login mapped to
   `db_datareader` / `db_datawriter` + `EXECUTE`.
5. Point the appsettings connection string at the target SQL instance.

Even with `-Execute`, this drill **does not silently reconfigure IIS** — destructive site changes are
deferred to the operator.

### 05 — Deploy the package (dry-run; `-Execute` copies `app/`)

Parameters: `-PhysicalPath` (default `C:\inetpub\LocalAIFactory`), `-Zip` (release ZIP; if omitted it
selects the newest `LocalAIFactory-release-*.zip` under `.tmp-release/`). If no release ZIP is found,
it tells you to run `scripts/release/build-release.ps1` + `package-release.ps1` and stops. Dry-run
prints: *stop site (if any) → extract ZIP → copy `app/` → start site*. With `-Execute` it expands the
ZIP to a temp folder, ensures the target path exists, copies `app/*` into it, and cleans up the temp
folder. It **never deletes user data and never touches the database**. Run `03` before this.

### 06 — Health checks (read-only)

Parameters: `-AppUrl` (default `http://localhost:8080`), `-Server`, `-Database`. Probes `GET` on
`/`, `/Support`, `/Readiness`, `/BaseKnowledge` (expecting `200`), then runs
`database/verify-full-install.ps1` and, if present, delegates to
`scripts/release/post-install-healthcheck.ps1`. It prints `HEALTHCHECK: 1` on success / `0` on
failure and **exits non-zero on any failure**. It requires a running deployed app.

### 07 — Rollback (dry-run; `-Execute` restores the previous `app/`)

See §5 below. Dry-run prints the rollback plan and the app/DB backups it found, and changes nothing.

### 08 — Capture evidence (read-only)

See §6 below. Writes an evidence folder; changes nothing on the host or in the database.

---

## 3. End-to-end procedure on a real host

> Run from the repository root. The dry-run pass is safe anywhere; the `-Execute` pass must be run
> **elevated, on the approved host, with an operator present**.

### Step A — Pre-flight (read-only)

```powershell
.\scripts\deployment-drill\00-prerequisites.ps1
.\scripts\deployment-drill\01-check-host.ps1
```

Resolve every `MISS` from `00` before continuing (especially the Hosting Bundle and a least-privilege
SQL login — see §4).

### Step B — Dry-run pass (changes nothing)

```powershell
.\scripts\deployment-drill\02-install-prereqs-dryrun.ps1
.\scripts\deployment-drill\03-setup-sql-express-dryrun.ps1
.\scripts\deployment-drill\04-setup-iis-dryrun.ps1
.\scripts\deployment-drill\05-deploy-package-dryrun.ps1
```

Read each plan and confirm the parameters (site name, port, physical path, SQL instance/database,
release ZIP) match the target. Nothing has changed on the host yet.

### Step C — `-Execute` pass (elevated, operator present)

Perform `02` manually (install the Hosting Bundle and SQL from official sources), then:

```powershell
.\scripts\deployment-drill\03-setup-sql-express-dryrun.ps1 -Server '.\SQLEXPRESS' -Database 'LocalAIFactory' -Execute
.\scripts\deployment-drill\04-setup-iis-dryrun.ps1 -SiteName 'LocalAIFactory' -Port 8080 -PhysicalPath 'C:\inetpub\LocalAIFactory' -Execute
.\scripts\deployment-drill\05-deploy-package-dryrun.ps1 -PhysicalPath 'C:\inetpub\LocalAIFactory' -Execute
```

Start the IIS site, then let the app start once so it migrates and seeds the database.

### Step D — Verify

```powershell
.\scripts\deployment-drill\06-run-healthchecks.ps1 -AppUrl 'http://localhost:8080' -Server '.\SQLEXPRESS' -Database 'LocalAIFactory'
```

This must report `HEALTHCHECK: 1` and exit `0`. If it fails, investigate before sign-off; roll back
with `07` if necessary.

### Step E — Evidence

```powershell
.\scripts\deployment-drill\08-capture-evidence.ps1 -AppUrl 'http://localhost:8080' -Server '.\SQLEXPRESS' -Database 'LocalAIFactory'
```

Attach the resulting evidence folder to the deployment sign-off record (see §6).

---

## 4. Pre-flight requirements for the real run

- **ASP.NET Core 10 Runtime + Hosting Bundle** must be installed on the host. Without it, IIS returns
  **500.31** or **502.5** for the ASP.NET Core app. Install it from the official Microsoft .NET site
  before `05`.
- **Least-privilege SQL login for the app-pool identity.** Do not run the app under `sa`. Create a
  dedicated login (Windows-auth / trusted connection preferred) mapped to `db_datareader` +
  `db_datawriter` + `EXECUTE` on the `LocalAIFactory` database.
- **A verified DB backup before deploy.** Take and verify a SQL backup of any pre-existing database so
  that `07` has something to restore from. The backup-take step is part of the handover walkthrough.
- **Connection string pointed at the target instance.** The `appsettings` connection string must
  reference the target SQL instance (`Trusted_Connection` or the dedicated login). MSSQL is the
  authoritative store; the app must work with only SQL Server present.
- **No secrets in committed config.** Connection strings with credentials live in environment
  variables or a git-ignored local override — never in committed config. Data-protection keys live in
  the git-ignored `keys/` folder.

---

## 5. Rollback procedure (07)

`07-run-rollback-dryrun.ps1` parameters: `-PhysicalPath` (default `C:\inetpub\LocalAIFactory`),
`-PreviousApp` (default: newest `*.bak-app` directory under the physical path), `-DbBackup` (optional
SQL `.bak`; never auto-selected), `-Execute`.

The plan it prints:

1. Stop the IIS site / app pool so no requests hit a half-rolled-back app.
2. Restore the previous published `app/` from its timestamped backup (taken before `05` deployed).
3. Restart the site and run `06-run-healthchecks.ps1` to confirm the previous version is healthy.
4. **(DB, only if a migration must be reverted)** restore the SQL backup taken earlier in the handover
   walkthrough.

Behaviour:

- In dry-run it makes **no changes** and reports the app/DB backups it found.
- With `-Execute`, if **no previous app backup** is found it **refuses to delete the current app and
  aborts** (exit `1`). If a backup exists, it copies it back over the physical path. The operator then
  starts the site and re-runs `06`.
- **The database restore is never automated**, even under `-Execute`. It stays manual and is performed
  with the operator present via `database/restore-database.ps1 -BackupFile <path>`.

---

## 6. Evidence to capture for sign-off (08)

`08-capture-evidence.ps1` (read-only) writes the following under the evidence root
(`-OutDir`, default `.tmp-deployment-evidence`):

- **`host.json`** — captured-UTC timestamp, computer name, OS caption, `dotnet --version`, app URL,
  SQL server, and database.
- **`page-health.json`** — the `200`/non-`200` status of `/`, `/Support`, `/Readiness`,
  `/BaseKnowledge`.
- **`verify-full-install.txt`** — output (and exit code) of `database/verify-full-install.ps1`, the
  DB/knowledge-base verifier.
- **`support-bundle.txt`** — output of `scripts/support/export-support-bundle.ps1`, if that exporter
  is present.

Nothing on the host or in the database is modified. Attach the whole folder to the sign-off record.

---

## 7. What this proves — and what it does not

**Proves:**

- The deployment procedure exists, is repeatable, and is documented step by step.
- The dry-run pass runs cleanly: steps `00`–`05` and `07` exit `0` on the build host, `00` reports
  6/6 prerequisites present, `01` reports host facts correctly, and `05` locates a real, hashed
  release ZIP (16,997,982 bytes, SHA-256
  `eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b`).
- The scripts are non-destructive and operator-gated by construction.

**Does NOT prove:**

- A real production deployment. No representative production host, SQL Express deployment, full SQL
  Server, or Docker target has been deployed in this work.
- That IIS or Docker work end-to-end on a target — **IIS is not installed** and **Docker is not
  installed** on the build host, so `06`/`08` page-health could not be driven to green here.
- A fresh-server proof. The verified facts come from a development host (`DESKTOP-M1HANKN`), not a
  provisioned server.

Treat this as the procedure to execute later, not as a completed deployment.

### Related documents

- `docs/ROADMAP_TO_TRUE_20_OF_20.md` — the remaining work to turn this drill into a real, evidenced
  production deployment (referenced as the forward roadmap; create/keep it in sync as that work lands).
- `docs/CUSTOMER_HANDOVER_WALKTHROUGH.md` — the operator-facing handover walkthrough, including the
  pre-deploy backup step that `07`'s DB rollback restores from.
- `scripts/deployment-drill/README.md` — the short reference this document expands on.
