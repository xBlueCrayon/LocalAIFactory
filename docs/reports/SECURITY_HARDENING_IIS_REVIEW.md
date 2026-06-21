# Security Hardening — IIS Review (Phase 3)

**Date:** 2026-06-21 · Reviewed against the live Mode A IIS pilot + the repository.

| # | Control | State | Evidence |
|---|---|---|---|
| 1 | IIS site auth | **Windows Authentication ON, Anonymous OFF** | `appcmd list config` → `windowsAuthentication enabled=true`, `anonymousAuthentication enabled=false` |
| 2 | Negotiate enforced | **401 without creds → 200 with Windows creds over HTTPS** | `14-iis-windows-auth-proof` PASS |
| 3 | App pool identity | **`ApplicationPoolIdentity`** (virtual account, no interactive logon) | `appcmd list apppool /text:processModel.identityType` |
| 4 | SQL permissions | **least privilege** — `db_datareader + db_datawriter + EXECUTE`, **`is_sysadmin=0`**, no `db_owner` | live `sys.database_role_members` query |
| 5 | HTTPS binding | present (`https/*:8443:`) — **self-signed pilot TLS** (not production CA) | `appcmd list site /text:bindings` |
| 6 | Secrets in config | **none** — Windows trusted SQL auth; no inline passwords/keys in tracked `appsettings*.json` | secret scan over tracked appsettings |
| 7 | Repo secret scan | **0 hardcoded secrets** in tracked source/config | `security-audit.ps1` → "no hardcoded secrets matched" |
| 8 | Forbidden artifacts | **0** bin/obj/db/model/key/zip tracked; **0** files > 5 MB | `security-audit.ps1`, cleanliness gate |
| 9 | Data Protection keys | git-ignored (`keys/`); not committed | `.gitignore` |
| 10 | RBAC / deny-by-default | `UserRole` (Viewer<Analyst<Admin) + per-project `ProjectAccess`, server-side, IDOR-guarded | SecurityTests (unit) |
| 11 | Audit | append-only `AuditEvent` (who/what/when/which-project/denied) | Audit-Model.md, live AuditEvents |
| 12 | AI-proposal governance | propose-never-overwrite; AI output is never authoritative | KnowledgePack permanence tests |
| 13 | Release ZIP safety | `verify-release-package` asserts no secrets/db/model files | gate PASS |
| 14 | Security audit overall | **PASS — 0 HIGH** (3 INFO: "review destructive pattern" notes on known maintenance scripts) | `security-audit.ps1` |

## OWASP-style notes (honest)

- **A01 Broken Access Control:** mitigated — server-side RBAC + `ProjectAccess` deny-by-default + IDOR
  regression test. The **application** currently runs dev-auth behind IIS for page reachability; wiring app
  RBAC to the IIS Windows identity (production scheme + bootstrap admin) is the remaining app-config step.
- **A02 Cryptographic Failures:** HTTPS proven, but **self-signed** localhost cert (pilot). Production needs
  a CA-issued cert + HSTS + TLS-version/cipher policy.
- **A05 Security Misconfiguration:** app pool least-privilege, no sysadmin, no secrets in config. IIS
  request filtering / removing the `Server` header / custom error pages are recommended production hardening
  (documented, not yet applied).
- **A07 Identification & Auth Failures:** Windows/Negotiate proven at the IIS layer; enterprise SSO/OIDC is
  **not** implemented (design only).
- **A09 Logging & Monitoring:** append-only audit + ANCM event log; no SIEM integration.

## Honest statement

This is a **pilot** security posture, **not** a production security certification and **not** an external
penetration test. The app runs **dev-auth behind IIS** for page reachability; the IIS transport (HTTPS) and
auth (Windows/Negotiate) layers are proven, and the SQL runtime identity is least-privilege. The biggest
open security items remain: a **CA-issued certificate + TLS hardening**, **app-level RBAC under the IIS
Windows identity**, **enterprise SSO**, and an **external security review / penetration test**.
