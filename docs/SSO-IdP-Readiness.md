# SSO / IdP Readiness

> **Status: DESIGN + minimal-hooks roadmap.** Today the platform authenticates with **Windows
> Authentication (Negotiate)** in production and a guarded dev handler in Development — see
> `docs/Security-Model.md`. Enterprise SSO (Entra ID / OpenID Connect, SAML) is **not implemented**. This
> document designs how SSO would layer on top **without breaking the current Windows/dev auth**.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. Where we are (honest baseline)

- **Production:** Windows Authentication via the Negotiate scheme (NTLM/Kerberos), terminated by IIS. No
  application passwords; identity is the caller's `DOMAIN\user`. The `UserAccount` row holds only the
  app-level `UserRole` and lifecycle, never a credential.
- **Development/test:** a `DevAuthHandler` lets a developer assume an identity/role, **physically absent**
  outside Development (a unit-tested `GuardDevAuth` fails startup if dev auth is requested in production).
- **Authorisation:** `UserRole` (Viewer < Analyst < Admin) RBAC + per-project `ProjectAccess` allow-list
  (deny-by-default, IDOR-guarded), enforced **server-side** in `SecuredController`, **audited**.

SSO does not replace any of this; it adds **additional authentication front-doors** that resolve to the
same `UserAccount` / `UserRole` / `ProjectAccess` model.

## 2. The cardinal rule

> **Windows auth and dev auth must not break.** SSO is additive: a new authentication scheme alongside
> Negotiate, selected by configuration. An estate running on Windows auth today keeps working unchanged;
> SSO is opt-in per deployment.

## 3. Target authentication options

| Option | Status | Notes |
|---|---|---|
| **Windows Authentication (Negotiate)** | **Implemented** | the current production door; unchanged |
| **Dev auth** | **Implemented** | Development-only, guarded out of production |
| **Entra ID / OpenID Connect (OIDC)** | **Design / roadmap** | the primary enterprise target; standards-based |
| **SAML 2.0** | **Conceptual** | for estates standardised on SAML; heavier, lower priority |
| **SCIM provisioning** | **Conceptual** | automated user/group provisioning; future |
| **MFA** | **Inherited** | via the IdP (Entra/AD); the app does not implement its own MFA |

## 4. How SSO slots into the pipeline

The existing pipeline is deny-by-default: every endpoint requires an authenticated user unless
`[AllowAnonymous]`. SSO adds a scheme and a **claims→role/project mapping** step (see
`docs/Claims-Roles-Mapping.md`):

```
request
  ├─ Negotiate (Windows)        ─┐
  ├─ OIDC (Entra)   [roadmap]    ├─►  authenticated principal
  └─ Dev (Development only)     ─┘        │
                                          ▼
                         resolve / provision UserAccount (by stable subject id)
                                          │
                                          ▼
                         map claims → UserRole + ProjectAccess
                                          │
                                          ▼
                         existing server-side enforcement (SecuredController), audited
```

The resolution-to-`UserAccount` step is the integration seam: whatever the front door, the platform ends
up with a `UserAccount` carrying a `UserRole`, and the **same** server-side RBAC/project enforcement runs.

## 5. Minimal hooks to add now (low-risk)

To keep SSO a configuration choice rather than a rewrite later, the following **additive** hooks are
proposed (design; see `docs/Enterprise-Auth-Integration-Plan.md` for detail):

1. **Stable external subject id** on `UserAccount` (nullable) so an OIDC/SAML subject can map to an
   existing account without relying on `DOMAIN\user`.
2. **Auth-scheme selection in config** (`Security:AuthScheme = Windows | Oidc`), defaulting to Windows, so
   no existing deployment changes behaviour.
3. **A claims-mapping policy** (config-driven) translating IdP roles/groups to `UserRole` and project
   grants.
4. **Login-event audit** for SSO sign-ins, reusing the `AuthSuccess` / `AuthDenied` `AuditEventType`s.

None of these change the current Windows/dev path; they are dormant unless an OIDC scheme is configured.

## 6. Admin bootstrap and break-glass

- **Admin bootstrap:** the first administrator must exist independent of the IdP so a misconfigured SSO
  cannot lock everyone out. Today an Admin `UserAccount` is seeded/granted operationally; under SSO, a
  configured bootstrap admin (mapped from a known subject, or a local Windows admin) provides the initial
  Admin.
- **Break-glass admin:** a documented, audited fallback — e.g. Windows auth remains enabled as a secondary
  scheme, or a local emergency admin — so that an IdP outage does not prevent administrative recovery.
  Break-glass use is **audited** and intended to be rare.

## 7. Tenant / customer isolation

The current platform is a **single-tenant, private, local-first pilot** (per the Security Model). SSO does
not by itself make it multi-tenant. If multiple customers/estates ever share an instance:

- isolation would be enforced primarily through `ProjectAccess` (and a future estate/tenant scope), not
  through the IdP alone;
- claims mapping would have to bind a user to a tenant as well as to roles/projects;
- this is **roadmap**, called out honestly in the scorecard (no multi-tenant model today).

## 8. What stays true under SSO

- Server-side enforcement in `SecuredController` is unchanged — SSO never moves authorisation to the
  client.
- Deny-by-default and the IDOR guard hold identically.
- The append-only `AuditEvent` trail records SSO sign-ins and denials.
- MSSQL-only operation is unaffected: SSO is an authentication front-door, not a dependency of core
  function.

## 9. Honest status

- **Implemented:** Windows auth, guarded dev auth, RBAC, project ACLs, server-side enforcement, audit.
- **Design / roadmap:** OIDC (Entra), SAML, SCIM, the additive hooks in §5, tenant isolation.

Scorecard: SSO/IdP readiness is **Low / design**; the exact proof to advance it is an OIDC sign-in mapping
to `UserRole`/project ACLs against a real IdP, captured — with Windows/dev auth still working.
