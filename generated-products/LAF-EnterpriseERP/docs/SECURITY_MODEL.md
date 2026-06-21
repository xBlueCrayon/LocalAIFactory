# Security Model

LAF Enterprise ERP enforces **authorization** (who may do what to which document type) and
**segregation of duties** (maker/checker) in the service layer. It does **not** yet implement
**authentication** (proving who a user is) — identity is selected through an explicit dev-auth
mechanism intended for the proof, with a clearly-marked path to production SSO.

## Identity: `ICurrentUser`

All security decisions read the ambient identity from `ICurrentUser`:

```csharp
public interface ICurrentUser
{
    string Username { get; }
    IReadOnlyList<string> Roles { get; }
}
```

- **Web (`HttpCurrentUser`)** resolves identity from two cookies, `erp_user` and `erp_roles`
  (pipe-separated). With no cookies it **defaults to `admin` with all six roles**
  (`System Manager`, `Sales User`, `Purchase User`, `Accounts User`, `Accounts Manager`,
  `Stock User`).
- **Login page (`/Home/Login`)** is the dev-auth UI: a form that writes those cookies. It explicitly
  states "Dev auth for the proof: pick an identity + roles. Production binds this to Windows/SSO."
- **Tests (`CurrentUser`)** use a mutable implementation so a single test can switch from maker to
  checker via `TestHost.Login(username, roles)`.
- **Startup** runs with no `HttpContext`, so `HttpCurrentUser` resolves to the all-roles admin —
  which is why the seeded demo transactions auto-approve.

> **This is DEV authentication.** There is no password, no credential store, no session token, and no
> verification — anyone can assume any identity and roles by posting the login form. See
> *What is NOT secured* below.

## Authorization: the role-permission matrix (`RbacService`)

`RbacService.Can(docType, action)` checks the acting user's roles against the `RolePermissions` table,
which maps `(RoleName, DocType)` to six capabilities:

| Capability | Action string |
| --- | --- |
| `CanRead` | `read` |
| `CanCreate` | `create` |
| `CanWrite` | `write` |
| `CanSubmit` | `submit` |
| `CanApprove` | `approve` |
| `CanCancel` | `cancel` |

`Can` returns `true` if **any** of the user's roles grants the capability for that doctype;
`Demand(docType, action)` throws a `DomainException` (→ HTTP 400) when not permitted.

### Seeded matrix (`DataSeeder.SeedRolesAndWorkflows`)

Roles seeded: `System Manager`, `Accounts User`, `Accounts Manager`, `Stock User`, `Sales User`,
`Purchase User`. Demo users: `admin`, `alice` (Sales), `bob` (Accounts Manager).

| DocType | Role | Read | Create | Write | Submit | Approve | Cancel |
| --- | --- | :-: | :-: | :-: | :-: | :-: | :-: |
| SalesInvoice, SalesOrder | **Sales User** | ✓ | ✓ | ✓ | ✓ | – | – |
| SalesInvoice, SalesOrder | **Accounts Manager** | ✓ | – | – | ✓ | ✓ | ✓ |
| PurchaseInvoice, PurchaseOrder | **Purchase User** | ✓ | ✓ | ✓ | ✓ | – | – |
| PurchaseInvoice, PurchaseOrder | **Accounts Manager** | ✓ | – | – | ✓ | ✓ | ✓ |
| PaymentEntry, JournalEntry | **Accounts User** | ✓ | ✓ | ✓ | ✓ | – | – |
| PaymentEntry, JournalEntry | **Accounts Manager** | ✓ | – | – | ✓ | ✓ | ✓ |

The matrix encodes the intent that the role which **creates/submits** a document is **not** the role
that **approves** it.

## Segregation of duties: maker/checker

Enforced in `WorkflowService` independently of the RBAC matrix:

1. **Submit role** — the submitter must hold the workflow definition's `SubmitRole`, else
   `DomainException`.
2. **Approver role** — the approver must hold `ApproverRole`, else `DomainException`.
3. **Maker ≠ checker** — when `MakerCannotApprove` is true (the default and seeded value), a user
   **cannot approve a document they submitted** (`WorkflowInstance.SubmittedBy` is compared,
   case-insensitively, to the acting username). Even a user who legitimately holds *both* the submit
   and approver roles is blocked from approving their own document.
4. **Mandatory rejection reason** — `Reject` throws unless a non-blank reason is supplied.

See [WORKFLOW_MODEL.md](WORKFLOW_MODEL.md) for the full lifecycle and
[BUSINESS_CONTROLS.md](BUSINESS_CONTROLS.md) for the tests that prove each rule.

## What is NOT secured (be honest before deploying)

The following are **deliberately not implemented** in this build:

- **Authentication.** No real login, no password or credential storage, no MFA, no session/JWT — the
  dev cookie is trusted as-is. Anyone can self-assign any role.
- **Encryption.** No encryption at rest or of any field; no secrets management.
- **Field-level / row-level permissions.** Authorization is per-(role, doctype, action) only. There
  is no per-field masking and no per-company / per-territory row scoping.
- **API authorization.** The REST endpoints under `/api` do **not** call `RbacService`; they execute
  as whatever identity `HttpCurrentUser` resolves (defaulting to all-roles admin). RBAC and
  maker/checker are enforced by the **service layer / workflow engine**, which the document
  orchestrators invoke — but the read endpoints and the create endpoint are not gated by an explicit
  permission demand.
- **Audit immutability guarantees.** `AuditEvents` is append-only by convention (the code only
  inserts), but there is no database-level tamper protection.

### Production path

The intended production change is to bind `ICurrentUser` to the **Windows / SSO** identity (the login
page itself documents this) and to add an authentication middleware in front of the controllers and
`/api` group, after which the existing RBAC matrix and maker/checker engine apply unchanged.
