# Roadmap to a True 20/20 (Commercial GA)

> **Authority.** `MASTER_VISION.md` is the authoritative source of truth for this
> repository. Where this document and `MASTER_VISION.md` conflict, `MASTER_VISION.md`
> prevails. This roadmap is subordinate planning material.
>
> **Honesty banner.** **None of the 15 blockers below are complete.** The current
> product state is a *controlled, operator-assisted pilot/demo* running against SQL
> Server LocalDB on a single developer workstation. This document is a deliberately
> honest accounting of the gap between that state and a true commercial
> general-availability (GA) release. Nothing here is marketing. Where a step requires
> hardware or software we do not have on the build host, that is stated explicitly and
> the step is marked as *not executed*.

---

## Anchored facts (verified at time of writing)

These are the verified facts this roadmap is built on. They are stated so the reader can
calibrate every claim below against the real, current state of the repository.

- **Branch / commit:** `ke-008-code-symbols`, last commit `7a35961`.
- **Build host:** `DESKTOP-M1HANKN`, Windows 11 Pro, .NET SDK `10.0.301`.
- **Build host capabilities:**
  - IIS is **NOT** installed on the build host.
  - Docker is **NOT** installed on the build host.
  - SQL Server Express **IS** present (instance `MSSQL$SQLEXPRESS`).
  - SQL Server **LocalDB** is what the demo actually runs against.
- **Build status:** 0 errors (Release configuration).
- **Knowledge base (verified live in LocalDB):** 4 packs / 438 items total.
  - `professional-base-v1` = 390 items
  - `financial-institution-operations-v1` = 16 items
  - `kyc-aml-transaction-approval-v1` = 16 items
  - `market-intelligence-forecasting-v1` = 16 items
- **Release artifact:** `LocalAIFactory-release-20260621-040519.zip`
  - Size: `16,997,982` bytes
  - SHA256: `eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b`
- **GitHub release state:** a **DRAFT prerelease** `v1.0.0-rc` exists (draft **and**
  prerelease — *not* published). There is **no** final `v1.0` tag.
- **Authentication today:** Windows / Negotiate in production (IIS), plus a guarded
  Development-only dev-auth handler. RBAC (Viewer < Analyst < Admin) and per-project
  `ProjectAccess` ACLs are enforced server-side and audited. There is **no** enterprise
  SSO / OIDC / Entra ID integration — that is design and additive hooks only.
- **Readiness (reported honestly):** mean ~**61.2%**, max **88**, **none at 100**
  (per the `/Readiness` page and `readiness-scorecard.json`).

### Cross-references (existing in this repository)

- `docs/Known-Limitations.md`
- `docs/SSO-IdP-Readiness.md`
- `docs/Edition-and-Licensing-Strategy.md`
- `docs/CUSTOMER_HANDOVER_WALKTHROUGH.md`
- `docs/Windows-Server-IIS-Deployment-Guide.md`
- `docs/SQL-Express-Pilot-Deployment.md`
- `docs/Full-SQL-Server-Deployment.md`
- `docs/Docker-Deployment-Guide.md`
- `docs/Autonomous-Local-Fix-Loop-Proof.md`
- `scripts/deployment-drill/` — the executable **dry-run** deployment drill pack
  (steps `00`–`08`); never executed against a real server.
- `scripts/sso/` — **read-only** SSO validators (`check-oidc-config.ps1`,
  `validate-claims-mapping.ps1`); no real tenant integration.
- `database/` — environment provisioning scripts (`create-localdb.ps1`,
  `create-sqlexpress-db.ps1`, `create-full-mssql-db.ps1`, etc.).

> Note: two file paths referenced in the originating task brief
> (`docs/SSO_ENTRA_ID_PROOF_PACK.md` and `docs/Production-Deployment-Drill-Pack.md`)
> do **not** exist in the repository at those exact paths. The closest existing
> equivalents are `docs/SSO-IdP-Readiness.md` (SSO readiness) and the executable
> `scripts/deployment-drill/` pack plus `docs/Windows-Server-IIS-Deployment-Guide.md`
> (deployment drill). Cross-links above point only at files that actually exist.

