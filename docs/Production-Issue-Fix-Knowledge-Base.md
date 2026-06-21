# Production Issue Fix Knowledge Base

A readable index of the production-incident patterns LocalAIFactory should recognise, with
reusable IF/THEN rules. The machine-readable forms live in:

- Installable pack: `knowledge-packs/production-issue-fixes-v1/` (42 items)
- Learning registry: `benchmarks/support-issue-learning-registry.json` (42 issues)

Every item is an **original** summary. No vendor documentation, support-forum answer, or
copyrighted text is reproduced. Community patterns are flagged with a confidence score and, where
relevant, marked as not yet confirmed against an official source. Items tagged **firsthand** are
observations from this project.

> Scope reminder (per CLAUDE.md): MSSQL is the primary store and must work standalone; Qdrant and
> Ollama are optional and must degrade gracefully; never put a blocking external call or a
> large-text load on the request path.

---

## 1. How to use this KB

When a deployment, runtime, or integration failure appears:

1. **Identify the layer first.** Most failures are environment/configuration, not application code.
   Confirm whether the app itself is healthy (e.g. run the publish output via Kestrel) before
   editing code.
2. **Match the symptom** to a pattern below.
3. **Run the diagnostic** for that pattern.
4. **Apply the known fix**, then record the outcome back into the learning registry so detection
   improves over time.

---

## 2. Issue pattern index

### Hosting / IIS / runtime
- ASP.NET Core IIS deployment failure (`SIL-001`)
- ANCM / Hosting Bundle missing or mismatched — *firsthand: absent on a fresh IIS host* (`SIL-002`)
- IIS Windows Authentication failure (`SIL-003`)
- HTTPS certificate binding failure (`SIL-007`)
- `netsh http add sslcert` failure (`SIL-008`)
- stdout logging / locked log file (`SIL-010`)
- IIS 500.19 configuration data invalid (`SIL-011`)
- ASP.NET Core 500.30 startup failure (`SIL-012`)
- ASP.NET Core 502.5 process startup failure (`SIL-013`)
- Windows service / process file-lock (`SIL-017`)
- Reverse-proxy / forwarded-headers misconfiguration (`SIL-035`)
- Upload-size / request-limit failure (`SIL-037`)
- appcmd attribute path quirk — *firsthand* (`SIL-042`)

### Database
- SQL Server Express connection failure (`SIL-004`)
- EF Core migration failure on startup (`SIL-005`)
- App-pool identity / SQL permission failure (`SIL-006`)
- LocalDB vs SQL Express differences (`SIL-014`)
- Database backup / restore failure (`SIL-015`)
- DB timeout / connection-pool exhaustion — *firsthand: GroupBy / large-text hang* (`SIL-038`)

### Configuration / OS / tooling
- appsettings / environment configuration mistakes (`SIL-009`)
- File / folder permission failure (NTFS) (`SIL-016`)
- PowerShell quoting / path issues (`SIL-020`)
- Invoke-WebRequest won't send credentials over HTTP — *firsthand* (`SIL-041`)
- Long-path / clone-timeout, clone-retry "destination exists" — *firsthand* (`SIL-021`)

### Build / release / benchmark
- Playwright / browser install failure (`SIL-018`)
- GitHub release upload failure (`SIL-019`)
- Large-repo benchmark failure (`SIL-022`)
- High-memory benchmark failure (OOM) (`SIL-039`)
- Production incident rollback failure (`SIL-040`)

### Optional components
- Ollama model-availability issue (`SIL-023`)
- Qdrant / vector-store connection issue (`SIL-024`)
- OCR / PDF processing failure (`SIL-025`)

### Integrations
- SFTP / SMTP integration failure (`SIL-026`)
- WordPress REST API (`SIL-027`)
- Odoo external API (`SIL-028`)
- ERPNext / Frappe API (`SIL-029`)
- WooCommerce REST API (`SIL-030`)
- Magento REST/GraphQL (`SIL-031`)
- Drupal JSON:API / REST (`SIL-032`)
- Keycloak / OIDC (`SIL-033`)
- Entra ID claims-mapping (`SIL-034`)
- CORS / auth-cookie cross-site (`SIL-036`)

