# Enterprise Auth Integration Plan

> **Status: DESIGN + minimal-hooks roadmap.** A concrete, additive plan for adding enterprise SSO on top
> of the implemented Windows-auth + RBAC + project-ACL model **without breaking it**. Companion to
> `docs/SSO-IdP-Readiness.md` and `docs/Claims-Roles-Mapping.md`; built on `docs/Security-Model.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. Goals and non-goals

**Goals**
- Add Entra ID / OpenID Connect as an enterprise authentication front-door.
- Keep Windows auth and dev auth working unchanged (opt-in by config).
- Reuse the existing `UserAccount` / `UserRole` / `ProjectAccess` model and server-side enforcement.
- Audit SSO sign-ins and denials through the existing append-only trail.

**Non-goals (this plan)**
- Multi-tenant SaaS (single-tenant private pilot remains the reality).
- The platform implementing its own password store or MFA (MFA stays with the IdP).
- SAML as a first deliverable (conceptual; OIDC first).

## 2. Current wiring (the baseline this plan extends)

- `SecurityStartup.AddPilotSecurity` → `auth.AddNegotiate()` (Windows) in non-Development; `DevAuthHandler`
  in Development only, with the unit-tested `GuardDevAuth` failsafe.
- Deny-by-default at the framework level; `SecuredController` enforces `RequireAdminAsync` /
  `RequireProjectAsync` server-side; denials return 403 + `AccessDenied` view and are audited.

The plan adds a scheme and a mapping step; it does not alter the above.

## 3. Phased delivery

### Phase A — additive seams (low-risk, no behaviour change)

1. **`UserAccount.ExternalSubjectId`** (nullable) — a stable IdP subject identifier so an OIDC/SAML user
   maps to an existing account independent of `DOMAIN\user`. Additive migration only.
2. **`Security:AuthScheme`** config (`Windows` default, `Oidc` opt-in) selecting the front-door scheme.
3. **Claims-mapping configuration** (see `docs/Claims-Roles-Mapping.md`) — a config object mapping IdP
   roles/groups to `UserRole` and project grants. Dormant unless OIDC is selected.

Phase A ships without enabling OIDC; existing deployments are unaffected.

### Phase B — OIDC front-door

1. Register the OpenID Connect handler **alongside** Negotiate, activated only when
   `Security:AuthScheme = Oidc`. Configuration: authority (Entra tenant), client id, client secret (via
   the existing Data Protection / secret-store discipline — **never** committed), scopes, callback path.
2. On sign-in, resolve the principal to a `UserAccount` by `ExternalSubjectId`:
   - **existing account** → use it;
   - **no account** → provision per policy (default: `Viewer`, **no** project grants — deny-by-default,
     exactly as today for new Windows users).
3. Map claims → `UserRole` + `ProjectAccess` per the mapping policy.
4. Audit the sign-in (`AuthSuccess`) or denial (`AuthDenied`).

### Phase C — provisioning & lifecycle (optional, later)

- **SCIM** or scheduled directory sync for automated provisioning/deprovisioning.
- Group-driven project access (a directory group → a set of project grants).
- These are conceptual; not required for a first SSO pilot.

### Phase D — SAML (conceptual)

- For estates standardised on SAML 2.0, add a SAML handler behind the same scheme-selection seam, mapping
  SAML assertions to the same `UserAccount`/role/project model. Lower priority than OIDC.

## 4. Admin bootstrap and break-glass

- **Bootstrap admin:** a configured known subject (or a retained local Windows admin) is granted Admin so
  a fresh SSO deployment has an administrator before any directory mapping exists.
- **Break-glass:** Windows auth may remain enabled as a secondary scheme, or a documented local emergency
  admin exists, so an IdP outage cannot lock out administration. Break-glass sign-ins are **audited** and
  expected to be rare and reviewed.

## 5. Secrets and configuration discipline

- OIDC client secrets / certificates follow the existing rule: **no secrets in the repo**; they live in
  environment variables or a secret store, encrypted at rest via Data Protection where applicable.
- Committed example config uses placeholders only; the no-secrets release audit still applies.

## 6. Testing strategy

- **Unit:** claims-mapping is a pure function (claims → `UserRole` + project grants) and must be unit
  tested, like the existing `GuardDevAuth` and IDOR regression tests.
- **Guard tests:** assert that selecting `Oidc` does not disable server-side enforcement, and that dev
  auth remains physically absent in production.
- **Integration:** an end-to-end OIDC sign-in against a real (test) Entra tenant resolving to the correct
  role and project grants — this is the capture that advances the scorecard.

## 7. What must not regress

- Deny-by-default, the IDOR guard, server-side enforcement in `SecuredController`.
- Windows auth and the dev-auth production guard.
- The append-only audit trail.
- MSSQL-only operation (SSO is a front-door, not a core dependency).

## 8. Honest status

Design + a small additive-hooks plan. No OIDC/SAML code exists today. Scorecard: SSO/IdP **Low / design**;
proof for advancement is a captured OIDC sign-in mapping to roles/project ACLs against a real IdP with
Windows/dev auth intact.