---

## Summary of the 15 blockers

| # | Blocker | Current status | Priority | Est. effort | Risk |
|---|---------|----------------|----------|-------------|------|
| 1 | Real Windows Server / IIS deployment | **IIS PILOT EXECUTED (Mode A): real IIS + ANCM + least-privilege SQL Express, app served through IIS, 0 HTTP 500s, rollback proven.** Remaining: HTTPS + production posture + Server edition + blue/green | P0 | 1–2 days | Medium |
| 2 | SQL Server Express deployment (deployed app) | **Substantially DONE (Mode C): published app executed against SQL Express 2022 — migrated+seeded+served, healthcheck PASS.** Remaining: behind IIS + production posture | P0 | 2–3 days | Medium |
| 3 | Full SQL Server deployment | Provisioning script exists; not executed | P1 | 2–4 days | Medium |
| 4 | Docker deployment | Docker not installed on build host; no Dockerfile proven | P2 | 3–5 days | Medium |
| 5 | Entra ID / OIDC / SSO | Design + additive hooks only; validators read-only; no real tenant | P0 | 5–10 days | High |
| 6 | External security review / pen-test | Internal `security-audit.ps1` only (0 HIGH); no third party | P0 | 2–6 weeks (external) | High |
| 7 | Customer pilot on sanitized estate data | No signed pilot; benchmarks use synthetic fixtures | P0 | 4–8 weeks (external) | High |
| 8 | SMTP real relay test | Config + health/test scripts exist; dev-sink only; no real relay | P1 | 1–2 days | Low |
| 9 | SFTP real endpoint test | Config + scripts exist; no real endpoint | P1 | 1–2 days | Low |
| 10 | Real OCR / CNN validation | Architecture + prototypes only; no trained model | P2 | 4–12 weeks | High |
| 11 | Cross-repository estate model | Not built; single-repo today | P2 | 3–6 weeks | Medium |
| 12 | Autonomous fix loop on a real repo | Proven on synthetic workspace only; dry-run default; allowlist-only | P1 | 2–4 weeks | High |
| 13 | Commercial license enforcement | Edition/license model designed; enforcement not active | P1 | 1–2 weeks | Medium |
| 14 | Signed customer acceptance | Acceptance test scripted; no signed customer signoff | P0 | depends on pilot | High |
| 15 | Final v1.0 release publication | Only a draft `v1.0.0-rc` prerelease exists; GA deliberately not done | P0 | 1 day (gated on 1–14) | Low |

Priority key: **P0** = mandatory for GA; **P1** = required for a credible GA but can
trail the first P0 wave; **P2** = needed for full-scope GA but can be staged.

---

## 1. Real Windows Server / IIS deployment

- **Current status: A REAL IIS PILOT WAS EXECUTED (Mode A, 2026-06-21).** IIS was enabled
  on this host (dism, no reboot) and the **ASP.NET Core Hosting Bundle 10.0.9** installed
  (winget → ANCM `AspNetCoreModuleV2` registered). The **published app was deployed under
  IIS** (site `LocalAIFactoryPilot`, app pool `LocalAIFactoryPilotPool`, No Managed Code /
  ApplicationPoolIdentity) and served **through IIS** (`Server: Microsoft-IIS/10.0`) — 7
  routes 200, DB-backed search, **0 HTTP 500s** — against **SQL Express
  `LocalAIFactory_IISProof`** with a **least-privilege** app-pool login (db_datareader +
  db_datawriter + EXECUTE, no db_owner). Windows-auth 401 Negotiate challenge demonstrated;
  rollback proven. Evidence: `reports/MODE_A_IIS_*`. Drill scripts `10`/`11`/`12` added.
  **Remaining for production:** HTTPS binding, full Negotiate+RBAC round-trip, a Windows
  **Server** edition, staged/blue-green rollout, and operations over time.
- **Why it blocks 20/20:** The product's production hosting model is IIS on Windows
  Server. Until the package is deployed, started, and served from a real IIS site, the
  primary deployment path is unproven. A dry-run validates intent, not reality.
