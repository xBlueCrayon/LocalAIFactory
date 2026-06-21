# Production-Readiness Gate — Result

**Date:** 2026-06-21 · `scripts/production/verify-production-readiness.ps1` (live)

## FINAL CLASSIFICATION: **PILOT_READY**

**PASS = 19 · PARTIAL = 6 · BLOCKED = 5 (operator 2 / external 2 / customer 1) · FAIL = 0.**

All hard **technical** gates pass; the remaining gaps are **operator/external/customer** (not code).

## Per-area

| # | Area | Class | # | Area | Class |
|---|---|---|---|---|---|
| 1 | Build | **PASS** | 16 | OWASP/ASVS mapping | **PASS** |
| 2 | Tests | **PASS** | 17 | NIST/SSDF mapping | **PASS** |
| 3 | Release package | **PASS** | 18 | Load test | PARTIAL |
| 4 | Knowledge packs | **PASS** | 19 | 100-system benchmark | **PASS** |
| 5 | LocalDB proof | **PASS** | 20 | Docs/API cross-check | PARTIAL |
| 6 | SQL Express proof | **PASS** | 21 | Knowledge governance | **PASS** |
| 7 | IIS proof | **PASS** | 22 | Local-LLM governance | **PASS** |
| 8 | HTTPS proof | PARTIAL (self-signed) | 23 | Issue/fix pack | **PASS** |
| 9 | Windows-auth proof | **PASS** | 24 | License enforcement | BLOCKED_OPERATOR |
| 10 | Production env config | PARTIAL (dev-auth behind IIS) | 25 | External security review | BLOCKED_EXTERNAL |
| 11 | SQL least privilege | **PASS** | 26 | Entra/OIDC tenant | BLOCKED_EXTERNAL |
| 12 | Backup/restore | **PASS** | 27 | Customer pilot signoff | BLOCKED_CUSTOMER |
| 13 | Rollback | **PASS** | 28 | Production host evidence | BLOCKED_OPERATOR |
| 14 | Support bundle | **PASS** | 29 | Monitoring/alerting | PARTIAL |
| 15 | Security audit | **PASS** (0 HIGH) | 30 | Incident response | PARTIAL |

## What PILOT_READY means (honest)

- Every **technical** gate that this workstation can satisfy is **green** (0 FAIL): build, tests, release,
  knowledge, LocalDB, SQL Express, IIS, Windows-auth, least-priv SQL, backup/restore, rollback, support,
  security (0 HIGH), ASVS/SSDF mappings, 100-system benchmark, knowledge + LLM governance, issue/fix pack.
- The **PARTIAL**s are honest gaps the host cannot fully close: self-signed (not CA) TLS; app runs dev-auth
  behind IIS (not app-RBAC under Windows identity); load is a **local simulation**; docs/API cross-check is
  **sampled**; monitoring/incident-response are runbook-level (no SIEM/on-call).
- The **BLOCKED**s require a human/operator, an external party, or the customer — see
  `HUMAN_OPERATOR_REQUIREMENTS_FOR_FULL_PRODUCTION.md` and `FULL_PRODUCTION_BLOCKERS.md`.

**The docs do not claim "production ready."** This gate returns `PILOT_READY`, and that is what every report
states. `FULL_PRODUCTION_READY` is reachable only after the BLOCKED items are satisfied on a real production
host with external review and customer signoff.
