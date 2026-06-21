# External Proof Intelligence Report

How high-end software actually achieves production readiness, external proof, and
General-Availability (GA) confidence — and how **LocalAIFactory** maps to each, with the
honest gap.

> **Scope note.** This is a research/analysis document, not a release claim. It exists to make
> the difference between *emulated/internal proof* and *external/third-party proof* explicit.
> The authoritative project gate is `docs/Production-Readiness-Gate.md`
> (gate **V2 = PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED**). Nothing here upgrades that gate.

## How to read this report

Each of the 20 categories below has three parts:

1. **External proof (industry norm)** — what a third party (auditor, customer, pen-tester, CI
   system, SRE on-call) would normally accept as evidence in a real high-end software org.
2. **LocalAIFactory today** — what we currently have, honestly described.
3. **Gap** — exactly what external/operator action still has to happen, and the residual risk.

Confidence levels used: **High** (official source or directly observed in repo), **Medium**
(well-established engineering practice, partially confirmed), **Low** (inference / not yet
externally verified).

---

## 1. ASP.NET Core deployment to IIS

**External proof (industry norm).** A reverse-proxy/in-process deployment behind IIS is normally
proven by: a published, self-contained or framework-dependent build; a working site + app-pool
(No Managed Code) binding; a clean Application event log on cold start; and a smoke test of every
top-level route returning `2xx`/`3xx` quickly. Microsoft's own guidance treats the published
artifact + `web.config` (with the ANCM handler) as the unit of deploy.
*(Confidence: High — Microsoft Learn "Host ASP.NET Core on Windows with IIS".)*

**LocalAIFactory today.** IIS hosting is proven on a developer workstation: `docs/reports/
MODE_A_IIS_SITE_APPPOOL_PROOF.md`, `DEPLOYMENT_IIS_EXECUTION_PROOF.md`, and the published-app
proofs exist; core pages return quickly under `RequestTimingMiddleware`. The deployment guide is
`docs/Windows-Server-IIS-Deployment-Guide.md`.

**Gap.** The proof is on **Windows client IIS on a workstation**, not on a provisioned **Windows
Server** with the production app-pool identity, real DNS host header, and an operator-run cold
start. Residual risk: server-only behaviours (service account ACLs, SPN, GPO). *(Medium.)*

## 2. .NET Hosting Bundle / ASP.NET Core Module (ANCM)

**External proof (industry norm).** Evidence that the **.NET Hosting Bundle** version matches the
target runtime, that `aspnetcorev2.dll` is registered, and that the chosen hosting model
(in-process vs out-of-process) starts cleanly — confirmed by the absence of `500.30` (in-process
start failure), `500.31` (runtime not found), `502.5` (out-of-process failure), and `500.19`
(bad config / module not found) in logs.
*(Confidence: High — Microsoft Learn ANCM + Azure/IIS error reference.)*

**LocalAIFactory today.** The hosting model and `web.config` are validated locally; the IIS
posture healthcheck (`IIS_PRODUCTION_POSTURE_HEALTHCHECK.md`) and event-log evidence
(`MODE_A_SUPPORT_BUNDLE_AND_EVENTLOG_EVIDENCE.md`) cover ANCM startup.

**Gap.** Hosting Bundle must be **installed by the operator on the target server**, after which a
post-install `iisreset` + cold start must show a clean ANCM start. Not yet performed on server.
*(Medium.)*

## 3. Windows Authentication / Negotiate / Kerberos

**External proof (industry norm).** On a domain intranet, real proof is: a domain-joined server,
Windows Authentication enabled in IIS (Anonymous off), correct **SPNs** for the app-pool identity,
and a domain user authenticated end-to-end via **Negotiate** resolving to **Kerberos** (not
falling back to NTLM), with the resulting `WindowsIdentity` claims visible to the app. Microsoft
explicitly warns Negotiate must not be used behind a proxy without 1:1 connection affinity.
*(Confidence: High — Microsoft Learn "Configure Windows Authentication in ASP.NET Core".)*

**LocalAIFactory today.** `docs/reports/IIS_WINDOWS_AUTH_PROOF.md` and
`MODE_A_IIS_HTTP_AUTH_HEALTHCHECK.md` show Windows/Negotiate auth working behind IIS in the pilot.