- **Exact environment required:** A Windows Server (2019/2022) host with the IIS role,
  the ASP.NET Core Hosting Bundle for .NET 10, and a reachable SQL Server instance.
  This host does **not** exist on the build machine; this step cannot be completed on
  `DESKTOP-M1HANKN`.
- **Exact commands/procedure (on the real server):**
  ```powershell
  # On the target Windows Server, from the unzipped release + scripts:
  .\scripts\deployment-drill\00-prerequisites.ps1
  .\scripts\deployment-drill\01-check-host.ps1
  .\scripts\deployment-drill\02-install-prereqs-dryrun.ps1   # then re-run for real once validated
  .\scripts\deployment-drill\03-setup-sql-express-dryrun.ps1
  .\scripts\deployment-drill\04-setup-iis-dryrun.ps1
  .\scripts\deployment-drill\05-deploy-package-dryrun.ps1
  .\scripts\deployment-drill\06-run-healthchecks.ps1
  .\scripts\deployment-drill\08-capture-evidence.ps1
  ```
  The `-dryrun` steps must be promoted to live execution on the real host. Follow
  `docs/Windows-Server-IIS-Deployment-Guide.md` for the IIS site, app pool (No Managed
  Code), and Hosting Bundle specifics.
- **Evidence to capture:** Output of `06-run-healthchecks.ps1` and
  `08-capture-evidence.ps1`; HTTP 200 + timing for `/`, `/Projects`, `/Knowledge`,
  `/Models`; IIS site/app-pool configuration screenshots; Event Log entries; the
  resolved `eac98e2c…` SHA256 of the deployed package.
- **Success criteria:** All four core pages return 200 in well under one second from a
  real IIS site; the app migrates and seeds against the server's SQL instance on first
  start; restart survives an app-pool recycle and a server reboot.
- **Rollback plan:** Stop the IIS site, remove the application from the app pool, and
  restore the prior site binding. Database is additive-only on first run; drop the
  freshly created database if this was a clean install (`database/backup-database.ps1`
  before any change).
- **Responsible role:** Platform / infrastructure engineer.
- **Estimated effort:** 3–5 days (including host provisioning and first-run debugging).
- **Risk:** High — first real contact with IIS, Hosting Bundle, and server SQL.
- **Priority:** P0.

---

## 2. SQL Server Express deployment (as a deployed app)

- **Current status: SUBSTANTIALLY DONE (Mode C, 2026-06-21).** Executed: the **published app**
  was run against **SQL Server Express 2022** (`MSSQL$SQLEXPRESS`) with a fresh
  `LocalAIFactory_DeploymentProof` database — the app **migrated (14), seeded 4 packs / 438 items,
  and served** 13 HTTP routes (all 200, 0 HTTP 500s); `09-post-deploy-healthcheck` **PASS**; rollback
  proven. Evidence: `reports/DEPLOYMENT_DATABASE_PROOF.md`, `reports/DEPLOYMENT_PUBLISHED_APP_PROOF.md`.
  Remaining for full closure: run Express **behind IIS** (blocker 1) with a least-privilege app-pool SQL
  login, and a production (non-dev-auth) posture.
- **Why it blocks 20/20:** SQL Express is the stated minimum production-capable store
  for a small pilot. The product must be proven to migrate, seed, and serve from a real
  Express instance behind a deployed app, not only from LocalDB in a dev session.
- **Exact environment required:** SQL Server Express instance `MSSQL$SQLEXPRESS`
  reachable from the deployed app; appropriate SQL login/permissions; the app
  configured with the Express connection string
  (`database/appsettings.SqlExpress.example.json`).
- **Exact commands/procedure:**
  ```powershell
  .\database\create-sqlexpress-db.ps1
  # Point the deployed app at the Express instance using the example config as a template:
  #   database/appsettings.SqlExpress.example.json
  .\database\apply-migrations.ps1
  .\database\seed-professional-knowledge-base.ps1
  .\database\verify-knowledge-base.ps1     # expect 4 packs / 438 items
  ```
- **Evidence to capture:** `verify-knowledge-base.ps1` output showing 4 packs / 438
  items against Express; the app serving the four core pages from Express; migration
  log; connection string in use (with credentials redacted).
