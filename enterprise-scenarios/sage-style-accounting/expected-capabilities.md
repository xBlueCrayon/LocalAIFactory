# Expected Capabilities — Indigo Trading Ltd Scenario

> **Synthetic scenario, inspired-by only.** Nothing here implies vendor compatibility, equivalence,
> or certification. **Finance content is not accounting, tax, or legal advice. Mauritius statutory
> references are awareness-only.**

This file states, honestly, what LocalAIFactory **should be able to reason about today** for this
scenario versus what would require **future implementation**. The platform's job in the simulation is
**knowledge-level reasoning about an enterprise solution design** — not to be a shipping accounting
product.

---

## What LocalAIFactory SHOULD do today (knowledge-level reasoning)

These are reasoning/design tasks the platform should handle now, using approved project memory and
local models:

1. **Explain the domain model.** Describe a defensible chart-of-accounts structure, double-entry
   journals, control accounts, and how AP/AR/inventory/payroll subledgers roll into the GL.
2. **Design the posting engine.** Articulate that every financial effect comes from a source document,
   posts a balanced journal, and that document-post + GL + inventory + audit happen in one DB
   transaction with rollback on failure.
3. **Reason about controls.** Explain maker/checker, segregation of duties, deny-by-default RBAC,
   append-only audit trail, IDOR scoping, and reversal-only corrections — and *why* each matters.
4. **Reason about period close.** Describe open / soft-close / hard-close states, the validations that
   should gate a close, and reopening as a privileged audited action.
5. **Reason about inventory valuation.** Explain weighted-average (or other basis) and how a
   goods-received/issued movement recalculates cost and posts to the GL atomically.
6. **Map to the LocalAIFactory architecture.** Propose Core/Data/Web layering, EF Core entities,
   projection records for list views, policy-based authorization, and MSSQL-only operability.
7. **Identify failure modes and tests.** Enumerate unbalanced journals, closed-period posting,
   overselling, self-approval, double-submit, partial-commit rollback — and the unit/integration
   tests that prove each is handled.
8. **Produce review artifacts.** Generate acceptance criteria, test questions, risk/rollback notes,
   and a CEO/CTO summary at consultant grade.
9. **Stay disciplined on disclaimers.** Correctly flag finance items as "not advice" and Mauritius
   statutory items as "awareness-only," and refuse to claim vendor equivalence.

## What would require FUTURE implementation (not claimed today)

These are build tasks beyond knowledge-level reasoning; the platform should be **honest that they are
not done**:

1. **A working accounting module.** Actual GL/AP/AR/inventory/payroll tables, controllers, and a live
   posting engine are not part of the current LocalAIFactory codebase; they would be a new build.
2. **Statutory calculation logic.** Real Mauritius payroll/tax computation, rates, and thresholds are
   out of scope and would need a qualified professional plus implementation and validation.
3. **Bank reconciliation engine.** Matching algorithms against imported statements are design-level
   here, not implemented.
4. **PDF document generation.** Invoice/payslip rendering from templates is described, not built.
5. **Concurrency-hardened valuation.** Production-grade locking/concurrency tokens for simultaneous
   stock issues would need implementation and load testing.
6. **Statutory export format.** Only a neutral, internally-defined export is contemplated; no vendor
   or government file format is implemented or guaranteed.
7. **End-to-end UI.** The screens, workflows, and report drill-downs are specified, not delivered.

## The honest line

LocalAIFactory today is a **reasoning and project-memory platform**. For this scenario it should
**design, critique, and stress-test** an accounting/payroll/inventory solution and map it onto the
.NET 10 / MSSQL / EF Core architecture — citing approved knowledge where available. It should **not**
claim to *be* an accounting system, to compute real statutory figures, or to be compatible with any
vendor product. Any answer that overstates implemented capability, drops the disclaimers, or asserts
vendor equivalence is a failed answer.
