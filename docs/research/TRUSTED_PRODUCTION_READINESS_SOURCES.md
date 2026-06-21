# Trusted Production-Readiness Source Registry

A curated registry of the sources used for LocalAIFactory's production-readiness research. Each
row records the **source type**, **URL**, **reliability level**, **topic**, and **what was
learned** (summarized in original words). URLs are real official/community/firsthand references.

**Reliability levels**

- **Official** — vendor/standards-body primary documentation (highest weight).
- **Community** — Q&A, blogs, vendor support blogs, GitHub issues/discussions (corroborating).
- **Firsthand** — observed directly in this repository's code/evidence artifacts.

> Use of these sources is corroborative. Where a source could not be re-verified at write time, the
> claim is marked **inference** in the consuming document and confidence is lowered accordingly.

## Deployment, hosting & ANCM

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| Microsoft Learn | https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/iis/?view=aspnetcore-10.0 | Official | IIS hosting | Published artifact + web.config (ANCM handler) is the unit of deploy; in-process vs out-of-process hosting models. |
| Microsoft Learn | https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-10.0 | Official | ANCM | The module bridges IIS and Kestrel; failures surface as 500.30/502.5; stdout + Application event log are the diagnostics. |
| Microsoft Learn | https://learn.microsoft.com/en-us/aspnet/core/test/troubleshoot-azure-iis?view=aspnetcore-10.0 | Official | Troubleshooting | 500.30 in-process start failure, 502.5 out-of-process failure, 500.31 runtime not found; Hosting Bundle install/repair as first fix. |
| GitHub (dotnet/AspNetCore.Docs) | https://github.com/dotnet/AspNetCore.Docs/blob/main/aspnetcore/host-and-deploy/azure-iis-errors-reference.md | Official (docs repo) | IIS error reference | Canonical 500.x/502.x reference table maintained by the product docs team. |
| Microsoft TechCommunity (IIS support blog) | https://techcommunity.microsoft.com/blog/iis-support-blog/http-error-500-30---asp-net-core-app-failed-to-start-root-cause-and-solutions/4239747 | Community (vendor) | 500.30 root causes | Bitness mismatch, missing runtime, DI/startup exceptions are the common 500.30 causes. |

## Authentication, identity & claims

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| Microsoft Learn | https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-10.0 | Official | Windows auth | Negotiate/Kerberos/NTLM config for IIS/Kestrel/HTTP.sys; intranet/domain use; Negotiate behind a proxy needs 1:1 connection affinity. |
| NuGet | https://www.nuget.org/packages/Microsoft.AspNetCore.Authentication.Negotiate | Official | Negotiate package | Cross-platform Negotiate handler; version-pinned to runtime. |
| Microsoft Learn | https://learn.microsoft.com/en-us/entra/identity-platform/v2-protocols-oidc | Official | Entra/OIDC | OIDC authorization-code flow, app registration, redirect URIs, ID/access tokens and claims. |
| Keycloak docs | https://www.keycloak.org/docs/latest/server_admin/ | Official (OSS IdP) | OIDC/claims | Generic OIDC role/group → app claim mapping pattern (corroborates the Entra flow vendor-neutrally). |

## TLS / transport security

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| Microsoft Learn | https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0 | Official | HTTPS/HSTS | Enforce HTTPS + HSTS in production only; HSTS not recommended in dev/localhost. |
| OWASP | https://cheatsheetseries.owasp.org/cheatsheets/HTTP_Strict_Transport_Security_Cheat_Sheet.html | Official (OWASP) | HSTS | max-age, includeSubDomains, preload considerations; HSTS requires a trusted cert to be useful. |

## Database & migrations

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| Microsoft Learn | https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying | Official | EF migrations | Startup `Database.Migrate()` is inappropriate for production; prefer idempotent SQL scripts or migration bundles with DBA review and rollback. |
| Microsoft Learn | https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/managing | Official | Migration mgmt | Generating scripts/bundles, idempotent scripts that self-check applied migrations. |
| Microsoft Learn | https://learn.microsoft.com/en-us/sql/relational-databases/security/authentication-access/database-level-roles | Official | SQL roles | db_datareader/db_datawriter least-privilege; avoid sysadmin/db_owner in prod. |

## Secure development & verification standards

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| NIST CSRC | https://csrc.nist.gov/pubs/sp/800/218/final | Official (standard) | SSDF | Four families PO/PS/PW/RV; producer attestation context (EO 14028); maps to ASVS/800-53. |
| OWASP | https://owasp.org/www-project-application-security-verification-standard/ | Official (OWASP) | ASVS | L1/L2/L3 verification requirements; the requirements layer a pen-test verifies. |
| OWASP | https://owasp.org/Top10/ | Official (OWASP) | Top 10 | Common web risk categories used to frame the threat model. |
| SLSA | https://slsa.dev/ | Official | Supply chain | Provenance/build-integrity levels; informs SBOM + signing gap. |