- **Success criteria:** Fresh Express database created, migrated, seeded to 438 items,
  and serving all four core pages under a deployed app — not a `dotnet run` dev session.
- **Rollback plan:** Drop the created Express database; restore from
  `database/backup-database.ps1` snapshot if one existed.
- **Responsible role:** Platform / database engineer.
- **Estimated effort:** 2–3 days.
- **Risk:** Medium — instance permissions and connection-string handling are the usual
  failure points.
- **Priority:** P0.

---

## 3. Full SQL Server deployment

- **Current status:** `database/create-full-mssql-db.ps1` and
  `database/appsettings.FullSqlServer.example.json` exist. They have **not** been
  executed against a full (non-Express) SQL Server.
- **Why it blocks 20/20:** Larger pilots and production estates will run on full SQL
  Server. The schema (34 tables), migrations, and seeding must be proven there.
- **Exact environment required:** A full SQL Server instance (Standard/Enterprise/
  Developer) reachable from the app; a database owner login; the full-server connection
  string template.
- **Exact commands/procedure:**
  ```powershell
  .\database\create-full-mssql-db.ps1
  # Configure from: database/appsettings.FullSqlServer.example.json
  .\database\apply-migrations.ps1
  .\database\seed-professional-knowledge-base.ps1
  .\database\verify-full-install.ps1
  .\database\verify-knowledge-base.ps1     # expect 4 packs / 438 items
  ```
- **Evidence to capture:** `verify-full-install.ps1` and `verify-knowledge-base.ps1`
  output; SQL Server edition/version banner; migration history table contents;
  page-timing for the four core pages.
- **Success criteria:** All 34 tables present, migrations applied cleanly, 438 items
  seeded, core pages served against full SQL Server.
- **Rollback plan:** Drop the created database; restore from backup if the instance was
  pre-existing (`database/restore-database.ps1` / `restore-verify-database.ps1`).
- **Responsible role:** Database engineer.
- **Estimated effort:** 2–4 days.
- **Risk:** Medium.
- **Priority:** P1.

---

## 4. Docker deployment

- **Current status:** `docs/Docker-Deployment-Guide.md` exists. **Docker is NOT
  installed on the build host**, and no Dockerfile has been built or run to produce a
  working container. The Docker path is documented but **unproven**.
- **Why it blocks 20/20:** Container deployment is a stated option for customers who
  prefer it. Until an image is built, run, and serves the core pages, the option is
  paper only.
- **Exact environment required:** A host with Docker (or a compatible container
  runtime) and access to the .NET 10 base images. This host does **not** exist on the
  build machine; this step cannot be completed on `DESKTOP-M1HANKN` as configured.
- **Exact commands/procedure (on a Docker-capable host):**
  ```bash
  # On a host with Docker installed (not available on the build host):
  docker build -t localaifactory:rc .
  docker run --rm -p 8080:8080 \
    -e ConnectionStrings__Default="<sql-connection-string>" \
    localaifactory:rc
  # Then verify the four core pages on the mapped port.
  ```
  A Dockerfile and the SQL connectivity model must first be authored/validated per
  `docs/Docker-Deployment-Guide.md`. Treat any existing Dockerfile as unproven until
  built.
- **Evidence to capture:** `docker build` log; `docker run` startup log showing
  migration + seed; HTTP 200 + timing for the four core pages from the container; image
  digest.
- **Success criteria:** A reproducible image builds, starts, migrates/seeds against an
  external SQL Server, and serves the four core pages.
- **Rollback plan:** `docker rm -f` the container and `docker rmi` the image; no host
  state changed beyond the external database (treated per blockers 2/3).
- **Responsible role:** Platform engineer.
- **Estimated effort:** 3–5 days.
- **Risk:** Medium — SQL connectivity and image size/base-image selection.
- **Priority:** P2.

---

## 5. Entra ID / OIDC / SSO

