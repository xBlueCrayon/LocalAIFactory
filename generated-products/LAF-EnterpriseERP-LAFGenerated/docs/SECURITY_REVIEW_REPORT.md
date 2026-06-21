# Security Review Report — LAF Enterprise ERP V2 (LAF-generated)

**Subject:** `generated-products/LAF-EnterpriseERP-LAFGenerated`
**Scope:** application-level security controls of the generator-emitted ERP. This review is honest
about what is real and tested versus what is a deliberate gap. It mirrors the security posture of
LAF Enterprise ERP V1: the *governance* controls are genuine; the *platform* security controls are
not present and must be added before any non-pilot use.

> **One-line verdict: governance controls (RBAC, maker/checker, audit, immutability) are real and
> tested; authentication, transport security, and data protection are absent. NOT production-secure.**

---

## 1. What is real and tested

### 1.1 Role-based access control (RBAC)
`RbacService.Can(docType, action)` evaluates a seeded role-permission matrix
(`RolePermission` rows: `CanRead/Create/Write/Submit/Approve/Cancel` per `RoleName`+`DocType`).
`Demand` throws `DomainException` on failure. The matrix is seeded in `DataSeeder` (e.g. Sales User
may create/submit a SalesInvoice but not approve it; Accounts Manager may submit/approve/cancel).
A parameterized test (`ControlsAndValidationTests.|| Rbac_matrix_enforced`) asserts six role/action
combinations resolve as expected.

### 1.2 Maker/checker separation
`WorkflowService.Approve` throws *"the submitter may not approve their own document"* when the
acting user equals `SubmittedBy` and the workflow definition has `MakerCannotApprove = true` (the
seeded default). Amounts at/below a per-document threshold auto-approve; above threshold a **separate
user holding the approver role** is required. `RealLifeScenarioTests` proves the maker cannot
approve their own invoice and that a distinct checker can.

### 1.3 Mandatory rejection reason
`WorkflowService.Reject` throws if the reason is null/whitespace. The document returns to Draft and
a `WorkflowApproval` row with the reason is recorded.

### 1.4 Immutable posted documents
Submitted financial/stock documents cannot be silently edited: `SalesService.EditRate` refuses to
modify a non-draft invoice, and corrections flow through `Cancel` → reversing GL + stock entries
(`AccountingService.ReverseVoucher`, `StockService.ReverseVoucher`). GL entries themselves are
append-only (`GLEntry`, `StockLedgerEntry` are immutable by construction).

### 1.5 Append-only audit trail
`AuditService.Record` writes one `AuditEvent` per state-changing action with `PerformedBy`,
`Action`, `EntityType/Id`, `Details`, and UTC timestamp. The real-life scenario asserts ≥ 5 audit
events and the presence of a SalesInvoice "Approve" event.

**These five controls are the security value of the product and they hold up.**

---

## 2. What is NOT present (the gaps)

### 2.1 Authentication — DEV-AUTH ONLY (blocker)
Identity is whatever the caller puts in two **plaintext cookies**:

```csharp
// HomeController.Login (POST)
Response.Cookies.Append("erp_user",  username ?? "admin");
Response.Cookies.Append("erp_roles", roles    ?? "System Manager");
```

`HttpCurrentUser` reads `erp_user` / `erp_roles` from the request cookies and, **when absent,
defaults the caller to `admin` with every role** (System Manager, Sales, Purchase, Accounts,
Accounts Manager, Stock User). There is:

- no password / credential verification,
- no session integrity (cookies are unsigned, unencrypted, client-settable),
- no anti-forgery (CSRF) token on the login form,
- no lockout, no MFA, no SSO.

Anyone who can reach the app is effectively `admin`. `Program.cs` documents this explicitly as a
*dev auth mode for the proof* and notes that "production would bind `ICurrentUser` to the
Windows/SSO identity." That binding does not exist yet.

### 2.2 Transport security — none
No HTTPS redirection/HSTS is configured in `Program.cs`. The dev-auth cookies and all API traffic
travel in clear text unless a reverse proxy terminates TLS externally.

### 2.3 Data protection — none
No at-rest encryption, no column-level protection for any field, no secrets vault. (No secrets are
*committed* either — connection strings come from configuration — but there is no protection
mechanism for sensitive data once stored.)

### 2.4 API authorization — incomplete
The RBAC `Demand` gate is wired on exactly one write path (`POST /api/sales-invoices` calls
`rbac.Demand("SalesInvoice","create")`). The remaining API endpoints — including all GET collections
that expose customer/supplier/financial data, and the generated catalog POSTs — perform **no
authentication or authorization check**. There is no API key, bearer token, or scheme.

### 2.5 Other absent controls
- No rate limiting / throttling.
- No input sanitization beyond domain validation (Razor auto-encodes output, which mitigates stored
  XSS in the read views, but there is no explicit policy).
- No audit of *reads* (only state changes are audited).
- No security headers (CSP, X-Frame-Options, etc.).

---

## 3. Threat-model summary

| Threat | Status |
|---|---|
| Privilege escalation within the app | **Mitigated** by RBAC + maker/checker (tested) — *given a trusted identity* |
| Unauthorized document approval / self-approval | **Mitigated** (maker≠checker enforced) |
| Tampering with posted ledgers | **Mitigated** (immutable + reversal-only) |
| Repudiation of actions | **Mitigated** (append-only audit) |
| Spoofing identity | **NOT mitigated** — cookies are client-settable; default is admin/all-roles |
| Network eavesdropping | **NOT mitigated** — no TLS |
| Unauthorized API data access | **NOT mitigated** — most endpoints are unauthenticated |
| CSRF on dev-auth | **NOT mitigated** |
| Data theft at rest | **NOT mitigated** — no encryption |

---

## 4. Required before any non-pilot deployment

1. Replace dev-cookie auth with real authentication bound to the estate's **Windows/SSO** identity;
   remove the admin/all-roles default.
2. Enforce **TLS** (HTTPS redirect + HSTS) and sign/encrypt session state.
3. Apply the RBAC `Demand` gate (or an authorization policy) to **every** API endpoint, not just
   sales-invoice create.
4. Add **CSRF** protection to all state-changing forms/endpoints.
5. Add **at-rest encryption** / secrets management per the estate's data-protection standard.
6. Add security headers, rate limiting, and read-audit where regulation requires it.

---

## 5. Conclusion

The generated ERP's **governance security is genuine and test-backed** — RBAC, maker/checker,
mandatory reject reasons, immutability, and audit all work as designed. Its **platform security is
intentionally absent**: dev-cookie auth (defaulting to admin), no TLS, no encryption, no MFA/SSO,
and largely unauthenticated APIs. This is acceptable for a controlled pilot on a trusted network and
matches V1's posture, but the product **must not be exposed to untrusted users or real data** until
the items in §4 are implemented.
</content>
