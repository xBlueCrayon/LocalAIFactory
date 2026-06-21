# Final Customer Handover Index — LocalAIFactory

The single entry point for a customer or operator receiving the LocalAIFactory package. It tells you
**what is in the box**, the **order** to install and validate, where to get **support**, and a
**links table by role**. It does not duplicate the detailed guides — it points at them.

> LocalAIFactory is a private, local-first, **MSSQL-authoritative** AI software-engineering platform
> for a banking middleware estate. MSSQL is the source of truth; Ollama (local inference), Qdrant
> (vectors), and a GPU are all **optional** and degrade gracefully when absent. The platform makes
> **no** vendor-certification, regulatory, financial, fraud-proof, or commercial-GA claim. See
> [`Known-Limitations.md`](Known-Limitations.md) and [`readiness-scorecard.json`](readiness-scorecard.json).

---

## 1. What's included

The deployable package (assembled by `scripts/release/package-release.ps1`, never the `.git` source
tree) contains:

| Component | Where | Purpose |
|---|---|---|
| **Application binaries** | `app/` | The runnable ASP.NET Core Web host (publish: ~151 files, ~45 MB) |
| **Database scripts** | `database/` | Create / seed / verify / backup / restore / migrate (all create-if-absent, never drop) |
| **appsettings examples** | `database/appsettings.*.example.json` | LocalDB / SQL Express / full SQL Server templates (no secrets) |
| **Knowledge packs (4)** | `knowledge-packs/` | `professional-base-v1` (390 items) + 3 domain packs (48 items) = **438 items** |
| **Install / ops scripts** | `scripts/release/`, `scripts/diagnostics/` | Verify install, health-check, system snapshot |
| **Documentation** | `docs/` | Deployment, runbooks, governance, limitations |

Full contents and exclusions (secrets, Data Protection keys, model weights, source tree) are in
[`Customer-Handover-Package.md`](Customer-Handover-Package.md).

---

## 2. Install order

Pick the deployment mode first (decision table in [`Deployment-Guide.md`](Deployment-Guide.md)), then
follow the matching guide:

1. **Prerequisites** — .NET 10 SDK (or ASP.NET Core 10 runtime for framework-dependent IIS),
   `sqlcmd`, one SQL Server engine. See [`Industrial-Installation-Guide.md`](Industrial-Installation-Guide.md) §1.
2. **Create the database** — one create script for your engine; create-if-absent, never drops:
   - LocalDB demo → [`FINAL_LOCAL_DEPLOYMENT_GUIDE.md`](FINAL_LOCAL_DEPLOYMENT_GUIDE.md)
   - SQL Express pilot → [`SQL-Express-Pilot-Deployment.md`](SQL-Express-Pilot-Deployment.md)
   - Full SQL Server → [`Full-SQL-Server-Deployment.md`](Full-SQL-Server-Deployment.md)
   - Windows Server / IIS → [`Windows-Server-IIS-Deployment-Guide.md`](Windows-Server-IIS-Deployment-Guide.md)
   - Docker → [`Docker-Deployment-Guide.md`](Docker-Deployment-Guide.md) (compose files exist; **not** executed on the build host)
   - MSSQL-only / air-gapped → [`Offline-Mode-Guide.md`](Offline-Mode-Guide.md)
3. **Run the app** — it **auto-migrates, seeds, and installs all 4 knowledge packs idempotently** on
   first startup (`KnowledgePacks:InstallAllAtStartup`, default `true`). See the local guide.
4. **Configuration** — copy the matching `appsettings.*.example.json`; supply connection strings and
   any license config from environment variables or a git-ignored override (never the package).
   See [`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md) §2.

---

## 3. Validation order

Run these read-only gates in order. Each exits non-zero on failure, so they are deployment gates:

1. **Knowledge base** — `pwsh database/verify-knowledge-base.ps1` → expects a `KnowledgePack` row,
   baseline ≥ 100, unique Uids, all curated. See [`FINAL_KNOWLEDGE_BASE_GUIDE.md`](FINAL_KNOWLEDGE_BASE_GUIDE.md).
2. **Installation artifacts + KB** — `pwsh scripts/release/verify-installation.ps1`.
3. **Running-instance health** — `pwsh scripts/release/post-install-healthcheck.ps1 -Url <url>` →
   GETs core pages, asserts 200/302, changes nothing.
4. **Backup proof** (before any later upgrade) — see [`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md).

Verified on the build host this release: build **0 errors**, **235/235** tests, benchmark standard
**PASS** (ERP/CRM Gold 6/6, core-banking Gold 6/6, KYC/AML→approval Gold 7/7), UI smoke **PASS**
(11 pages incl. `/Support`), `verify-poc` **PASS**. These prove the implemented core; they do **not**
close the open gaps (see §5).

---

## 4. Support entry points