**Gap.** Proven with the **workstation/dev-auth path behind IIS**, not against a **real Active
Directory domain** with SPNs and Kerberos ticketing. NTLM-fallback vs true Kerberos is not yet
externally confirmed. This is one of the harder operator gaps. *(Medium → Low for "true Kerberos".)*

## 4. SQL Server production permissions

**External proof (industry norm).** A least-privilege application login: **not** `sysadmin`, not
`db_owner` in production beyond what migrations need, scoped to one database, with `db_datareader`/
`db_datawriter` (+ explicit `EXECUTE` where needed). Proof is the actual permission set queried on
the production instance plus a successful app run under that login.
*(Confidence: High — established SQL Server security practice.)*

**LocalAIFactory today.** Least-privilege is proven: the SQL login has `is_sysadmin = 0`
(`MODE_A_SQL_EXPRESS_IIS_DB_PROOF.md`, `DEPLOYMENT_DATABASE_PROOF.md`), and the app runs against it.

**Gap.** Proven on **SQL Express on the workstation**, not on the **production SQL instance** with
an operator-provisioned service account and the production connection string. *(Medium.)*

## 5. EF Core production migrations

**External proof (industry norm).** Production schema is normally **not** migrated by
`Database.Migrate()` at app startup — Microsoft warns that startup migration is inappropriate for
production (concurrent instances, elevated app permissions, no clean rollback). Preferred proof is
**idempotent SQL scripts** or **migration bundles** generated in CI, reviewed by a DBA, applied in
a change window, with a tested rollback.
*(Confidence: High — Microsoft Learn "Applying Migrations".)*

**LocalAIFactory today.** The app migrates + seeds on startup (documented in CLAUDE.md) — correct
for local-first single-instance use, and the 34-table schema + ModelSnapshot are consistent. We
have an upgrade/rollback runbook (`docs/Upgrade-Rollback-Runbook.md`).

**Gap.** For a multi-instance / DBA-governed production install, a **scripted (idempotent) or
bundle-based** migration path applied outside the request/startup path should be the documented
default; startup-migrate is acceptable only for the single-instance pilot. *(Medium.)*

## 6. TLS / Certificate Authority / HSTS

**External proof (industry norm).** A certificate chaining to a **trusted CA** (public or
enterprise PKI), bound to the IIS site, valid host name, modern TLS, and **HSTS** enabled in prod
(never in dev). Proof is an external TLS scan (chain valid, no weak ciphers) from a client that
did **not** manually trust the cert.
*(Confidence: High — established TLS practice; ASP.NET Core HSTS guidance on Microsoft Learn.)*

**LocalAIFactory today.** HTTPS is proven with a **self-signed certificate**
(`docs/reports/IIS_HTTPS_BINDING_PROOF.md`): the binding works and traffic is encrypted.

**Gap.** Self-signed proves *transport encryption*, **not trust**. A real **CA-issued certificate**
(enterprise PKI or public) plus an external scan is required for GA. HSTS must be confirmed on with
a correct max-age in prod only. *(Medium — this is a known, expected operator gap.)*

## 7. Microsoft Entra ID / OIDC + claims mapping

**External proof (industry norm).** A real **Entra ID (Azure AD) tenant**, an app registration,
OIDC/OAuth2 redirect URIs, and tokens whose **claims** (groups/roles/app-roles) map to the app's
RBAC — proven by signing in as a real tenant user and seeing the correct authorization decisions.
*(Confidence: High — OIDC + Entra app-registration practice; Keycloak/OIDC docs corroborate the
generic flow.)*

**LocalAIFactory today.** OIDC/Entra readiness is **designed and emulated**: `docs/SSO-IdP-
Readiness.md`, `docs/Enterprise-Auth-Integration-Plan.md`, `docs/Claims-Roles-Mapping.md`,
`docs/SSO_ENTRA_ID_PROOF_PACK.md`. The claims→RBAC mapping is specified.

**Gap.** No **real tenant** has issued real tokens to the running app; the claims-mapping is not
end-to-end verified against live Entra. This is purely external/operator. *(Low until real tenant.)*

## 8. OWASP ASVS (application security verification)

**External proof (industry norm).** A documented ASVS level (L1/L2/L3) with each verification
requirement marked pass/fail, ideally reviewed independently. ASVS is the *requirements* layer
that a pen-test then *verifies*.
*(Confidence: High — OWASP ASVS.)*