## Reliability, SRE & DevOps metrics

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| Google SRE Book | https://sre.google/sre-book/evolving-sre-engagement-model/ | Official (firsthand-vendor) | PRR | Production Readiness Review as prerequisite to operate a service; verifies prod setup + operational readiness. |
| Google SRE Book | https://sre.google/sre-book/embracing-risk/ | Official | Error budgets | Error budget = 1 − SLO; reconciles velocity vs reliability. |
| Google SRE Book | https://sre.google/sre-book/service-best-practices/ | Official | Prod best practices | On-call, change management, capacity, postmortems. |
| DORA | https://dora.dev/guides/dora-metrics-four-keys/ | Official | DORA metrics | Deployment frequency, lead time, change failure rate, time to restore (velocity vs stability). |
| Google Cloud Blog | https://cloud.google.com/blog/products/devops-sre/using-the-four-keys-to-measure-your-devops-performance | Official | Four Keys | Operationalizing the four metrics. |

## Observability tooling

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| OpenTelemetry / Grafana | https://grafana.com/docs/opentelemetry/ | Official | Observability | OTLP traces/metrics/logs into Loki/Tempo/Mimir; Collector/Alloy pipeline. |
| Keycloak | https://www.keycloak.org/observability/telemetry | Official | Telemetry | OTel logs/metrics/traces export from an enterprise IdP; otel-lgtm dev stack pattern. |
| Prometheus | https://prometheus.io/docs/practices/alerting/ | Official | Alerting | Alert design (symptom-based, paging vs ticketing) — informs monitoring gap. |

## Change/incident management & audit

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| Atlassian (ITIL guide) | https://www.atlassian.com/itsm/change-management | Community (vendor) | ITIL change | RFC, change windows, CAB, backout plans. |
| AICPA | https://www.aicpa-cima.com/topic/audit-assurance/audit-and-assurance-greater-than-soc-2 | Official | SOC 2 | Trust services criteria; evidence over an audit period. |
| ISO | https://www.iso.org/standard/27001 | Official | ISO 27001 | ISMS controls + certification model. |

## Platform engineering / community failure corpus

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| GitHub (dotnet/aspnetcore) | https://github.com/dotnet/aspnetcore/issues | Community | Runtime issues | Real-world ANCM/auth/forwarded-headers bug reports and resolutions. |
| GitHub (dotnet/efcore) | https://github.com/dotnet/efcore/issues | Community | EF behavior | Migration/concurrency/query-translation edge cases. |
| Stack Overflow | https://stackoverflow.com/questions/tagged/asp.net-core | Community | Q&A | Frequency-ranked symptoms (500.19/500.30/502.5, auth, CORS, cookies). |
| InfoQ | https://www.infoq.com/ | Community | Eng leadership | Conference-derived platform-engineering / AI-in-SWE trends. |

## CMS / ERP / e-commerce integration docs (benchmark-system references)

| Source type | URL | Reliability | Topic | What was learned |
|-------------|-----|-------------|-------|------------------|
| WordPress | https://developer.wordpress.org/rest-api/ | Official | REST API | Auth + endpoint model used in integration-expectation library. |
| WooCommerce | https://woocommerce.github.io/woocommerce-rest-api-docs/ | Official | REST API | Consumer key/secret auth; order/product resources. |
| Magento | https://developer.adobe.com/commerce/webapi/rest/ | Official | REST/Web API | Token auth, ACL-scoped resources. |
| Drupal | https://www.drupal.org/docs/develop/decoupled-drupal | Official | API-first | JSON:API and decoupled patterns. |
| Odoo | https://www.odoo.com/documentation/ | Official | ERP | XML-RPC/JSON-RPC external API. |
| Frappe / ERPNext | https://frappeframework.com/docs | Official | ERP framework | REST resource API + token/session auth. |

## Firsthand (in-repository evidence)

| Source type | Path | Reliability | Topic | What was learned |
|-------------|------|-------------|-------|------------------|
| Repo artifact | docs/reports/MODE_A_IIS_SITE_APPPOOL_PROOF.md | Firsthand | IIS pilot | Site + app-pool proven on workstation IIS. |
| Repo artifact | docs/reports/IIS_HTTPS_BINDING_PROOF.md | Firsthand | TLS | HTTPS via self-signed cert proven. |
| Repo artifact | docs/reports/IIS_WINDOWS_AUTH_PROOF.md | Firsthand | Windows auth | Negotiate behind IIS proven (dev-auth path). |
| Repo artifact | docs/reports/MODE_A_SQL_EXPRESS_IIS_DB_PROOF.md | Firsthand | SQL least-priv | App login `is_sysadmin = 0`. |
| Repo artifact | docs/reports/LOAD_TEST_IIS_RESULTS.md | Firsthand | Load | 29,540 requests / 0 HTTP 500s (workstation). |
| Repo artifact | benchmarks/results/production-readiness-gate-v2.json | Firsthand | Gate | V2 gate result machine-readable. |
| Repo artifact | docs/reports/LOCAL_LLM_REASONING_PROOF.md | Firsthand | Local LLM | Reasoning proof, mean at the 90/90 cap. |