| Need | Go to |
|---|---|
| Running-instance health at a glance | The read-only `/Support` page in the app |
| Operational triage and escalation | [`Support-Runbook.md`](Support-Runbook.md) |
| Symptom → fix | [`Troubleshooting-Guide.md`](Troubleshooting-Guide.md), [`07-Troubleshooting.md`](07-Troubleshooting.md) |
| Backup / restore / upgrade / rollback | [`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md), [`Upgrade-Rollback-Runbook.md`](Upgrade-Rollback-Runbook.md) |
| What is **not** claimed | [`Known-Limitations.md`](Known-Limitations.md) |
| Live readiness scores | The `/Readiness` page · [`readiness-scorecard.json`](readiness-scorecard.json) |

---

## 5. Honest posture (read before deploying)

This release is positioned as a **controlled, operator-assisted paid pilot** scoped to the proven
core. It is **not** commercial general availability and carries no compliance, regulatory, financial,
or fraud certainty. Notable open items, each with a stated proof-to-close in
[`Known-Limitations.md`](Known-Limitations.md):

- **No executed production deployment** — IIS scripts are dry-run runbooks; no representative
  production host was deployed in this work.
- **Docker not exercised** — `deploy/` compose files exist but Docker is **not installed** on the
  build host; the container path is unverified here.
- **No trained OCR/CV model** — document and cheque paths are deterministic prototypes only
  ([`OCR-CNN-Document-Intelligence-Status.md`](OCR-CNN-Document-Intelligence-Status.md)).
- **No autonomous fix loop proven on a real repo** — the controlled loop is safe by construction but
  proven only on a synthetic workspace ([`Autonomous-Engineering-Status.md`](Autonomous-Engineering-Status.md)).
- **No enterprise SSO/IdP** — Windows/Negotiate authentication only.

---

## 6. Links by role

| Role | Read |
|---|---|
| **Customer / operator (start here)** | This index, [`FINAL_RELEASE_OVERVIEW.md`](FINAL_RELEASE_OVERVIEW.md), [`Customer-Handover-Package.md`](Customer-Handover-Package.md), [`Customer-Onboarding-Guide.md`](Customer-Onboarding-Guide.md) |
| **Fast local install** | [`FINAL_LOCAL_DEPLOYMENT_GUIDE.md`](FINAL_LOCAL_DEPLOYMENT_GUIDE.md), [`02-Setup.md`](02-Setup.md), [`Quick-Start-With-Screenshots.md`](Quick-Start-With-Screenshots.md) |
| **Deployment** | [`Deployment-Guide.md`](Deployment-Guide.md), [`SQL-Express-Pilot-Deployment.md`](SQL-Express-Pilot-Deployment.md), [`Full-SQL-Server-Deployment.md`](Full-SQL-Server-Deployment.md), [`Windows-Server-IIS-Deployment-Guide.md`](Windows-Server-IIS-Deployment-Guide.md), [`Docker-Deployment-Guide.md`](Docker-Deployment-Guide.md), [`Offline-Mode-Guide.md`](Offline-Mode-Guide.md) |
| **Database** | [`SQL-Server-Deployment-Guide.md`](SQL-Server-Deployment-Guide.md), [`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md), [`Upgrade-Rollback-Runbook.md`](Upgrade-Rollback-Runbook.md) |
| **Knowledge base** | [`FINAL_KNOWLEDGE_BASE_GUIDE.md`](FINAL_KNOWLEDGE_BASE_GUIDE.md), [`Knowledge-Architecture-General-Project-Chat.md`](Knowledge-Architecture-General-Project-Chat.md), [`Knowledge-Pack-Authoring-Guide.md`](Knowledge-Pack-Authoring-Guide.md) |
| **AI governance** | [`AI-Governance.md`](AI-Governance.md), [`AI-Output-Provenance-and-Approval.md`](AI-Output-Provenance-and-Approval.md), [`Multi-Agent-Knowledge-Factory.md`](Multi-Agent-Knowledge-Factory.md) |
| **Advanced-module status** | [`OCR-CNN-Document-Intelligence-Status.md`](OCR-CNN-Document-Intelligence-Status.md), [`Autonomous-Engineering-Status.md`](Autonomous-Engineering-Status.md), [`Market-Module-Disclaimers.md`](Market-Module-Disclaimers.md) |
| **Security** | [`Security-Model.md`](Security-Model.md), [`Threat-Model.md`](Threat-Model.md), [`RBAC-Matrix.md`](RBAC-Matrix.md), [`Audit-Model.md`](Audit-Model.md) |
| **Support** | [`Support-Runbook.md`](Support-Runbook.md), [`Troubleshooting-Guide.md`](Troubleshooting-Guide.md), [`Hardware-Profiles.md`](Hardware-Profiles.md) |
| **Executive / buyer** | [`FINAL_RELEASE_OVERVIEW.md`](FINAL_RELEASE_OVERVIEW.md), [`Known-Limitations.md`](Known-Limitations.md), [`Edition-Matrix.md`](Edition-Matrix.md) |

The full role index lives in [`README.md`](README.md) (documentation hub).