---

## 3. Reusable IF / THEN rules

These are the high-leverage rules the agent should apply before changing code.

**Hosting and startup**
- **IF** IIS returns **500.30**, **THEN** check Event Viewer + stdout logs + appsettings + runtime
  bundle **before** changing code — 500.30 only means "started then crashed"; the real cause is the
  captured startup exception.
- **IF** IIS returns **502.5**, **THEN** confirm the runtime is installed (`dotnet --list-runtimes`)
  and the publish runs standalone (`dotnet App.dll`) before touching code.
- **IF** IIS returns **500.19**, **THEN** validate `web.config` XML and install the missing IIS
  module (URL Rewrite / Hosting Bundle) before assuming an app fault.
- **IF** the app runs under Kestrel but fails under IIS, **THEN** suspect the IIS hosting layer
  (physical path, Hosting Bundle, web.config), not the application.
- **IF** ANCM fails to start the process on a freshly imaged host, **THEN** install the matching
  Hosting Bundle and run `iisreset` — a new host may have no ANCM at all (*firsthand*).

**Database**
- **IF** SQL says **"Login failed"** naming a machine or `IIS APPPOOL\...` account, **THEN** create a
  least-privilege SQL login for the app-pool identity (`is_sysadmin=0`); never elevate to sysadmin.
- **IF** SQL Express is unreachable, **THEN** check instance name, SQL Browser, TCP/IP, and firewall
  before suspecting the app.
- **IF** a page **hangs** (a `-> started` log line with no matching `<- completed`), **THEN** look
  for `GroupBy(_ => 1)`, large-text materialization, or a blocking external call on the request path
  — these are the documented hang causes (*firsthand*).
- **IF** a rollback is needed and the failed release ran a **destructive migration**, **THEN** restore
  the pre-release backup; reverting code alone will not recover lost data.

**Tooling and scripting**
- **IF** a **clone retry** says "destination path already exists", **THEN** delete the partial clone
  directory **before** retrying (and enable `core.longpaths` for "Filename too long") (*firsthand*).
- **IF** a PowerShell `Invoke-WebRequest` health probe gets **401 over http** but a browser succeeds,
  **THEN** the transport is the cause — probe over **HTTPS** (*firsthand*).
- **IF** a PowerShell command mangles a path with spaces, **THEN** quote it and invoke via the call
  operator `&` / use `--%` for literal native args before blaming the tool.
- **IF** an **appcmd set** reports success but nothing changed, **THEN** read it back with
  `appcmd list config` and fix the `/section` + `/commit:apphost` scope and quoting (*firsthand*).
- **IF** a **GitHub release upload** says "asset already exists", **THEN** re-run with `--clobber` (or
  rename) and verify token scope.

**Optional components and integrations**
- **IF** an **optional** component (Qdrant/Ollama) is down, **THEN** the feature must degrade — never
  block a core page; read health from the cached snapshot, not a live call.
- **IF** an integration returns **401/403**, **THEN** verify the auth mode (application password /
  API token / OAuth1 / OIDC claims) and whether a reverse proxy stripped the Authorization header,
  before debugging payloads.
- **IF** behind a reverse proxy the app emits **http://** redirects, **THEN** enable and configure
  forwarded-headers middleware (with known proxies) before changing redirect/auth logic.
- **IF** OCR returns empty/garbled text, **THEN** check whether the PDF is image-only (needs OCR) vs
  has a text layer, and verify engine/DPI; never assert OCR text as authoritative without confidence.

---

## 4. Feedback loop

Each resolved incident should update `benchmarks/support-issue-learning-registry.json`:
set/confirm `detectBy`, `fixOrWarn`, `confidence`, and `officialSourceConfirmation`. Firsthand fixes
start with `officialSourceConfirmation: false` and can be promoted once cross-checked against an
official source.