- **Current status:** Enterprise SSO is **design and additive hooks only**.
  `docs/SSO-IdP-Readiness.md` describes the intended integration; `scripts/sso/`
  contains **read-only** validators (`check-oidc-config.ps1`,
  `validate-claims-mapping.ps1`). There is **no** real tenant integration. Production
  auth today is Windows/Negotiate plus a guarded Development-only dev-auth handler;
  RBAC and per-project ACLs are enforced and audited server-side.
- **Why it blocks 20/20:** Banking customers will require federated identity (Entra ID /
  OIDC) with their own tenant, claims mapping, and group-to-role provisioning. Until the
  app authenticates real users through a real IdP and maps claims to the existing RBAC
  model, enterprise auth is unproven.
- **Exact environment required:** An Entra ID (or compatible OIDC) tenant with an app
  registration, a redirect URI for the deployed app, client credentials, and test users
  in mapped groups.
- **Exact commands/procedure:**
  ```powershell
  # Validate intended OIDC config + claims mapping (read-only, available today):
  .\scripts\sso\check-oidc-config.ps1
  .\scripts\sso\validate-claims-mapping.ps1
  # Then: implement the OIDC handler behind the existing additive hooks,
  # register the app in the tenant, and perform an interactive sign-in test.
  ```
  Wiring the real OIDC handler is new implementation work, not a script run.
- **Evidence to capture:** Successful interactive sign-in trace; claims dump mapped to
  Viewer/Analyst/Admin; audit-log entries for the federated login; validator output.
- **Success criteria:** A real tenant user signs in via OIDC, is mapped to the correct
  RBAC role and project ACLs, and the action is audited — with Windows/Negotiate still
  working as the fallback.
- **Rollback plan:** Disable the OIDC handler via config and revert to
  Windows/Negotiate; the hooks are additive, so removal does not affect existing auth.
- **Responsible role:** Identity / security engineer.
- **Estimated effort:** 5–10 days (excluding customer tenant lead time).
- **Risk:** High — claims mapping, group provisioning, and redirect/CORS correctness.
- **Priority:** P0.

---

## 6. External security review / penetration test

- **Current status:** Only an **internal** `security-audit.ps1` has been run, reporting
  **0 HIGH findings**. There has been **no** third-party security review or penetration
  test.
- **Why it blocks 20/20:** A banking customer's procurement and risk functions will
  require an independent security assessment. An internal audit, however clean, is not
  evidence for an external risk sign-off.
- **Exact environment required:** A representative deployed instance (ideally the IIS
  deployment from blocker 1) reachable by an accredited external assessor, with a defined
  scope and rules of engagement.
- **Exact commands/procedure:** Internal pre-check first:
  ```powershell
  .\scripts\security-audit.ps1   # internal baseline; expect 0 HIGH
  ```
  Then engage an external firm: agree scope, provide a test environment, run the
  assessment, and remediate findings. This is a contracted external activity, not a
  script.
- **Evidence to capture:** Signed external report; remediation tracker; re-test
  confirmation; internal audit baseline for comparison.
- **Success criteria:** External report delivered with no unmitigated HIGH/CRITICAL
  findings and a documented remediation/acceptance record.
- **Rollback plan:** Not applicable (assessment only); any remediation changes follow
  normal change control and are individually revertible.
- **Responsible role:** Security lead (internal) + external assessor.
- **Estimated effort:** 2–6 weeks elapsed (external scheduling dominates).
- **Risk:** High — unknown findings could require non-trivial remediation.
- **Priority:** P0.

---

## 7. Customer pilot on sanitized estate data

- **Current status:** **No signed pilot exists.** All benchmarks and demonstrations use
  **synthetic fixtures**, not real (even sanitized) customer estate data.
- **Why it blocks 20/20:** The core value claim — curated project memory and assisted
  enhancement of a real banking middleware estate (BDM, MCIB, ChequeXpert/Parascript,
  ETAMS) — is unproven until the platform ingests and operates over genuine, sanitized
  customer code and knowledge under a signed agreement.
- **Exact environment required:** A signed pilot agreement; a sanitized subset of a
  customer estate; a deployed instance (blocker 1/2); agreed success metrics.
