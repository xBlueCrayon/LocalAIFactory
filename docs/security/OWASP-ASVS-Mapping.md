# OWASP ASVS Mapping (Self-Assessment)

This maps LocalAIFactory's **actual, evidenced** controls to OWASP Application Security Verification
Standard (ASVS) themes. It is a **self-assessment**, not a certification or an independent
verification. ASVS requirement text is **not** reproduced; themes are summarised in original wording.

Status legend: **PASS** = control in place with evidence; **PARTIAL** = partially in place;
**GAP** = not yet in place. Each row records evidence and a fix path.

> Honest baseline gaps (apply across the table): no external penetration test; TLS in the pilot is
> **self-signed**; application-level authentication is **dev-grade behind IIS** (the real
> authentication boundary is IIS Windows auth, not the app); no SSO yet.

---

## V1 — Architecture, design, threat modeling

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Layered, dependency-controlled architecture | PASS | Eight projects, no dependency cycles; `Core` references nothing; documented in `docs/01-Architecture.md` and CLAUDE.md. | Maintain a build-time dependency test. |
| Secure design contract | PARTIAL | CLAUDE.md acts as an enforced contract (no blocking calls on request path; least-privilege DB). | Add a recorded threat-modeling artifact. |

## V2 — Authentication

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Server-side authentication | PASS (at IIS) | Windows / Negotiate authentication **proven at IIS** for the deployed estate. | Document the auth boundary explicitly. |
| Application-level auth strength | PARTIAL | App auth is dev-grade and relies on the IIS Windows-auth boundary. | Harden app-level auth; add SSO (OIDC) — see Keycloak/Entra patterns. |
| Credential storage / no secrets | PASS | No secrets in repo; API keys encrypted at rest via Data Protection; `keys/` git-ignored. | Periodic secret-scan in the build. |

## V3 — Session management

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Session handling | PARTIAL | Sessions ride on the IIS/Windows-auth boundary; cookie attributes set deliberately for any cross-site case. | Verify `SameSite`/`Secure` and timeout policy app-wide. |

## V4 — Access control

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Least-privilege data access | PASS | App-pool SQL login is least-privilege; `is_sysadmin = 0`. | Keep role grants minimal per query needs. |
| Role/permission model (RBAC) | PARTIAL | Role/claims mapping documented; approval-gated actions. | Publish an explicit role/permission matrix; consider ABAC. |

## V5 — Validation, sanitization, encoding

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Input handling | PARTIAL | MVC model binding/validation; parameterised EF Core queries (no string SQL). | Add systematic input-validation review per endpoint. |
| Output encoding | PARTIAL | Razor encodes by default; client markdown via `marked.js`. | Confirm no unsafe HTML injection paths in rendered markdown. |

## V7 — Error handling and logging

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Audit logging | PASS | Append-only audit model (`docs/Audit-Model.md`); AI output provenance/approval. | Add hash-chained tamper-evidence. |
| Diagnostic logging | PASS | `RequestTimingMiddleware` per-request timing; documented hang detection. | Add structured metrics/correlation IDs. |

## V8 — Data protection

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Sensitive data at rest | PASS | Keys encrypted via Data Protection; data-protection plan documented. | Add a data-classification taxonomy. |
| Local-first data minimisation | PASS | Runs fully locally (MSSQL-only mode); no internet/GPU dependency. | Maintain the no-exfiltration posture. |

## V9 — Communications / TLS

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Transport security | PARTIAL | HTTPS available; pilot uses a **self-signed** certificate. | Move to a CA-issued/internal-PKI certificate for production. |

## V10 — Malicious code / dependency

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Dependency hygiene | PARTIAL | Controlled dependency set; security audit reports 0-HIGH. | Add automated dependency scanning evidence. |

## V14 — Configuration

| Area | Status | Evidence | Fix path |
|---|---|---|---|
| Secure configuration | PASS | No secrets in config; environment-variable overrides; additive-migration discipline. | Continue config-source logging at startup. |
| Independent security testing | GAP | No external penetration test performed. | Commission an independent test against this mapping. |

---

## Overall posture

LocalAIFactory presents **credible, evidenced** application-security controls at the
architecture, authentication-at-IIS, least-privilege, audit, secrets, and data-protection levels,
with **honest PARTIAL/GAP** marks for app-level auth strength, self-signed TLS, dependency scanning,
and the absence of an external penetration test. This document is a self-assessment, not an ASVS
certification.
