# Production Security Checklist

A practical pre-production and ongoing security checklist for LocalAIFactory deployments. It maps each
item to the project's **actual evidence** and marks honest gaps. Companion documents:
`OWASP-ASVS-Mapping.md` and `NIST-SSDF-Mapping.md`.

Status legend: **PASS** / **PARTIAL** / **GAP**.

---

## Identity and access

- [x] **PASS** — Windows / Negotiate authentication proven at IIS for the deployed estate.
- [ ] **PARTIAL** — Application-level authentication is dev-grade behind the IIS auth boundary.
  *Fix path:* harden app auth; add SSO (OIDC via Keycloak/Entra) — see the integration patterns.
- [x] **PASS** — App-pool SQL login is least-privilege; `is_sysadmin = 0` confirmed.
- [ ] **PARTIAL** — RBAC role/permission model documented but not a fine-grained matrix/ABAC.
  *Fix path:* publish an explicit role/permission matrix.

## Secrets and data protection

- [x] **PASS** — No secrets in the repository; connection strings via environment/local override.
- [x] **PASS** — API keys encrypted at rest via Data Protection; `keys/` is git-ignored.
- [x] **PASS** — Local-first: runs in MSSQL-only mode with no internet/GPU dependency (no data exfiltration path).
- [ ] **PARTIAL** — No formal data-classification taxonomy.
  *Fix path:* add classification and demonstrate lineage import → approval → retrieval.

## Transport security

- [ ] **PARTIAL** — HTTPS available; pilot uses a **self-signed** certificate.
  *Fix path:* issue a CA-signed / internal-PKI certificate for production; track expiry and binding.
- [x] **PASS** — Authenticated health probes run over HTTPS (firsthand: HTTP probes drop Windows credentials).

## Audit and provenance

- [x] **PASS** — Append-only audit model in place (`docs/Audit-Model.md`).
- [x] **PASS** — AI output provenance and approval lifecycle documented and enforced.
- [ ] **PARTIAL** — Audit is append-only by design but not hash-chained/tamper-evident.
  *Fix path:* add hash-chained audit entries with a verification check.

## Build, dependencies, and release

- [x] **PASS** — Security audit reports **0 HIGH** findings.
- [ ] **PARTIAL** — No automated dependency-vulnerability scanning evidence.
  *Fix path:* add scheduled dependency scanning to the build.
- [x] **PASS** — Release packaging includes checksums (`checksums/`).
- [ ] **PARTIAL** — Previous-artifact retention for rollback is documented but partly manual.
  *Fix path:* guarantee retained artifacts + pre-release backup in automation.

## Runtime resilience (security-relevant availability)

- [x] **PASS** — Core pages render in MSSQL-only mode; Qdrant/Ollama optional and gated.
- [x] **PASS** — Health read from a cached snapshot; no blocking external calls on the request path.
- [x] **PASS** — Documented anti-patterns prevent page-hang DoS (no `GroupBy(_ => 1)`, no large-text list loads).
- [ ] **PARTIAL** — No explicit timeout/retry/circuit-breaker policy library.
  *Fix path:* add and test resilience policies; prove core pages render with every optional service down.

## Configuration and deployment hardening

- [x] **PASS** — Secure defaults; optional services off by default; additive-migration discipline.
- [x] **PASS** — File/folder permissions scoped (least-privilege Modify on writable paths like `keys/`).
- [ ] **PARTIAL** — Reverse-proxy/forwarded-headers must be configured per environment if a proxy is used.
  *Fix path:* configure forwarded headers with known proxies when a proxy terminates TLS.
- [ ] **PARTIAL** — Upload/request-size limits should be set explicitly and kept as low as feasible.

## Independent assurance

- [ ] **GAP** — No external penetration test performed.
  *Fix path:* commission an independent security test against the ASVS/SSDF mappings.
- [ ] **GAP** — No formal vulnerability-disclosure process.
  *Fix path:* publish a disclosure/contact policy.

---

## Honest summary

Evidenced strengths: Windows auth at IIS, least-privilege SQL (`is_sysadmin=0`), append-only audit,
no secrets, 0-HIGH security audit, and strong availability/degradation controls. Honest gaps:
**no external pen-test**, **self-signed TLS** in the pilot, **app-level dev-grade auth behind IIS**,
**no SSO yet**, no automated dependency scanning, no hash-chained audit, and no formal
vulnerability-disclosure process. This checklist is a self-assessment, not a certification.
