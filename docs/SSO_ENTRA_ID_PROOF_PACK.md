# SSO / Entra ID Proof Pack

> **Status: DESIGN + readiness — NOT implemented in this release.** Enterprise SSO (Entra ID /
> OpenID Connect) is **not** wired into the running platform. Production authentication is **Windows
> Authentication (Negotiate)** with a guarded **Development-only** dev-auth handler; RBAC
> (`Viewer < Analyst < Admin`) plus per-project `ProjectAccess` ACLs are enforced **server-side** and
> **audited**. This document is the operator-facing **checklist + procedure** to actually validate an
> Entra ID integration **later, on a real tenant**, together with the read-only validators that already
> exist in `scripts/sso`.
>
> **Authority:** subordinate to `MASTER_VISION.md`. Companion to `docs/SSO-IdP-Readiness.md` (design)
> and `docs/Claims-Roles-Mapping.md` (claims → role/project mapping).

---

## 0. The cardinal rule

> **Windows auth and dev auth must NOT break.** SSO is **additive** — a new authentication scheme
> alongside Negotiate, **selected by configuration** via `Security:AuthScheme = Windows | Oidc`,
> defaulting to **Windows**. An estate running on Windows auth today keeps working unchanged; OIDC is
> opt-in **per deployment**. Nothing in this proof pack permits removing or weakening the Windows or
> dev-auth front doors. If enabling OIDC would break either, the integration is not done.

This pack is the bridge from the **design** (`docs/SSO-IdP-Readiness.md`) to a **captured proof** on a
real tenant. It does not, by itself, change any running behaviour.

---

## 1. What exists today vs what this pack proves later

