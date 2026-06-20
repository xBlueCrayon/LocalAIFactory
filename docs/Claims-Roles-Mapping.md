# Claims → Roles Mapping

> **Status: DESIGN.** How IdP claims (groups/roles) would map to the platform's `UserRole` and
> per-project `ProjectAccess` once SSO is added. Companion to `docs/SSO-IdP-Readiness.md` and
> `docs/Enterprise-Auth-Integration-Plan.md`; built on the implemented model in `docs/Security-Model.md`
> and `docs/RBAC-Matrix.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. The target model (already implemented)

Whatever the authentication front-door, the platform authorises against:

- **`UserRole`** — a total order: `Viewer (0) < Analyst (1) < Admin (2)`.
- **`ProjectAccess`** — a per-user, per-project allow-list with `AccessLevel` (`None`/`Read`/`Write`;
  `Write` reserved). **Absence of a row denies.**

New users are **deny-by-default**: `Viewer`, enabled, with **no** project grants. SSO mapping must respect
this — an unmapped or unknown user gets the least privilege, never more.

## 2. What gets mapped

The mapping is a **pure function**: `(IdP claims) → (UserRole, set of ProjectAccess grants)`. It is
config-driven and unit-testable (like the existing `GuardDevAuth` and IDOR tests). Inputs are the IdP's
role/group claims; outputs are the app role and project grants.

```
claims (roles[], groups[])  ──►  UserRole            (highest matched role wins)
                            └─►  ProjectAccess[]      (union of group→project grants)
```

## 3. Role mapping

A configured table maps IdP role/group values to `UserRole`:

| IdP claim (example) | → `UserRole` |
|---|---|
| `LAF-Admins` | `Admin` |
| `LAF-Analysts` | `Analyst` |
| `LAF-Viewers` | `Viewer` |
| *(no match)* | `Viewer` (deny-by-default) |

Rules:

- **Highest match wins.** A user in both `LAF-Analysts` and `LAF-Admins` is `Admin`.
- **No match ⇒ `Viewer`.** Never elevate an unmapped user.
- **Admin is sensitive.** Only an explicit, configured admin group grants `Admin`; there is no implicit
  path to Admin.

## 4. Project (group) mapping

A configured table maps IdP groups to project grants:

| IdP group (example) | → project grant |
|---|---|
| `BDM-Engineers` | `ProjectAccess(BDM, Read)` |
| `MCIB-Engineers` | `ProjectAccess(MCIB, Read)` |
| `ETAMS-Team` | `ProjectAccess(ETAMS, Read)` |

Rules:

- **Grants are the union** of all matched group→project rules.
- **Absence still denies.** A user with no matching group has no project access — identical to today's
  Windows-user default.
- **`Write` is reserved.** Mapping issues `Read` grants; `Write` is not yet a live capability.
- **Admins bypass the allow-list** (as in the current model), but project mapping is still recorded for
  non-admins.

## 5. Provisioning behaviour

On first SSO sign-in for an unknown subject:

1. Provision a `UserAccount` keyed on the stable `ExternalSubjectId`.
2. Apply role mapping (default `Viewer`).
3. Apply project mapping (default: none).
4. Audit the provisioning + sign-in.

On subsequent sign-ins, re-evaluate the mapping so directory changes (a user added to/removed from a
group) take effect — **subject to** the propose-not-overwrite discipline for any *manually* assigned
grants (see §6).

## 6. Interaction with manual grants

Some grants may be assigned manually by an Admin in-app rather than by a group. To avoid SSO silently
revoking an Admin's deliberate decision:

- **Group-derived grants** are managed by the mapping (added/removed as group membership changes).
- **Manually-assigned grants** are marked as such and are **not** removed by re-evaluation; a conflict
  (mapping would remove a manual grant) surfaces for Admin review rather than silent revocation.

This mirrors the platform-wide rule that automated processes **propose** changes to human-anchored state
rather than overwriting it.

## 7. Claim hygiene and trust

- Only claims from the **trusted, configured IdP** are honoured. Claims are validated (issuer, audience,
  signature, expiry) by the OIDC/SAML handler before mapping runs.
- The mapping **never** trusts a client-supplied role/group header (unlike the Development-only
  `X-Dev-User`, which is physically absent in production).
- The mapping is **deterministic and auditable**: given the same claims and config, it yields the same
  role and grants, and the decision is logged.

## 8. Testing

- Unit-test the pure mapping function across cases: highest-role-wins, no-match-defaults-to-Viewer,
  group-union grants, no-group-no-access, manual-grant-preserved.
- Assert that mapping output flows into the **same** server-side enforcement (`SecuredController`) — SSO
  changes the front door, not the authorisation point.

## 9. Honest status

Design only. No claims-mapping code exists; the role/project model it targets is implemented and tested.
Scorecard: SSO/IdP **Low / design**. Proof for advancement: the mapping implemented and unit-tested, and
a real OIDC sign-in resolving to the correct `UserRole` + `ProjectAccess`, captured — with Windows/dev
auth still working.
