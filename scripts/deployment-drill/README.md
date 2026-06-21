# Deployment Drill Pack

An **operator-gated, dry-run-by-default** walkthrough of a production-shaped deployment of
LocalAIFactory to a Windows Server / IIS + SQL host. Every script in this folder is **safe to run from
the repo root with no arguments**: it changes nothing unless you pass `-Execute` (and even then the
most destructive steps stay manual and confirmation-gated).

> **This is not a proof of a real deployment.** No representative production host, SQL Express, full
> SQL Server, or Docker target has been deployed in this work. These scripts are the *executable
> procedure* to run later, with an operator present, on an approved host. See
> [`../../docs/Production-Deployment-Drill-Pack.md`](../../docs/Production-Deployment-Drill-Pack.md)
> and [`../../docs/ROADMAP_TO_TRUE_20_OF_20.md`](../../docs/ROADMAP_TO_TRUE_20_OF_20.md).

## Scripts (run in order)

| # | Script | Default | What it does |
|---|--------|---------|--------------|
| 00 | `00-prerequisites.ps1` | read-only | Checks tools/permissions a real deploy needs (.NET, sqlcmd, admin, disk, IIS features). |
| 01 | `01-check-host.ps1` | read-only | Reports host facts (OS, RAM, disk, existing IIS site / SQL instance). |
| 02 | `02-install-prereqs-dryrun.ps1` | dry-run | Shows the prereq install plan (Hosting Bundle, SQL tools). `-Execute` to apply. |
| 03 | `03-setup-sql-express-dryrun.ps1` | dry-run | Shows the SQL Express + database creation plan. `-Execute` to apply. |
| 04 | `04-setup-iis-dryrun.ps1` | dry-run | Shows the IIS site/app-pool plan. `-Execute` to apply (defers destructive site changes). |
| 05 | `05-deploy-package-dryrun.ps1` | dry-run | Shows the package copy plan. `-Execute` extracts the newest release ZIP and copies `app/`. |
| 06 | `06-run-healthchecks.ps1` | read-only | Probes core pages + DB + knowledge base. Returns non-zero on any failure. |
| 07 | `07-run-rollback-dryrun.ps1` | dry-run | Shows the rollback plan. `-Execute` restores the previous `app/`; DB restore stays manual. |
| 08 | `08-capture-evidence.ps1` | read-only | Collects host facts, page health, DB/KB verification, support bundle into an evidence folder. |
| 09 | `09-post-deploy-healthcheck.ps1` | read-only | Certifies a deployed endpoint: HTTP + DB pack/item counts + migrations + mode. (Used for Mode C and Mode A.) |
| 10 | `10-iis-mode-a-deploy.ps1` | dry-run | **Mode A:** deploy the published app under IIS (app pool + site + web.config + ACL). `-Execute` applies. |
| 11 | `11-iis-mode-a-healthcheck.ps1` | read-only | **Mode A:** HTTP + SQL + IIS site/app-pool state health check; writes JSON evidence to `.tmp-*`. |
| 12 | `12-iis-mode-a-rollback-dryrun.ps1` | dry-run | **Mode A:** rollback. `-StopOnly` stops site+pool (frees port); `-Execute` removes site+pool. |

> **Mode A (real IIS) was executed on 2026-06-21** — IIS enabled + ASP.NET Core Hosting Bundle/ANCM, app
> served through IIS against SQL Express with a least-privilege app-pool login. Evidence: `docs/reports/MODE_A_IIS_*`.
> Companion DB scripts: `database/setup-iis-sqlexpress-proof.ps1`, `grant-iis-apppool-sql-access.ps1`, `verify-iis-sqlexpress-proof.ps1`.

## Safety contract

- **Dry-run by default.** 02–05 and 07 require `-Execute` to make any change; 00, 01, 06, 08 are read-only.
- **Non-destructive.** No script drops a database, deletes user data, or deletes the current app
  without a verified backup to restore from. The DB restore in 07 is never automated.
- **Operator-gated.** `-Execute` steps print that they must be run elevated, on an approved host,
  with an operator present.
- **Repo-root runnable.** Each script resolves paths relative to itself, so it works from any cwd.
- **Delegates, never duplicates.** Where a real installer/verifier already exists under
  `scripts/release` or `database/`, these drill scripts call it rather than re-implementing it.

## Quick dry run (changes nothing)

```powershell
.\scripts\deployment-drill\00-prerequisites.ps1
.\scripts\deployment-drill\01-check-host.ps1
.\scripts\deployment-drill\02-install-prereqs-dryrun.ps1
.\scripts\deployment-drill\03-setup-sql-express-dryrun.ps1
.\scripts\deployment-drill\04-setup-iis-dryrun.ps1
.\scripts\deployment-drill\05-deploy-package-dryrun.ps1
.\scripts\deployment-drill\06-run-healthchecks.ps1   # needs a running app to return 200s
.\scripts\deployment-drill\07-run-rollback-dryrun.ps1
.\scripts\deployment-drill\08-capture-evidence.ps1   # needs a running app for page health
```

To actually deploy on an approved host, re-run 02→05 elevated with `-Execute`, then 06 to verify.
