# Deployment Guide — LocalAIFactory

An overview of every supported deployment mode with a **decision table** to pick one, then links to
the detailed guide for that mode. This document does not duplicate the engine-specific guides — it
routes you to them.

> MSSQL is the source of truth; Ollama (inference), Qdrant (vectors), and a GPU are **optional** and
> degrade gracefully. Every create script is **create-if-absent** (never drops). The IIS path is a
> dry-run runbook, not an unattended production deployer. The platform makes no commercial-GA or
> compliance claim. See [`Known-Limitations.md`](Known-Limitations.md).

---

## 1. Decision table

| Mode | Best for | SQL engine | AI | Production-grade | Guide |
|---|---|---|---|---|---|
| **LocalDB demo** | Developer / demo, one machine | `(localdb)\MSSQLLocalDB` | Optional | No | [`FINAL_LOCAL_DEPLOYMENT_GUIDE.md`](FINAL_LOCAL_DEPLOYMENT_GUIDE.md) |
| **SQL Express pilot** | Small controlled pilot | `.\SQLEXPRESS` | Optional | Pilot | [`SQL-Express-Pilot-Deployment.md`](SQL-Express-Pilot-Deployment.md) |
| **Full SQL Server** | Production-style, separate DB instance | `localhost` / your host | Optional | Yes (operator-run) | [`Full-SQL-Server-Deployment.md`](Full-SQL-Server-Deployment.md) |
| **Windows Server / IIS** | Hosted web app behind IIS | Any of the above | Optional | Operator-gated | [`Windows-Server-IIS-Deployment-Guide.md`](Windows-Server-IIS-Deployment-Guide.md) |
| **Docker (compose)** | Containerised host | In-container or external MSSQL | Optional | **Unverified here** | [`Docker-Deployment-Guide.md`](Docker-Deployment-Guide.md) |
| **MSSQL-only / air-gapped** | No internet / no AI | Any MSSQL engine | None | Yes | [`Offline-Mode-Guide.md`](Offline-Mode-Guide.md) |

All modes share the same database contract (create-if-absent, additive migrations, idempotent pack
seeding) — see [`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md).

---

## 2. How to choose

- **Just want to see it run?** LocalDB demo — one create script, run, the app auto-seeds all 4
  knowledge packs (438 items).
- **Running a real but contained pilot?** SQL Express pilot. Note Express has size/RAM caps and does
  not support backup `COMPRESSION`.
- **Pilot or production on a managed SQL estate?** Full SQL Server with a least-privilege service
  account and a real backup schedule.
- **Need it behind IIS?** Use the IIS guide; its helper is dry-run by default and operator-gated for
  the site/app-pool steps.
- **Containerised?** The Docker compose files exist as a reference, but **Docker is not installed on
  the build host**, so this path is unverified here.
- **Air-gapped / no AI?** MSSQL-only mode runs the full system of record without internet, Ollama,
  Qdrant, or a GPU.

---

## 3. Common path (every mode)

1. **Prerequisites** — .NET 10 SDK (or the ASP.NET Core 10 runtime for framework-dependent IIS),
   `sqlcmd`, one SQL Server engine. ([`Industrial-Installation-Guide.md`](Industrial-Installation-Guide.md) §1)
2. **Create the database** — the create script for your engine; create-if-absent, never drops.
3. **Configure** — copy the matching `appsettings.*.example.json` (no secrets); inject connection
   strings / license config from environment or a git-ignored override at deploy time.
4. **First run** — the app auto-migrates, seeds, and installs all knowledge packs idempotently
   (`KnowledgePacks:InstallAllAtStartup`, default `true`).
5. **Verify** — `verify-knowledge-base.ps1`, `verify-installation.ps1`, `post-install-healthcheck.ps1`
   (all read-only, exit non-zero on failure).
6. **Back up** — before any later upgrade ([`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md)).

---

## 4. Build / package the deployable artifact

```powershell
# Framework-dependent (default, smaller) — bundles app + knowledge packs + scorecard
pwsh scripts/release/build-release.ps1 -Output ".\.tmp-publish"

# Self-contained (no runtime needed on the target)
pwsh scripts/release/build-release.ps1 -Output ".\.tmp-publish" -SelfContained -Runtime win-x64

# Assemble the shippable package (app + database scripts + packs + appsettings examples + docs)
pwsh scripts/release/package-release.ps1
```

Verified publish output this release: **151 files, ~45 MB**. The package excludes secrets, Data
Protection keys, model weights, and the source tree — see
[`Customer-Handover-Package.md`](Customer-Handover-Package.md).

---

## 5. Safety contract (all modes)

- **Create scripts never drop** — they detect an existing database and migrate only.
- **Migrations are additive and backward-compatible** — no destructive schema change ships without
  explicit human approval.
- **IIS deploy helper is dry-run unless `-Execute`**, and even then only publishes; site/app-pool
  changes are operator-gated.
- **Uninstall is a dry-run** that removes nothing and never drops the database.
- **No secrets in the package** — connection strings and license config are supplied at the
  deployment site.

---

## 6. Honest deployment status

- **No production deployment is proven in this work.** The IIS path is documented and dry-run-tested,
  but no representative production host was deployed.
- **SQL Express and Docker were not exercised on the build host.** Documented, not host-verified here.
- Close each with the same smoke + benchmark run executed on the target engine, captured in logs
  ([`Known-Limitations.md`](Known-Limitations.md) §5).