**LocalAIFactory today.** Security is documented and self-assessed: `docs/Security-Model.md`,
`docs/Threat-Model.md`, `docs/Security-Test-Checklist.md`, `docs/Final-Security-Audit-Report.md`,
`docs/08-Security.md`.

**Gap.** No **independent ASVS attestation**; the checklist is self-graded. An external reviewer
mapping our controls to ASVS L2 would close this. *(Medium.)*

## 9. NIST SSDF (SP 800-218)

**External proof (industry norm).** Evidence across the four SSDF families — **Prepare the
Organization (PO)**, **Protect the Software (PS)**, **Produce Well-Secured Software (PW)**,
**Respond to Vulnerabilities (RV)** — e.g. signed builds, protected repo, code review, tested
security, and a vulnerability-response process. Often required as a producer **attestation** for
federal buyers (EO 14028).
*(Confidence: High — NIST SP 800-218.)*

**LocalAIFactory today.** We map informally: protected `keys/` (git-ignored), Data-Protection
encryption-at-rest, code review via PRs, 240/240 tests, governance docs (`docs/AI-Governance.md`,
`docs/Prompt-Governance.md`).

**Gap.** No formal SSDF **self-attestation document** mapping each PO/PS/PW/RV task to evidence,
and no signed builds / SBOM (see §17–18). *(Medium.)*

## 10. SRE Production Readiness Review (PRR)

**External proof (industry norm).** A completed **PRR**: SLOs/SLIs defined, error budget set,
monitoring + alerting wired, runbooks present, capacity understood, and an on-call team that has
*accepted* the service. Google's SRE practice treats PRR as a prerequisite to operating a service.
*(Confidence: High — Google SRE Book.)*

**LocalAIFactory today.** Strong runbook coverage (`Support-Runbook.md`, `Database-Operations-
Runbook.md`, `Backup-Restore-Runbook.md`, `Upgrade-Rollback-Runbook.md`), readiness scorecards,
and a supportability dashboard spec.

**Gap.** No formal **SLO/error-budget** definition, no live monitoring/alerting wired to an
on-call rotation, and no signed PRR. For a local-first single-tenant tool this is lighter-weight,
but still absent. *(Medium.)*

## 11. ITIL / ITSM change & incident management

**External proof (industry norm).** Change records (RFCs) with approvals and change windows;
incident tickets with severity, timeline, and resolution; a CAB or lightweight change approval.
Proof is the ticket history in an ITSM tool.
*(Confidence: High — ITIL practice.)*

**LocalAIFactory today.** We document the *process* (`Controlled-Autonomous-Engineering-Runbook.md`,
`Upgrade-Rollback-Runbook.md`) and the autonomous approval gates.

**Gap.** No real ITSM tooling/tickets — by design this is the customer's process. We provide the
runbooks the customer plugs into their change process. *(Medium — customer-owned.)*

## 12. SOC 2 / ISO 27001 evidence

**External proof (industry norm).** An auditor-collected evidence set over a period (access
reviews, change logs, backup tests, vuln scans) culminating in a **report/certificate**. This is
organizational, not just product.
*(Confidence: High — established audit practice.)*

**LocalAIFactory today.** We provide product-level inputs an audit would consume: audit model
(`docs/Audit-Model.md`), data-protection plan, secrets handling, backup/restore evidence.

**Gap.** No auditor, no audit period, no report. This is an **organizational** effort outside the
product's control and is correctly out of scope for the product gate. *(Low / organizational.)*

## 13. External penetration-test evidence

**External proof (industry norm).** A report from an **independent** tester: scope, methodology,
findings with severities (CVSS), evidence, and a retest confirming fixes. This is the single most
load-bearing external security artifact for most buyers.
*(Confidence: High — industry norm.)*

**LocalAIFactory today.** Pen-test **readiness** is documented (`docs/Security-Pentest-
Readiness.md`) and internal hardening is done (`SECURITY_HARDENING_IIS_REVIEW.md`).

**Gap.** **No external pen-test has been performed.** Self-assessment cannot substitute. This is a
hard external blocker. *(Low until a real test + fixes + retest exist.)*

## 14. Rollback / change-window discipline

