# Security (R2-P0B) — Pilot Authentication, Authorization & Audit

> **Scope: pilot-grade, not full bank-production-grade.** This makes LocalAIFactory safe enough to host
> proprietary source during a *controlled* banking pilot — authenticated users, role-based access,
> project-level authorization, admin-only import, audited views/queries, and deny-by-default. It is **not** a
> full security accreditation (see *Limitations*).

## Authentication

**Production (IIS / Windows Server):** Windows Authentication (Negotiate → NTLM/Kerberos). The app receives the
authenticated `DOMAIN\user` principal. **No passwords are stored**; no external identity provider is required.

IIS setup:
1. Install the *Windows Authentication* IIS role feature; enable Windows Authentication and **disable Anonymous
   Authentication** for the site.
2. Host the app (in-process). The Negotiate handler reads the IIS-provided identity.
3. Ensure the app pool identity can read the database (MSSQL = source of truth).

**Development / tests only:** a `Dev` authentication scheme authenticates as a configured identity. It is
**only registered when `ASPNETCORE_ENVIRONMENT=Development`** and is physically absent in Production.
`SecurityStartup.GuardDevAuth` additionally **fails startup** if `Security:UseDevAuth` is set in a
non-Development environment. Dev identity is set via `Security:DevIdentity` or an `X-Dev-User` request header
(dev only — never trust this header in Production; it is unreachable there).

## Bootstrap admin

Set the first administrator in configuration:

```json
"Security": { "BootstrapAdmin": "DOMAIN\\your-admin" }
```

On first login that identity is provisioned as **Admin** (and re-asserted as Admin on every login, for
recovery). All other first-seen users are provisioned **deny-by-default**: role **Viewer**, **no project
access**, until an Admin grants it.

## Roles

| Role | Import | Consolidate / maintenance | Manage users & access | View/query granted projects | Audit trail |
|---|---|---|---|---|---|
| **Admin** | ✅ | ✅ | ✅ | ✅ (all) | ✅ |
| **Analyst** | ❌ | ❌ | ❌ | ✅ (granted only) | ❌ |
| **Viewer** | ❌ | ❌ | ❌ | ✅ read-only (granted only) | ❌ |

## Project access

- A non-admin sees **only** projects explicitly granted to them (Admins see all).
- Enforcement is **server-side** in every project-scoped action (`SecuredController.RequireProjectAsync`); the
  UI also hides what a user can't reach, but the server is authoritative. Direct-URL access to an ungranted
  project returns **403** (and is audited as `AuthDenied`).
- Grant/revoke is Admin-only (`/Users`) and audited.

## Audit trail (append-only)

Every meaningful action writes an `AuditEvent` (who / what / when / which project / IP / denied?). Events:
`AuthSuccess, AuthDenied, ImportStarted, ImportCompleted, ProjectViewed, SymbolQueried, DependencyViewed,
ImpactQueried, CoverageViewed, AccessGranted, AccessRevoked, RoleChanged, UserDisabled,
ConsolidationStarted, ConsolidationCompleted`. The trail is **never updated or deleted**. Viewable by Admins at
`/Audit` (filter by user / event type).

## Database

Three additive tables (no curated-knowledge changes; R2-P0A coverage/gap reporting unchanged):
`UserAccount` (Windows identity → role + lifecycle), `ProjectAccess` (explicit grants), `AuditEvent`
(append-only trail).

## Limitations (documented honesty)

- **Pilot-grade, not production-grade.** Sufficient to host proprietary source for a controlled pilot; **not** a
  full security accreditation.
- **MFA is delegated to Windows/AD** — not enforced at the application layer.
- **Audit is application-level**, not tamper-proof WORM storage.
- **Project ACLs are coarse** (read/none); no symbol- or row-level authorization.
- **No public API surface is secured** — the UI is the authenticated surface.
- **Encryption at rest** relies on MSSQL/OS + Data Protection keys (`keys/`); plan IIS key-ring management.
- **Legacy controllers** (Knowledge, Chat, Projects, etc.) require authentication (deny-by-default) but are not
  yet project-ACL-scoped — the source-exposing structural screens (Graph, Coverage, Import) **are** enforced.
- **Dev/test authentication must never run in Production** — guarded two ways (Development-only registration +
  startup guard). If misconfigured, the app fails fast rather than running insecurely.
