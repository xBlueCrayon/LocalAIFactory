# RBAC Matrix

Concrete capability-by-role mapping for LocalAIFactory, as enforced in R2-P0B. Roles are a total
order: **Viewer (0) < Analyst (1) < Admin (2)** (`Core/Enums/Enums.cs`, `UserRole`). Every higher
role inherits everything a lower role can do.

All enforcement is **server-side**, in the controller, before the action body runs
(`SecuredController.RequireAdminAsync` / `RequireProjectAsync`). UI hiding is never the control.
Project-scoped capabilities additionally require an explicit `ProjectAccess` grant — **absence of a
grant denies** — except for Admins, who bypass the project allow-list.

---

## Capability matrix

| Capability | Viewer | Analyst | Admin | Enforcement |
|---|:---:|:---:|:---:|---|
| Sign in (authenticated, enabled account) | ✅ | ✅ | ✅ | Negotiate auth + deny-by-default |
| View a project (overview/profile) | ✅¹ | ✅¹ | ✅ | `RequireProjectAsync` |
| Query / search symbols within a project | ✅¹ | ✅¹ | ✅ | `RequireProjectAsync` |
| View coverage / gap report | ✅¹ | ✅¹ | ✅ | `RequireProjectAsync` |
| View dependency / relationship graph | ✅¹ | ✅¹ | ✅ | `RequireProjectAsync` |
| View impact analysis | ✅¹ | ✅¹ | ✅ | `RequireProjectAsync` |
| Import a repository / ZIP into a project | ❌ | ✅¹ | ✅ | `RequireProjectAsync` + role |
| Run consolidation / re-embedding | ❌ | ✅¹ | ✅ | `RequireProjectAsync` + role |
| Install / update a Knowledge Pack | ❌ | ❌ | ✅ | `RequireAdminAsync` |
| Grant / revoke project access | ❌ | ❌ | ✅ | `RequireAdminAsync` |
| Manage users (create, change role, disable) | ❌ | ❌ | ✅ | `RequireAdminAsync` |
| View the audit trail | ❌ | ❌ | ✅ | `RequireAdminAsync` |

¹ Project-scoped: requires both the role **and** an explicit `ProjectAccess` grant for that
project. A new Viewer/Analyst with no grants sees no projects.

`✅` = permitted, `❌` = denied (returns HTTP 403 + `AccessDenied` view, audited as `AuthDenied`).

---

## Role definitions

### Viewer (0)
Read-only observer. Can sign in and, **for projects they are granted**, view the project and run
non-mutating queries (search, coverage, graph, impact). Cannot import, install packs, manage
users, or read the audit trail. This is the default role for every new account.

### Analyst (1)
Everything a Viewer can do, plus **operational** work on granted projects: importing repositories
and running consolidation / re-embedding that produces derived knowledge. Still cannot perform
estate-wide administrative actions (pack install, user management, project grants, audit).

### Admin (2)
Full control. Manages users and roles, grants/revokes per-project access, installs Knowledge
Packs, and reads the audit trail. Admins **bypass the per-project allow-list** (they can reach any
project) but their actions are audited identically.

---

## Deny-by-default rules

1. **New users** are created as `Viewer`, enabled, with **no** `ProjectAccess` rows. They can see
   nothing project-scoped until an Admin grants access.
2. **No `ProjectAccess` row = denied.** Visibility is an explicit allow-list, not an inference from
   role.
3. **Disabled accounts** (`UserAccount.Enabled = false`) are treated as unauthorized for all gated
   actions.
4. **Unknown / unmapped project ids** are authorized against the caller's grants, not trusted — the
   IDOR guard (see `Security-Model.md` §4) returns 403 for a project the caller may not access.

---

## Admin-only actions (summary)

These always require `RequireAdminAsync` and are never reachable by Viewer or Analyst:

- Install / update Knowledge Packs (`KnowledgePackInstalled` audit event).
- Grant / revoke project access (`AccessGranted` / `AccessRevoked`).
- Change a user's role / disable a user (`RoleChanged` / `UserDisabled`).
- Read the audit trail.

---

## Notes & gaps

- `AccessLevel.Write` exists in the model but is **reserved**; current project grants operate at
  `Read`. Write-level differentiation is not yet a behavioral distinction.
- There is **no external IdP / group-sync**. Roles and grants are managed in-app by Admins, not
  driven from AD groups. This is a deliberate pilot-scope choice (see `Known-Limitations.md`).
- The matrix above reflects enforced behavior; when adding a new gated action, wire it to
  `RequireAdminAsync` / `RequireProjectAsync` and add a row here.