- **Exact commands/procedure:** Ingest sanitized projects through the standard import
  pipeline, accumulate and approve knowledge through the approval lifecycle, and measure
  against the agreed metrics. This is a customer-facing engagement, not a script run.
- **Evidence to capture:** Signed pilot scope; ingestion logs; before/after knowledge
  counts; operator feedback; metric results vs. the synthetic baseline.
- **Success criteria:** Pilot completed against real sanitized data with the customer
  agreeing the platform met the agreed metrics.
- **Rollback plan:** Decommission the pilot instance and securely destroy sanitized data
  per the pilot agreement's data-handling terms.
- **Responsible role:** Delivery lead + customer sponsor.
- **Estimated effort:** 4–8 weeks elapsed.
- **Risk:** High — depends on real-world data quality and customer availability.
- **Priority:** P0.

---

## 8. SMTP real relay test

- **Current status:** Configuration plus health/test scripts exist, but only a
  **dev-sink** has been exercised. No test against a **real SMTP relay** has been done.
- **Why it blocks 20/20:** Notifications/alerts depend on real mail delivery. A dev-sink
  proves wiring, not deliverability.
- **Exact environment required:** A reachable SMTP relay (host, port, credentials/TLS
  settings) authorized to send from the deployment's domain.
- **Exact commands/procedure:** Configure the SMTP relay settings, then run the existing
  SMTP health/test routine against the real relay and confirm message receipt at a real
  mailbox.
- **Evidence to capture:** Send log; relay acceptance response; received message
  headers; redacted relay configuration.
- **Success criteria:** A test message is accepted by a real relay and received intact.
- **Rollback plan:** Revert to the dev-sink configuration; no persistent state.
- **Responsible role:** Platform engineer.
- **Estimated effort:** 1–2 days.
- **Risk:** Low.
- **Priority:** P1.

---

## 9. SFTP real endpoint test

- **Current status:** Configuration plus scripts exist. No test against a **real SFTP
  endpoint** has been done.
- **Why it blocks 20/20:** Estate ingest/exchange flows that use SFTP must be proven
  against a real server (host-key verification, auth, directory permissions).
- **Exact environment required:** A reachable SFTP server with credentials/keys and an
  agreed landing directory.
- **Exact commands/procedure:** Configure the SFTP endpoint settings, then run the
  existing SFTP test routine to connect, verify the host key, and perform a round-trip
  put/get against the real endpoint.
- **Evidence to capture:** Connection log with host-key fingerprint; successful
  put/get transcript; directory listing; redacted endpoint configuration.
- **Success criteria:** A file round-trips to and from a real SFTP endpoint with host-key
  verification enforced.
- **Rollback plan:** Revert to local/test configuration; remove any test files left on
  the endpoint.
- **Responsible role:** Platform engineer.
- **Estimated effort:** 1–2 days.
- **Risk:** Low.
- **Priority:** P1.

---

## 10. Real OCR / CNN validation

- **Current status:** **Architecture and prototypes only.** There is **no trained
  model**. The ChequeXpert / Parascript OCR/CNN path is a **prototype**, not a validated
  recognition pipeline.
- **Why it blocks 20/20:** Cheque/document recognition is a domain capability customers
  will expect to work on real documents. A prototype without a trained, validated model
  cannot be claimed as a feature.
- **Exact environment required:** A labelled training/validation dataset of representative
  documents; compute appropriate to training/inference (the build host has no GPU); the
  Parascript/ChequeXpert integration target.
- **Exact commands/procedure:** Assemble and label data; train or integrate a recognition
  model; validate against a held-out set; integrate behind the existing prototype
  interface. This is a model-development effort, not a single command.
- **Evidence to capture:** Dataset description; training/validation metrics (accuracy,
  error rates) on held-out data; integration test results on real documents.
- **Success criteria:** A trained/validated model meets agreed recognition accuracy on
  held-out real documents and is integrated into the pipeline.
- **Rollback plan:** Keep the prototype gated off; the recognition path is optional and
  does not block other functionality.
- **Responsible role:** ML engineer + domain SME.
- **Estimated effort:** 4–12 weeks (data-dependent).
- **Risk:** High — data availability and accuracy targets dominate.
- **Priority:** P2.

