# LAF Enterprise ERP — Security Review Report

**Date:** 2026-06-21 · **Scope:** generated ERP proof product (clean-room)

## What is implemented

- **RBAC** via a `RolePermission` matrix (read/create/write/submit/approve/cancel per doctype),
  enforced by `RbacService` and the workflow engine.
- **Maker/checker separation** — a submitter cannot approve their own document; over-threshold amounts
  require a separate approver role. Test-proven.
- **Audit trail** — every create/submit/approve/reject/cancel records an actor + timestamp + details.
- **Immutable financial records** — submitted documents cannot be edited; corrections go through
  cancel + reversal, never silent edits.
- **No secrets in source/config**; money is `decimal`, not float.
- **Domain errors return HTTP 400**, not stack traces, via the API guard wrapper.

## Honest limitations (NOT production-secure)

| Severity | Finding | Note |
|---|---|---|
| High | **Authentication is a dev cookie** (`erp_user`/`erp_roles`), not real auth | Production must bind `ICurrentUser` to Windows/Negotiate or SSO/OIDC. This is the single biggest gap. |
| High | No credential storage, password hashing, 2FA, or session security | Out of scope for the POC |
| Medium | No transport security configured in-app | Runs over HTTP for the demo; production needs TLS termination + HSTS |
| Medium | API write surface is partial and only Sales-Invoice-create is RBAC-gated at the endpoint | Other writes go through tested services; expand endpoint gating before exposure |
| Medium | No field-level (permission-level) or row-level (user) permissions | ERPNext has these; not implemented |
| Low | No rate limiting / anti-CSRF on the dev login form | Dev-only form |
| Low | CSV import trusts column structure (validated per-row, errors captured) | Bounded; no formula-injection handling on export |

## Verdict

The **business controls** (RBAC, maker/checker, audit, immutability) are genuinely implemented and
tested. The **platform security** (authentication, transport, secrets management) is explicitly a
DEV posture and must be hardened before any real deployment. **No security claim is made beyond the
control model.** This mirrors the LocalAIFactory external-proof stance: real auth/TLS/pen-test are
operator/external-owned and not faked here.