**External proof (industry norm).** A documented, **tested** rollback (app + schema) executed in a
change window with a verification step, and a backout decision criterion.
*(Confidence: High.)*

**LocalAIFactory today.** Rollback is proven locally: `docs/reports/MODE_A_IIS_ROLLBACK_PROOF.md`,
`DEPLOYMENT_ROLLBACK_PROOF.md`, `docs/Upgrade-Rollback-Runbook.md`.

**Gap.** Schema rollback for the production migration strategy (§5) and an operator-executed
rollback drill on the server are the remaining pieces. *(Medium.)*

## 15. Backup / restore (RPO / RTO)

**External proof (industry norm).** A **restore actually performed** from backup into a clean
instance, timed, with stated **RPO** (data-loss window) and **RTO** (time-to-restore), not just a
backup job that "ran".
*(Confidence: High.)*

**LocalAIFactory today.** Backup/restore is proven locally with evidence (`docs/Database-Backup-
Restore-Evidence.md`, `Backup-Restore-Runbook.md`).

**Gap.** RPO/RTO **targets agreed with the customer** and a restore drill on the production SQL
instance with the customer's retention policy. *(Medium.)*

## 16. Monitoring / alerting / on-call

**External proof (industry norm).** Live dashboards, alert rules with thresholds, paging to an
on-call rotation, and at least one alert proven to fire. Modern stacks: OpenTelemetry → Grafana
(Loki/Tempo/Mimir) or equivalent.
*(Confidence: High — OpenTelemetry + Grafana docs.)*

**LocalAIFactory today.** Request timing/health is observable in-app (`RequestTimingMiddleware`,
`IServiceHealthCache`, `HealthMonitorService`) and a **supportability dashboard** is specified
(`docs/Supportability-Dashboard-Spec.md`).

**Gap.** No external telemetry pipeline or paging is wired. For local-first this can be light, but
alert-to-on-call is absent. *(Medium.)*

## 17. GitHub release / reproducible build

**External proof (industry norm).** A tagged release with attached artifacts, **checksums**, and
ideally a **reproducible** build (same inputs → byte-identical output) so a third party can
re-derive the artifact.
*(Confidence: High.)*

**LocalAIFactory today.** Release instructions + verification exist (`docs/GitHub-Release-
Instructions.md`, `Release-Package-Verification.md`, `Published-Package-Contents.md`), checksums
are produced (`checksums/`), and post-release verification docs exist.

**Gap.** A **draft/published GitHub release** must be cut by an operator with the artifacts +
checksums attached and verified by a fresh download. Reproducibility is not formally demonstrated.
*(Medium.)*

## 18. Supply-chain security

**External proof (industry norm).** An **SBOM** (CycloneDX/SPDX), dependency vulnerability scan,
pinned/locked dependencies, and ideally signed artifacts / provenance (SLSA).
*(Confidence: High — NIST SSDF PS/PW, SLSA.)*

**LocalAIFactory today.** Dependencies are .NET/NuGet-managed; secrets are kept out of the repo;
local-first design minimizes the runtime supply-chain surface (no internet at runtime).

**Gap.** No generated **SBOM**, no automated dependency CVE scan in CI, no artifact signing.
*(Medium.)*

## 19. High-volume performance

**External proof (industry norm).** A load test at or beyond expected peak on
**production-representative hardware**, reporting throughput, latency percentiles (p95/p99), and
**zero unexpected 5xx**, sustained.
*(Confidence: High.)*

**LocalAIFactory today.** Strong internal load evidence: **29,540 requests with 0 HTTP 500s**
(`docs/reports/LOAD_TEST_IIS_RESULTS.md`, `Load-and-Reliability-Test-Report.md`, plus the
`iis-*-load-results.json` benchmarks).

**Gap.** The load was generated **on a workstation simulating** the environment, not on production
server hardware with production concurrency. Numbers are encouraging but not server-validated.
*(Medium — see the Red-Team matrix, which downgrades this.)*

## 20. Customer-pilot acceptance

**External proof (industry norm).** A **signed** pilot scope, a real customer running the system
on their data, and a **countersigned acceptance** against agreed acceptance criteria.
*(Confidence: High.)*

