# Official Deployment & Security References

Curated, annotated list of the **official** references consulted for the final release check
(2026-06-21). Internet access was available; every URL below was fetched or resolved during this
review. Each entry includes a one-line "why it matters here" for LocalAIFactory (.NET 10 / ASP.NET
Core MVC + MSSQL/EF Core, optional Ollama/Qdrant, Windows-auth RBAC, IIS or local).

> Only references actually fetched/resolved during this review are listed. URLs are versionless
> (Microsoft Learn serves the current moniker, .NET 10 / SQL Server latest, automatically).

---

## ASP.NET Core hosting & IIS deployment

- **Host ASP.NET Core on Windows with IIS**
  https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/
  *Why it matters:* defines the Hosting Bundle requirement, in-process vs out-of-process model, and
  app-pool / `web.config` (ANCM) setup for the IIS deployment path.

- **Troubleshoot ASP.NET Core on Azure App Service and IIS** (startup error codes)
  https://learn.microsoft.com/en-us/aspnet/core/test/troubleshoot-azure-iis
  *Why it matters:* authoritative mapping of 500.0 / 500.30–500.35 / 502.5 startup failures to causes
  and fixes — the exact symptoms an IIS install will hit if the runtime/bitness/Hosting Bundle is wrong.

## EF Core migrations on deploy

- **Applying Migrations — EF Core**
  https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying
  *Why it matters:* states the recommended production strategies (SQL scripts, idempotent scripts,
  migration bundles), the explicit warning against `Database.Migrate()` at startup, and the EF Core 9+
  automatic database-wide **migration lock** — directly relevant to `Program.cs:82`.

## SQL Server backup / restore (Express has no compression)

- **Backup Compression (SQL Server)**
  https://learn.microsoft.com/en-us/sql/relational-databases/backup-restore/backup-compression-sql-server
  *Why it matters:* confirms compression is **Enterprise/Standard/Developer only** (not Express); all
  editions can *restore* a compressed backup. Validates the repo's decision to omit `WITH COMPRESSION`
  on Express/LocalDB demo installs.

- **BACKUP (Transact-SQL)** (referenced from the page above)
  https://learn.microsoft.com/en-us/sql/t-sql/statements/backup-transact-sql
  *Why it matters:* canonical syntax/options for the backup T-SQL the install scripts emit.

## Microsoft Entra ID / OpenID Connect in ASP.NET Core

- **Tutorial: Prepare an ASP.NET Core web app for authentication (Microsoft identity platform)**
  https://learn.microsoft.com/en-us/entra/identity-platform/tutorial-web-app-dotnet-prepare-app
  *Why it matters:* minimal integration shape (`Microsoft.Identity.Web`, `AzureAd` config:
  Instance/TenantId/ClientId, `CallbackPath=/signin-oidc`, sign-out `/signout-callback-oidc`) and the
  exact redirect-URI registration that must match — only relevant **if** an Entra/OIDC option is added,
  since the default is Windows-auth RBAC.

## Docker Desktop on Windows (only for optional Qdrant)

- **Install Docker Desktop on Windows**
  https://docs.docker.com/desktop/setup/install/windows-install/
  *Why it matters:* WSL2 backend + hardware virtualization + recent Win build requirements and the
  common "WSL2 not installed / virtualization disabled" blockers; informs keeping Docker strictly
  optional so MSSQL-only mode never depends on it.

## Playwright (screenshots / headless)

- **Playwright — Continuous Integration**
  https://playwright.dev/docs/ci
  *Why it matters:* `npx playwright install --with-deps chromium`, headless-by-default, `workers: 1`
  for stability — informs `scripts/docs/capture-screenshots.ps1` prerequisites and flake avoidance.

## Ollama (optional local inference)

- **Ollama FAQ**
  https://ollama.readthedocs.io/en/faq/
  *Why it matters:* API on `127.0.0.1:11434`, fully offline once models are pulled, `OLLAMA_HOST` /
  `OLLAMA_ORIGINS` config, and "connection refused" = server not running — matches the optional,
  health-cached integration.

## Qdrant (optional vector store)

- **Qdrant — Installation**
  https://qdrant.tech/documentation/guides/installation/
  *Why it matters:* `docker run -p 6333:6333 -p 6334:6334 qdrant/qdrant`, REST 6333 / gRPC 6334,
  readiness `/readyz`, dashboard `:6333/dashboard` — REST-only, optional, never on the request path.

## Governance / security frameworks (applied, not certified)

- **NIST Secure Software Development Framework (SSDF, SP 800-218)**
  https://csrc.nist.gov/Projects/ssdf
  *Why it matters:* PO/PS/PW/RV practice groups; highest-value here are PS (protect secrets/integrity),
  PW (build/test gate), RV (vulnerability response).

- **NIST AI Risk Management Framework (AI RMF 1.0)**
  https://www.nist.gov/itl/ai-risk-management-framework
  *Why it matters:* Govern/Map/Measure/Manage + trustworthiness (validity, accountability, human
  oversight) — underpins the "AI output is a proposal, human approves" product principle.

- **OWASP Application Security Verification Standard (ASVS)**
  https://owasp.org/www-project-application-security-verification-standard/
  *Why it matters:* leveled checklist for authentication, access control, session, logging, and
  secrets/crypto — a verification yardstick for the RBAC + audit + secrets-at-rest controls.

---

### References not deep-fetched (resolved / well-known, not quoted here)

- **OpenTelemetry .NET** — https://opentelemetry.io/docs/languages/dotnet/ — baseline for the
  observability/request-timing practice already implemented via `RequestTimingMiddleware`.
- **AWS Well-Architected Framework** — https://aws.amazon.com/architecture/well-architected/ —
  Security pillar (least privilege, traceability/audit) and Reliability pillar map conceptually; this
  is an on-prem/local-first tool, so most AWS-specific guidance does **not** apply.
- **Google SRE Book** — https://sre.google/sre-book/table-of-contents/ — SLO/observability and "graceful
  degradation" principles align with the optional-dependency + health-cache design.

> The three above are listed for completeness and were not fetched in full during this pass; they are
> well-known canonical sources. Do not treat their inclusion as a deep citation.
