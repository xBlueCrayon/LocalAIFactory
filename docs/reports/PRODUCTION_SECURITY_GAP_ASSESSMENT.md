# Production Security Gap Assessment

**Date:** 2026-06-21 · Companion to `docs/security/OWASP-ASVS-Mapping.md`, `NIST-SSDF-Mapping.md`,
`Production-Security-Checklist.md`.

Controls mapped to **actual evidence**; gaps are explicit. No control is marked complete without proof.

| Control area | Status | Evidence | Gap / fix path |
|---|---|---|---|
| Authentication | PARTIAL | IIS Windows/Negotiate 401→200 over HTTPS proven | app runs dev-auth behind IIS; bind app RBAC to Windows identity + SSO |
| Authorization / access control | PASS | server-side RBAC + per-project ACL + IDOR test | — |
| Session management | PARTIAL | Windows-auth (no app sessions) | review cookie/session under production auth |
| Input validation / output encoding | PARTIAL | MVC model binding; knowledge search parameterised | full input-validation audit pending |
| File upload safety | PARTIAL | import pipeline robustness | size/type limits hardening |
| Logging / audit | PASS | append-only AuditEvent (who/what/when/denied) | tamper-evidence (hash chain) future |
| Secrets | PASS | no secrets tracked; Data Protection keys git-ignored; Trusted_Connection | Vault/Key Vault for production |
| Cryptography / TLS | PARTIAL | HTTPS proven | **self-signed** pilot cert → CA cert + HSTS + cipher policy |
| Database security | PASS | least-priv app-pool login (`is_sysadmin=0`, datareader/datawriter+EXECUTE) | migration/runtime split documented |
| Error handling | PARTIAL | no stack traces leaked in pilot; 0 HTTP 500s under load | production error pages |
| Dependency / supply chain | PARTIAL | pinned packages; no forbidden artifacts | dependency scanning in CI |
| Deployment config | PARTIAL | Mode A/C executed | production env config + Server host |
| Backup / restore | PASS | backup OK + restore VERIFY OK | retention policy + scheduled job |
| AI governance | PASS | propose-never-overwrite; LLM proposal-only; hallucination-refusal proven | — |
| Release artifact integrity | PASS | verify-release-package + checksum | signed artifacts future |
| **External penetration test** | **GAP** | internal `security-audit` 0 HIGH only | **external pen-test required** |
| **Enterprise SSO/OIDC** | **GAP** | design + read-only validators | real tenant integration required |

## Verdict

Security is **pilot-grade with 0 HIGH internal findings** and several real controls proven (least-priv SQL,
Windows-auth, audit, AI governance). The two hard external gaps — **external penetration test** and
**enterprise SSO** — plus production TLS hardening, are the path to a production security posture. These are
recorded as `BLOCKED_EXTERNAL_REQUIRED` in the production-readiness gate.