---

## 11. Cross-repository estate model

- **Current status:** **Not built.** The platform operates on a **single repository**
  today. There is no model spanning multiple repositories of a banking estate.
- **Why it blocks 20/20:** A real estate spans many repositories/systems (BDM, MCIB,
  ETAMS, etc.). Full-scope value requires reasoning across repositories, not one at a
  time.
- **Exact environment required:** Multiple representative (sanitized) repositories and a
  data model that links knowledge, code symbols, and projects across them.
- **Exact commands/procedure:** Design and implement the cross-repository data model and
  retrieval; this is new feature work that must respect the schema-change rules in
  `CLAUDE.md` (additive, approved migrations only).
- **Evidence to capture:** Design doc; migration (if any) reviewed and approved;
  cross-repo retrieval producing correct results across at least two repositories.
- **Success criteria:** Knowledge and retrieval span two or more repositories coherently
  without regressing single-repo behaviour or core-page performance.
- **Rollback plan:** Feature-flag off; any additive migration is backward-compatible by
  rule, so single-repo operation is unaffected.
- **Responsible role:** Backend engineer + architect.
- **Estimated effort:** 3–6 weeks.
- **Risk:** Medium.
- **Priority:** P2.

---

## 12. Autonomous fix loop on a real repository

- **Current status:** The `ControlledExecutor` autonomous fix loop is **proven on a
  synthetic workspace only** (see `docs/Autonomous-Local-Fix-Loop-Proof.md`). It runs
  **dry-run by default** and is **allowlist-only**. It has **not** been run against a
  real customer repository.
- **Why it blocks 20/20:** The long-term value is autonomously enhancing/debugging real
  banking projects. Proving the loop on synthetic code does not demonstrate safe,
  useful behaviour on real, messy estate code.
- **Exact environment required:** A sanitized real repository (ideally from the pilot,
  blocker 7), a controlled workspace, and an explicit, reviewed command allowlist.
- **Exact commands/procedure:** Run the fix loop in dry-run against the real repo first,
  review the proposed plan/diff, then promote to apply mode only with explicit approval
  and the allowlist enforced. Follow the procedure in
  `docs/Autonomous-Local-Fix-Loop-Proof.md`.
- **Evidence to capture:** Dry-run plan and diff; reviewer approval; applied-change
  result; build/test outcome on the real repo; audit trail of allowlisted commands.
- **Success criteria:** The loop produces a correct, reviewed, build-passing change on a
  real sanitized repository within the allowlist, with full audit.
- **Rollback plan:** Workspace is sandboxed; revert the workspace (git) and keep apply
  mode disabled. Dry-run default ensures no change without explicit promotion.
- **Responsible role:** Senior engineer (with reviewer).
- **Estimated effort:** 2–4 weeks.
- **Risk:** High — autonomous changes on real code require strong guardrails.
- **Priority:** P1.

---

## 13. Commercial license enforcement

- **Current status:** The edition/license model is **designed** (see
  `docs/Edition-and-Licensing-Strategy.md`) but **enforcement is not active**. Editions
  and entitlements are not currently gated at runtime.
- **Why it blocks 20/20:** A commercial GA needs enforceable editions/entitlements.
  Designed-but-unenforced licensing cannot back a commercial offering.
- **Exact environment required:** The deployed app plus a license issuance/validation
  mechanism per the strategy doc.
- **Exact commands/procedure:** Implement the enforcement layer behind the designed model
  (additive, per schema rules), issue a test license, and verify entitlement gating
  toggles features as specified.
- **Evidence to capture:** Test license artifacts; enforcement test showing gated/ungated
  behaviour; audit entries for license validation.
- **Success criteria:** Editions/entitlements are enforced at runtime per
  `docs/Edition-and-Licensing-Strategy.md`, validated by tests, with graceful behaviour
  when a license is absent or expired.
- **Rollback plan:** Feature-flag enforcement off to restore current unrestricted
  behaviour; any schema change is additive and backward-compatible.