| Area | Today (implemented, in this release) | Proven later (this pack's procedure) |
|---|---|---|
| Production auth | Windows / Negotiate (NTLM/Kerberos), terminated by IIS | Entra ID / OIDC as an **additional**, config-selected front door |
| Dev/test auth | Guarded `DevAuthHandler`, physically absent outside Development | Unchanged — still works as a secondary/break-glass path |
| Authorisation | `UserRole` RBAC + per-project `ProjectAccess` allow-list, deny-by-default, IDOR-guarded, server-side | Unchanged — OIDC sign-ins resolve to the **same** model |
| Claims → role mapping | **Not implemented** (design only) | A real Entra sign-in mapping group/app-role → `UserRole` |
| Project ACL mapping | Managed in-app today | Optional claim-driven `ProjectAccess` grants, deny-by-default |
| Audit | `AuthSuccess` / `AuthDenied` `AuditEvent` rows on the Windows/dev path | Same event types recording SSO sign-ins and denials |
| `scripts/sso` validators | Present; **read-only**; report "OIDC not configured" and **exit 0** | Same scripts report **PASS** against a configured `appsettings` |

**Honest baseline:** no OIDC handler, no tenant integration, and no claims-mapping code ship in this
release. The two scripts in `scripts/sso` validate **config shape only** — they do not enable SSO, do
not contact any IdP, and never print secret values. On this release they correctly report
"OIDC not configured" and exit 0. This document does **not** claim a completed proof.

---

## 2. Entra ID app registration checklist

Performed by a tenant administrator in the **target customer's** Microsoft Entra ID, when the operator
chooses to enable OIDC on a specific host. None of this affects deployments left on Windows auth.

- [ ] **Tenant** — confirm the correct Entra ID tenant (directory) ID; record it for the `Authority`.
- [ ] **App registration** — create a new registration (e.g. `LocalAIFactory-<environment>`); record the
      **Application (client) ID**.
- [ ] **Supported account types** — choose **single-tenant** ("Accounts in this organizational directory
      only") for a private banking estate. Multi-tenant is **not** in scope (single-tenant pilot today).
- [ ] **Platform / Redirect URI** — add a **Web** platform with redirect URI
      `https://<host>/signin-oidc` (must match `Oidc:CallbackPath`, default `/signin-oidc`). Use HTTPS.
- [ ] **Front-channel logout URL** — set `https://<host>/signout-callback-oidc` so sign-out is clean.
- [ ] **ID token claims** — ensure the sign-in returns a **stable subject**:
      - `oid` (Entra object id) **or** `sub` as the stable external subject id (see §6);
      - **group claims** (`groups`) **or** **app roles** (`roles`) carrying the role/project signal.
      If the group claim would overflow, switch to **app roles** or the groups-overage pattern; the
      mapping in §6/§7 works with either.
- [ ] **Admin consent** — grant **admin consent** for the requested delegated permissions
      (`openid`, `profile`, and group/role claims) so users are not individually prompted.
- [ ] **Conditional Access / MFA** — applied at the **IdP**, not in the app (the platform does not
      implement its own MFA; MFA is inherited from Entra).

Record the tenant id, client id, redirect URI, and the chosen claim source (groups vs app roles) for the
config in §4 and the evidence in §10.

---

## 3. Config shape the app would read

When OIDC is enabled on a host, the operator adds an `Oidc` section and flips `Security:AuthScheme`.
Example shape (illustrative — the values are per-tenant):

```jsonc
{
  "Security": {
    "AuthScheme": "Oidc"            // Windows (default) | Oidc; absent => Windows
  },
  "Oidc": {
    "Authority":   "https://login.microsoftonline.com/<tenant-id>/v2.0",
    "ClientId":    "<application-client-id>",   // NOT a secret — safe in config (see §5)
    "CallbackPath": "/signin-oidc",             // must match the redirect URI in §2

    // Secret material is NEVER in committed config. Provide via env var / Key Vault / certificate.
    // "ClientSecret": "${env:LAF_OIDC_CLIENT_SECRET}",        // env-var indirection, not a literal
    // "ClientCertificateThumbprint": "<thumbprint-in-cert-store>",

    "ClaimsMapping": {
      "SubjectClaim": "oid",         // stable external id used to resolve/provision UserAccount
      "RoleMap": {                   // IdP group/app-role -> UserRole (Viewer/Analyst/Admin)
        "LAF-Admins":   "Admin",
        "LAF-Analysts": "Analyst",
        "LAF-Viewers":  "Viewer"
      },
      "DefaultRole": "Viewer",       // least-privilege fallback when no group matches
      "ProjectAccessClaim": "groups" // optional: claim carrying project-grant groups
    }
  }
}
```

The two validators check exactly this shape:

- `check-oidc-config.ps1` confirms `Security:AuthScheme`, the presence of the `Oidc` section, and the
  required keys `Authority` / `ClientId` / `CallbackPath`. It reports secret material as
  **present/absent only** and warns if `ClientSecret` looks like an inline literal.
- `validate-claims-mapping.ps1` confirms `Oidc:ClaimsMapping` carries a `SubjectClaim`, a `RoleMap` whose
  values are all valid `UserRole`s, a `DefaultRole` (recommending least-privilege `Viewer`), and reports
  whether an optional `ProjectAccessClaim` is present.

> **Hard rule:** a client secret or certificate is **never** committed to the repo or to
> `appsettings.json` in source control. Use an environment variable, Azure Key Vault, or a certificate in
> the machine store. The client **ID** is not secret and is safe in config (§5).

---

## 4. Client ID vs client secret / certificate handling

| Material | Secret? | Where it lives | Notes |
|---|---|---|---|
| **Application (client) ID** | No | `Oidc:ClientId` in config | Public identifier; safe in committed config. |
| **Client secret** | **Yes** | Env var / Key Vault — **never** committed config | `check-oidc-config.ps1` warns on an inline literal. Rotate on a schedule and on suspected exposure. |
| **Client certificate** | **Yes** (private key) | Machine/user cert store; reference by **thumbprint** | Preferred over a secret for production. Track expiry; rotate before expiry. |
| **Tenant id** | No | Embedded in `Authority` | Identifies the directory, not a credential. |

Rotation discipline:

- Treat secret/certificate expiry as an operational task with an owner and a calendar reminder.
- Rotation is a **config/credential-store** change only — no code change, no schema change, and it does
  **not** affect deployments left on Windows auth.
- After any rotation, re-run `check-oidc-config.ps1` to confirm the shape is still complete (it confirms
  presence, never the value).

---

## 5. Claims mapping (subject → identity, group/role → UserRole)

The mapping is the integration seam described in `docs/Claims-Roles-Mapping.md`. It is a **pure,
config-driven, unit-testable function** `(IdP claims) → (UserRole, set of ProjectAccess grants)`:

1. **Subject claim → stable external id.** The `SubjectClaim` (`oid` or `sub`) becomes the stable
   `ExternalSubjectId` used to resolve — and on first sign-in **provision** — a `UserAccount`. This is the
   additive, nullable hook from `docs/SSO-IdP-Readiness.md` §5; it does **not** rely on `DOMAIN\user`.
2. **Group / app-role → `UserRole`.** `RoleMap` translates IdP group/app-role values to
   `Viewer | Analyst | Admin`. **Highest match wins**; **no match ⇒ `Viewer`** (deny-by-default);
   **Admin requires an explicit configured admin group** — there is no implicit path to Admin.
3. **Default least-privilege.** `DefaultRole` is `Viewer`. An unmapped or unknown user gets least
   privilege, never more.
4. **Optional project-access claim → `ProjectAccess`.** When `ProjectAccessClaim` is set, matched groups
   map to `ProjectAccess(<project>, Read)` grants (the **union** of matched rules). Absence still denies;
   `Write` is reserved.

Cross-link: see `docs/Claims-Roles-Mapping.md` for the full rule set (provisioning behaviour,
re-evaluation on subsequent sign-ins, interaction with manually-assigned grants, and claim hygiene).

---

## 6. Role / group mapping and project ACL mapping detail

**Role mapping** (`RoleMap`):

| IdP group / app-role (example) | → `UserRole` |
|---|---|
| `LAF-Admins` | `Admin` |
| `LAF-Analysts` | `Analyst` |
| `LAF-Viewers` | `Viewer` |
| *(no match)* | `Viewer` (deny-by-default) |

- Highest match wins; a user in both `LAF-Analysts` and `LAF-Admins` is `Admin`.
- No match ⇒ `Viewer`. Never elevate an unmapped user.
- Admin is sensitive: only an explicit, configured admin group grants `Admin`.

**Project ACL mapping** (optional, via `ProjectAccessClaim`):

| IdP group (example) | → project grant |
|---|---|
| `BDM-Engineers` | `ProjectAccess(BDM, Read)` |
| `MCIB-Engineers` | `ProjectAccess(MCIB, Read)` |
| `ETAMS-Team` | `ProjectAccess(ETAMS, Read)` |

- Grants are the **union** of all matched group→project rules.
- **Absence still denies** — identical to today's Windows-user default.
- `Write` is reserved; mapping issues `Read` grants only.
- Admins bypass the allow-list (as in the current model); project mapping is still recorded for
  non-admins.
- **Group-derived** grants are managed by the mapping (added/removed as membership changes);
  **manually-assigned** grants are preserved and a conflict surfaces for Admin review rather than silent
  revocation (propose-not-overwrite).

Whatever the front door, the result flows into the **same** server-side enforcement
(`SecuredController`) — OIDC changes the door, not the authorisation point.

---

## 7. Fallback dev auth + break-glass admin

A misconfigured or unavailable IdP must never lock everyone out.

- **Windows auth stays available as a secondary scheme.** Because OIDC is config-selected and additive,
  Negotiate is not removed; a deployment can keep Windows auth as the break-glass door.
- **Dev auth** remains Development-only and physically absent in production (`GuardDevAuth` fails startup
  if dev auth is requested in production) — it is a developer aid, not a production back door.
- **Break-glass admin.** The first administrator must exist **independent of the IdP** (a seeded/granted
  Admin `UserAccount`, or a local Windows admin) so a broken SSO config cannot prevent administrative
  recovery. **Break-glass use is audited** (`AuthSuccess` with the break-glass context) and is intended
  to be rare.

If OIDC fails to initialise, the operator can recover via the secondary Windows scheme and/or the
break-glass admin, then fix the `Oidc` config and re-validate with the §11 scripts.

---

## 8. Rollback path

Rolling SSO back is a **configuration** action, not a migration:

1. Set `Security:AuthScheme` back to `Windows` (or remove it — absent defaults to Windows).
2. Leave the `Oidc` section in place but **dormant** (it is ignored when the scheme is Windows), or
   remove it. Either way, `check-oidc-config.ps1` then reports "OIDC not configured" and exits 0.
3. **No schema change is required.** The external-subject-id hook on `UserAccount` is **additive and
   nullable**; rows provisioned via OIDC retain their `UserRole`/`ProjectAccess` and remain usable, but
   the OIDC door is simply closed. No data is dropped and no migration is reverted.

Rollback must leave Windows and dev auth working exactly as before — confirm with the §11 commands and a
normal Windows sign-in.

---

## 9. Audit evidence expected

The proof, once executed on a real tenant, is captured as the following evidence:

- **`AuditEvent` rows** of type `AuthSuccess` for successful Entra sign-ins and `AuthDenied` for denied
  ones, showing the resolved `UserAccount` and `UserRole`. (These are the **same** event types already
  used on the Windows/dev path — SSO reuses them, it does not invent a new trail.)
- **Screenshot(s)** of a successful Entra ID sign-in landing in the app, with the signed-in user
  resolving to the **correct `UserRole`** (e.g. an `LAF-Analysts` member shown as `Analyst`) and, where
  configured, the expected project ACLs.
- **Validator output** from both `scripts/sso` scripts against the target host's `appsettings.json`,
  showing the configured shape passes (§11).
- **A Windows/dev-auth regression check** — a Windows sign-in still working on the same build — to prove
  SSO was additive, not a replacement.

Store the evidence with the release/handover package (not in this repo if it contains
environment-specific identifiers).

---

## 10. Validation commands

The validators are **read-only**: they neither write config, enable a scheme, nor contact an IdP. Run
from the repository root, or point at a deployed host's settings.

```powershell
# Shape check: AuthScheme + Oidc section + required keys (never prints secret values)
.\scripts\sso\check-oidc-config.ps1

# Claims-mapping shape: SubjectClaim, RoleMap -> valid UserRoles, DefaultRole, optional ProjectAccessClaim
.\scripts\sso\validate-claims-mapping.ps1

# Against a specific environment's settings:
.\scripts\sso\check-oidc-config.ps1      -SettingsFile C:\inetpub\LocalAIFactory\appsettings.json
.\scripts\sso\validate-claims-mapping.ps1 -SettingsFile C:\inetpub\LocalAIFactory\appsettings.json
```

**On this release (no `Oidc` section):**

```
== OIDC / Entra ID config check (read-only) ==
  Security:AuthScheme = (unset -> defaults to Windows)
  Oidc section        = (absent)
  RESULT: OIDC NOT configured. This is the expected, supported default for this release
          (Windows/Negotiate + guarded dev auth). ...
```

```
== Claims -> role/project mapping validation (read-only) ==
  Oidc not configured -> no claims mapping to validate (expected default for this release).
```

Both **exit 0** — they do not fail a build or gate. This is the correct, honest result today.

**Once configured on a target host, PASS looks like:**

- `check-oidc-config.ps1` — `Security:AuthScheme = Oidc`, the `Oidc` section present, `Authority` /
  `ClientId` / `CallbackPath` all `[ OK ]`, secret material reported as `[ set ]` (value never shown),
  no inline-literal warning, ending `RESULT: OIDC shape looks complete.` (exit 0).
- `validate-claims-mapping.ps1` — `SubjectClaim`, each `RoleMap` entry mapping to a valid `UserRole`,
  `DefaultRole = Viewer`, and the project-access line, ending `VALIDATE-CLAIMS-MAPPING: PASS` (exit 0).

A green shape check is **necessary but not sufficient** — it proves the config is well-formed, not that a
real sign-in works. The end-to-end proof is §11.

---

## 11. Success criteria for closing the gap

The SSO/IdP readiness gap is closed when **all** of the following are true and **captured as evidence**:

1. A **real Entra ID sign-in** (on an actual tenant) authenticates a user and resolves them to a
   `UserAccount` with the **correct `UserRole`** via the configured `RoleMap`, defaulting unmapped users
   to `Viewer`.
2. Where a project-access claim is configured, the sign-in resolves to the **correct `ProjectAccess`**
   grants (deny-by-default for unmatched groups).
3. The sign-in and any denial are recorded as `AuthSuccess` / `AuthDenied` `AuditEvent` rows.
4. Both `scripts/sso` validators report **PASS** against the target host's settings.
5. **Windows authentication and dev auth still work** on the same build — proving SSO is additive, with a
   functioning break-glass path.

> **Not yet executed.** As of this release there is no OIDC handler, no tenant integration, and no
> claims-mapping code. This document is the **procedure and the checklist** to perform and capture that
> proof later — not a record that it has been done. Until the criteria above are met and captured, the
> scorecard status remains **SSO/IdP: Low / design**.
