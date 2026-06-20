# Security Model

This document describes the security model of LocalAIFactory as it is actually implemented and
tested (R2-P0B). It is deliberately honest: it states what is enforced, how it is enforced, and
what is **not** yet covered. Read it alongside `RBAC-Matrix.md`, `Audit-Model.md`, and
`Secrets-Handling.md`.

Scope: this is a **private, local-first pilot** platform for a banking middleware estate. It is not
internet-facing and is not a multi-tenant SaaS. The security model is sized for that reality.

---

## 1. Authentication

### Production: Windows Authentication (Negotiate)

In a deployed (non-Development) environment the application authenticates users with **Windows
Authentication** via the Negotiate scheme (NTLM/Kerberos), terminated by IIS. There are **no
application passwords**: identity is the caller's Windows account (`DOMAIN\user`). The
`UserAccount` row holds only the app-level role and lifecycle (enabled/disabled) — never a
credential.

Wiring lives in `src/LocalAIFactory.Web/Security/SecurityStartup.cs`
(`AddPilotSecurity` → `auth.AddNegotiate()`).

### Development: dev auth, guarded OUT of production

For local development and automated tests a `DevAuthHandler` (`Security/DevAuthHandler.cs`) lets a
developer assume an identity/role via configuration (`Security:DevIdentity`) or an `X-Dev-User`
header. This handler is **only registered when the environment is Development**, and a pure,
unit-tested guard fails startup if it is ever requested elsewhere:

```
SecurityStartup.GuardDevAuth(isDevelopment, devAuthRequested)
  → throws InvalidOperationException if devAuthRequested && !isDevelopment
```

Tests assert all three cases (dev+requested ok, prod+not-requested ok, prod+requested throws):
`tests/LocalAIFactory.Tests/SecurityTests.cs` (`Dev_auth_guard_throws_outside_development`).
The dev scheme is therefore **physically absent** from a production process, not merely disabled
by a flag.

### Deny-by-default at the framework level

Every endpoint requires an authenticated user unless it explicitly opts out with
`[AllowAnonymous]`. A request with no resolvable identity is rejected before any controller runs.

---

## 2. Authorization — RBAC

Application roles are a total order (`Core/Enums/Enums.cs`):

```
public enum UserRole { Viewer = 0, Analyst = 1, Admin = 2 }
```

- **Viewer** — read/observe.
- **Analyst** — Viewer plus operational actions (import, run queries that mutate derived state).
- **Admin** — full control: user/role management, project grants, knowledge-pack installation,
  audit access.

New users are **deny-by-default**: created as `Viewer` with `Enabled = true` but **no project
grants**. They can see nothing project-scoped until an Admin grants access.

The concrete capability mapping is in `RBAC-Matrix.md`.

---

## 3. Per-project access (ProjectAccess)

Project visibility is an **explicit allow-list**, not an inference. `ProjectAccess`
(`Core/Entities/Security.cs`) is a row joining a user to a project with an `AccessLevel`
(`None`/`Read`/`Write`; `Write` reserved). **Absence of a row denies.** Admins bypass the
allow-list. Granting and revoking are Admin-only and audited (`AccessGranted` / `AccessRevoked`).

---

## 4. Server-side enforcement

Authorization is enforced **server-side, in the controller, before the action body** — hiding a
button or a nav link is never treated as a control. Access-controlled controllers derive from
`SecuredController` (`src/LocalAIFactory.Web/Controllers/SecuredController.cs`) and gate each
action:

```csharp
if (await RequireAdminAsync("install knowledge pack") is { } denied) return denied;
if (await RequireProjectAsync(projectId, "view coverage") is { } denied) return denied;
```

- `RequireAdminAsync` — 403 unless the caller is an enabled Admin.
- `RequireProjectAsync(projectId, …)` — 403 unless `Access.CanAccessProjectAsync` returns true.

A denial returns **HTTP 403** and the shared `AccessDenied` view, and is **audited** as
`AuthDenied` before returning. Auditing is wrapped so it can never break the request path.

### IDOR guard

Insecure-direct-object-reference was a real fixed bug: previously a user could reach a project by
guessing its id. Project-scoped actions now call `RequireProjectAsync(projectId, …)` so the id is
authorized against the caller's grants, not trusted because it parsed. A **regression test**
exercises a cross-project id and asserts 403.

---

## 5. Auditing

Every privileged action and every denial is written to an **append-only** `AuditEvent`
(`who / Windows identity / IP / event type / action / target / project / when / detail`). The
trail is never updated or deleted in normal operation. See `Audit-Model.md` for the schema, the
audited-event list, and the tamper-evidence gap.

---

## 6. Secrets & Data Protection

- API keys are **encrypted at rest** using ASP.NET Core Data Protection. The key ring is
  persisted to a git-ignored `keys/` directory (`Program.cs`,
  `AddDataProtection().PersistKeysToFileSystem(...).SetApplicationName("LocalAIFactory")`).
- Connection strings carrying credentials live in **environment variables or a secret store**,
  never in committed config. Committed `appsettings.*.example.json` use Trusted Connection or
  placeholders only.
- `.env`, `keys/`, and local overrides are git-ignored. Absence of committed secrets is checked by
  tests and a release-time no-secrets audit.

Full handling guidance is in `Secrets-Handling.md`.

---

## 7. Optional-AI / MSSQL-only boundary

The platform must run with **only SQL Server present** — no GPU, no internet, no Ollama, no
Qdrant. The security posture does not depend on any external AI service. AI outputs are advisory
and never authoritative (see `Compliance-Disclaimers.md`); the security controls above hold
identically in MSSQL-only mode.

---

## 8. Autonomous-action safety boundary

The autonomous execution path is gated, not free-running: a command **allow/deny policy**,
**dry-run by default**, and **commit/push gated** behind explicit approval. The `ControlledExecutor`
**never self-promotes** its own privilege or approval state. See `Known-Limitations.md` for what
the executor does and does not yet do (no real fix/rollback loop).

---

## 9. Honest gaps

These are real and intentionally listed so no one over-claims:

- **No enterprise SSO / external IdP.** Identity is Windows/Negotiate only; there is no SAML/OIDC
  federation, no SCIM provisioning, no MFA layer beyond what the Windows domain itself provides.
- **No penetration test.** No third-party offensive security assessment has been performed.
- **No external security audit / certification.** Nothing here is certified by any body.
- **No tamper-evident audit.** The audit log is append-only by application convention and DB
  permissions, but is **not** hash-chained or cryptographically sealed; a DB admin could alter it.
- **No formal PII / data-retention / DLP policy.** Retention is operational guidance, not enforced
  controls (see `Audit-Model.md`).

Each gap has a defined closing proof in `Known-Limitations.md`.

---

## 10. Verification status

Security behavior is covered by `tests/LocalAIFactory.Tests/SecurityTests.cs` and related suites;
the full suite (207 tests) passes and the validation benchmark is PASS. Tested behaviors include
the dev-auth guard, deny-by-default project access, the IDOR regression, and the no-secrets audit.