**LocalAIFactory today.** The full pilot/acceptance machinery is **prepared**:
`docs/Commercial-Pilot-Package.md`, `Customer-Acceptance-Test.md`, `Customer-Handover-Package.md`,
`Customer-Onboarding-Guide.md`, and the handover walkthrough.

**Gap.** **No signed customer pilot or acceptance exists.** This — together with §13 (pen-test) and
§7 (real Entra) — is the core of "external proofs supplied". *(Low until signed.)*

---

## Summary mapping

| # | Category | Internal/emulated proof | External proof outstanding | Confidence in current state |
|---|----------|-------------------------|----------------------------|------------------------------|
| 1 | IIS deploy | Workstation IIS proven | Windows Server cold start | Medium |
| 2 | ANCM/Hosting Bundle | web.config + event log local | Bundle install on server | Medium |
| 3 | Windows auth | Dev-auth behind IIS | Real AD/Kerberos + SPN | Medium / Low |
| 4 | SQL permissions | `is_sysadmin=0` on Express | Prod SQL service account | Medium |
| 5 | EF migrations | Startup-migrate (single instance) | Scripted/bundle for prod | Medium |
| 6 | TLS/CA/HSTS | Self-signed HTTPS | CA cert + external scan | Medium |
| 7 | Entra/OIDC | Designed + emulated | Real tenant tokens | Low |
| 8 | OWASP ASVS | Self-assessed checklist | Independent attestation | Medium |
| 9 | NIST SSDF | Informal mapping | Formal attestation + SBOM | Medium |
| 10 | SRE PRR | Runbooks + scorecards | SLO/error-budget + on-call | Medium |
| 11 | ITIL/ITSM | Process documented | Customer ITSM tickets | Medium |
| 12 | SOC2/ISO27001 | Audit inputs ready | Auditor + report | Low / org |
| 13 | External pen-test | Readiness documented | **Real test + retest** | Low |
| 14 | Rollback/change-window | Local rollback proven | Server drill + schema | Medium |
| 15 | Backup/restore RPO/RTO | Restore proven local | Prod drill + agreed targets | Medium |
| 16 | Monitoring/alerting | In-app health + dashboard spec | Telemetry + paging | Medium |
| 17 | Release/reproducible | Instructions + checksums | Published release verified | Medium |
| 18 | Supply-chain | Local-first, no secrets | SBOM + scan + signing | Medium |
| 19 | High-volume perf | 29,540 req / 0 500s (workstation) | Server-hardware run | Medium |
| 20 | Customer pilot | Package prepared | **Signed pilot + acceptance** | Low |

**Bottom line.** Internal/emulated proof is broad and strong; the outstanding items are almost
entirely **external, operator-, or customer-owned** (real server, CA TLS, real Entra, external
pen-test, signed pilot, license enforcement). That is exactly the meaning of gate
**V2 = PRODUCTION_READY_WHEN_EXTERNAL_PROOFS_SUPPLIED**. See
`docs/reports/NEAR_GA_RED_TEAM_CHALLENGE_MATRIX.md` for the brutal per-claim downgrade and
`docs/reports/HUMAN_INTERACTION_GA_IMPACT_MODEL.md` for the GA-% progression.

## Primary sources

- Microsoft Learn — Host ASP.NET Core on Windows with IIS / ANCM / Azure-IIS error reference:
  <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/aspnet-core-module?view=aspnetcore-10.0>,
  <https://learn.microsoft.com/en-us/aspnet/core/test/troubleshoot-azure-iis?view=aspnetcore-10.0>
- Microsoft Learn — Configure Windows Authentication in ASP.NET Core:
  <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/windowsauth?view=aspnetcore-10.0>
- Microsoft Learn — Applying / Managing EF Core Migrations:
  <https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/applying>
- OWASP Application Security Verification Standard (ASVS): <https://owasp.org/www-project-application-security-verification-standard/>
- NIST SP 800-218 (SSDF): <https://csrc.nist.gov/pubs/sp/800/218/final>
- Google SRE Book — Production Readiness Review / Embracing Risk:
  <https://sre.google/sre-book/evolving-sre-engagement-model/>, <https://sre.google/sre-book/embracing-risk/>
- DORA metrics: <https://dora.dev/guides/dora-metrics-four-keys/>
- OpenTelemetry / Grafana observability: <https://grafana.com/docs/opentelemetry/>