- **Responsible role:** Backend engineer + product owner.
- **Estimated effort:** 1–2 weeks.
- **Risk:** Medium.
- **Priority:** P1.

---

## 14. Signed customer acceptance

- **Current status:** An acceptance test is **scripted**, but there is **no signed
  customer sign-off**.
- **Why it blocks 20/20:** GA for a banking customer requires a formal, signed acceptance
  against agreed criteria. A scripted test without a counter-signature is not acceptance.
- **Exact environment required:** The pilot deployment (blockers 1/2 and 7), agreed
  acceptance criteria, and an authorized customer signatory.
- **Exact commands/procedure:** Execute the scripted acceptance test against the pilot
  instance, walk the customer through results using
  `docs/CUSTOMER_HANDOVER_WALKTHROUGH.md`, and obtain a signed acceptance record.
- **Evidence to capture:** Acceptance test results; the signed acceptance document;
  any conditions/caveats recorded.
- **Success criteria:** A countersigned acceptance record exists against the agreed
  criteria.
- **Rollback plan:** Not applicable (sign-off is a milestone, not a change). Unmet
  criteria feed back into the relevant blockers.
- **Responsible role:** Delivery lead + customer signatory.
- **Estimated effort:** Dependent on the pilot (blocker 7); the signing step itself is
  short once the pilot succeeds.
- **Risk:** High — contingent on pilot outcomes.
- **Priority:** P0.

---

## 15. Final v1.0 release publication

- **Current status:** Only a **draft `v1.0.0-rc` prerelease** exists on GitHub (draft
  **and** prerelease — not published). There is **no** final `v1.0` tag. Final GA
  publication is **deliberately not done** and remains **blocked on blockers 1–14**.
- **Why it blocks 20/20:** Publishing a final GA release while real deployment, SSO,
  external security review, and a signed pilot are incomplete would be a false claim of
  readiness. Publication is intentionally withheld.
- **Exact environment required:** All of blockers 1–14 complete, with their evidence on
  file.
- **Exact commands/procedure (only once 1–14 are done):**
  ```bash
  # Verify the artifact hash matches the release before publishing:
  #   LocalAIFactory-release-20260621-040519.zip
  #   SHA256 eac98e2cdef11d7a2958b7b2d5257e0caf00576f0fd12740888dcece22e6e63b
  gh release view v1.0.0-rc           # confirm current draft/prerelease state
  # After 1-14 are complete and signed off, cut and publish the final tag:
  gh release create v1.0.0 --title "v1.0.0" --notes-file <release-notes>
  ```
- **Evidence to capture:** The completed evidence packs from blockers 1–14; the matching
  artifact SHA256; the published release record.
- **Success criteria:** A final `v1.0` release is published **only after** every
  preceding blocker is complete and signed off.
- **Rollback plan:** Delete/yank the published release and revert to the draft prerelease
  if any blocker is later found incomplete.
- **Responsible role:** Release manager + product owner.
- **Estimated effort:** ~1 day of mechanical work, fully gated on blockers 1–14.
- **Risk:** Low (mechanical) once gated conditions are met.
- **Priority:** P0.

---

## Definition of true 20/20

GA (a true 20/20) is **only** claimable when **all** of the following are complete with
captured evidence:

1. **Real production deployment** — the package deployed, started, and serving from a
   real Windows Server / IIS host (blocker 1), backed by a real SQL Server instance
   (blockers 2/3).
2. **Enterprise SSO** — real Entra ID / OIDC authentication mapped to the existing RBAC
   and project ACL model, with Windows/Negotiate retained as fallback (blocker 5).
3. **External security review** — an independent third-party assessment / penetration
   test with no unmitigated HIGH/CRITICAL findings (blocker 6).
4. **Signed customer pilot** — a completed pilot on real sanitized estate data with a
   countersigned acceptance record (blockers 7 and 14).

Until those four are simultaneously true, the product remains a **controlled,
operator-assisted pilot/demo**, and the final `v1.0` release (blocker 15) stays
**unpublished by design**. Readiness today is reported honestly at mean ~61.2%, max 88,
**none at 100** — consistent with this roadmap. See also `docs/Known-Limitations.md`.
