# ERP Gold — RBAC, Company Isolation & Audit Hardening Report

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21

## Governance controls

| Control | Detail | Proving test(s) |
|---------|--------|-----------------|
| Role/permission matrix | `RolePermission` matrix gates actions by role | `Approver_without_role_is_blocked`, `Submitter_without_submit_role_is_blocked` |
| Maker-cannot-approve | A submitter may not approve their own document | `Maker_cannot_approve_own_document` |
| Separate-checker approval | A different user with the role can approve | `Separate_checker_can_approve` |
| Approval thresholds | Amounts within threshold auto-approve on submit; over threshold require a separate approver | `Amount_within_threshold_auto_approves_on_submit`, `Amount_over_threshold_requires_separate_approver` |
| Rejection reason required | Reject requires text; returns the document to draft and records the reason | `Reject_requires_a_reason`, `Reject_returns_document_to_draft_and_records_reason` |
| Append-only audit | Every transition records actor + UTC + entity | `Every_transition_records_an_audit_event`, `Create_records_audit_event` |
| Unique business keys | Duplicate business codes rejected | covered in catalog + import tests |
| Concurrency | `RowVersion` optimistic concurrency on documents (SQL Server) | (model-level) |

Source: `WorkflowTests.cs` (9 `[Fact]`), `OpsAndImportTests.cs`, `CatalogGeneratedTests.cs`.

## Company isolation

Company scoping is enforced in **queries and services** — reports and lists are company-scoped, and cross-company access is prevented at the service layer.

## Honest limitations

- **Company isolation is enforced in queries/services, not row-level database security** (no SQL Server RLS / security policies).
- `RowVersion` concurrency is a SQL Server feature; the portable SQLite test mode does not exercise it identically.
- RBAC is application-enforced; there is no database-level role mapping.
