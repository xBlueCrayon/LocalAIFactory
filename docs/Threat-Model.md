# Threat Model

A STRIDE-style threat model for LocalAIFactory as a **private, local-first, MSSQL-authoritative**
tool operating over a banking middleware estate. It is deliberately scoped to the real deployment
(internal, Windows-domain, not internet-facing, not multi-tenant SaaS) and is honest about residual
risk. It does not claim completeness and is **not** a substitute for an external assessment
(`docs/Security-Pentest-Readiness.md`).

Read with `docs/Security-Model.md`, `docs/Data-Protection-Plan.md`, and `docs/Audit-Model.md`.

---

## 1. Assets

| Asset | Why it matters |
|---|---|
| **Curated knowledge base** | Approved business rules, snippets, and curated knowledge injected first into every prompt — the platform's core value; integrity and provenance are critical |
| **Imported source code** | Legacy banking source (BDM, MCIB, ChequeXpert, ETAMS) is sensitive intellectual property and may reveal system internals |
| **Credentials / secrets** | API keys (Data Protection encrypted), DB connection strings, Windows domain identities |
| **Audit trail** | The record of who did what; its integrity underpins accountability |
| **The system of record (MSSQL)** | Authoritative store; availability and integrity of the database is the product's correctness boundary |

---

## 2. Trust boundaries

- **Browser ↔ Web app.** Authenticated Windows users over the internal network. The app is the
  policy decision and enforcement point (server-side RBAC + project allow-list).
- **Web app ↔ MSSQL.** The app trusts the database; the database trusts the app's service account.
- **Web app ↔ optional AI services (Ollama / Qdrant).** Optional, local, untrusted for correctness —
  AI output is advisory and never authoritative. Health is read from a cached snapshot, never
  synchronously on the request path.
- **Autonomous executor ↔ OS shell.** A hard allow/deny command policy mediates every command; the
  executor is dry-run by default and cannot commit/push.
- **Operator scripts ↔ host.** Diagnostics/security scripts are read-only; operator/auto scripts can
  act but are governed by the command policy and human approval.

---

## 3. STRIDE analysis

### 3.1 Spoofing (identity)

- **Threat:** An attacker impersonates a legitimate user or assumes a higher role.
- **Mitigations:** Windows Authentication (Negotiate) — identity is the domain account, no app
  passwords to steal. Dev-auth handler is physically absent outside Development and fails startup if
  requested elsewhere (unit-tested guard). Deny-by-default: unauthenticated requests are rejected.
- **Residual risk:** No MFA beyond what the Windows domain provides; no SSO/IdP federation. Domain
  account compromise is out of this tool's control.

### 3.2 Tampering (integrity)

- **Threat:** Unauthorised modification of knowledge, source, configuration, or the audit log.
- **Mitigations:** Server-side RBAC gates mutations; knowledge has an approval lifecycle; secrets via
  Data Protection; the import path rejects writes outside the intended target. Audit is append-only.
- **Residual risk:** The audit log is append-only by convention and DB permissions, **not**
  hash-chained — a DB admin could alter it (`docs/Known-Limitations.md` §4). Database-level integrity
  depends on MSSQL administration outside the app.

### 3.3 Repudiation

- **Threat:** A user denies performing a privileged action.
- **Mitigations:** Every privileged action and every denial is written to an append-only
  `AuditEvent` (who / Windows identity / IP / event / target / project / when / detail). Audit writes
  are wrapped so they cannot break the request path.
- **Residual risk:** Without tamper-evidence, a privileged DB actor could in principle remove entries.

### 3.4 Information disclosure (confidentiality)

- **Threat:** Exposure of source code, curated knowledge, secrets, or another project's data.
- **Mitigations:** Per-project allow-list (absence denies); IDOR guard authorises ids against grants
  with a regression test; large text columns are never leaked into list views; secrets are encrypted
  at rest; no secrets tracked in the repo (audited). MSSQL-only mode means no data leaves to an
  external AI service.
- **Residual risk:** No formal PII classification or DLP; transport encryption depends on deployment
  (TLS termination at IIS) — see `docs/Data-Protection-Plan.md`. Data at rest relies on OS/DB-level
  controls (e.g., disk/TDE) that are deployment responsibilities, not enforced by the app.

### 3.5 Denial of service (availability)

- **Threat:** A request or import hangs or exhausts resources, making core pages unavailable.
- **Mitigations:** The whole performance posture is hang-avoidance — bounded queries, lightweight
  list projections, no `GroupBy(_ => 1)`, separate `CountAsync`, cached health snapshot, no blocking
  external calls on the request path; `RequestTimingMiddleware` makes stalls visible
  (`docs/Performance-Optimization-Report.md`).
- **Residual risk:** Concurrent multi-user load and large-repository import under load are unmeasured
  (`docs/Load-and-Reliability-Test-Report.md`). No rate limiting is asserted.

### 3.6 Elevation of privilege

- **Threat:** A lower-privileged user gains admin capability, or the autonomous executor exceeds its
  bounds.
- **Mitigations:** Roles are a total order enforced server-side; `RequireAdminAsync` gates admin
  actions; project access cannot be self-granted (Admin-only, audited). The autonomous executor uses
  a deny-by-default command policy, runs dry-run by default, never runs denied/approval-gated
  commands, halts on first failure, and **never self-promotes** (`Promoted`/`Committed` always
  false).
- **Residual risk:** `AccessLevel.Write` is reserved, not yet behaviourally distinct from `Read`
  (`docs/Known-Limitations.md` §3). A real fix/rollback loop on production code is not yet closed.

---

## 4. Residual-risk summary

| Risk | Status | Closing proof |
|---|---|---|
| No MFA / SSO beyond Windows domain | Accepted for pilot scope | OIDC/SAML + provisioning tests |
| Audit not tamper-evident | Known gap | Per-row hash chain + verifier |
| No PII / retention / DLP policy | Known gap | Documented, enforced policy + mechanism |
| Load / concurrency / large-import unproven | Known gap | Load + import-under-load proofs |
| No external pen-test / audit | Known gap | Independent assessment + remediation |
| Write-vs-read access not behavioural | Known gap | Enforced differentiation + tests |

All entries trace to `docs/Known-Limitations.md`, which holds the authoritative closing proofs.

---

## 5. Out of scope (explicit)

- Physical security of the host and database server.
- Windows domain / Active Directory security (assumed managed by the bank's IT).
- Security of optional models obtained and run by the customer.
- Internet-facing / multi-tenant SaaS threats — the platform is neither.
