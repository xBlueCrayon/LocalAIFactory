# Enterprise Workflow Learning Report

**Date:** 2026-06-21
**Scope:** Authoring the `enterprise-workflows-v1` knowledge pack, synthetic workflow fixtures, and
the supporting code-generation standard for LocalAIFactory.

---

## What was built

| Artefact | Path | Notes |
|----------|------|-------|
| Pack manifest | `knowledge-packs/enterprise-workflows-v1/manifest.json` | `itemCount` 40, version 1.0.0, reviewStatus Approved |
| Knowledge items | `knowledge-packs/enterprise-workflows-v1/enterprise-workflows.json` | **40 items**, one per workflow family |
| Source registry | `knowledge-packs/enterprise-workflows-v1/source-registry.json` | **11 sources** (BPMN, ITIL-concept, maker/checker concept, FATF, Basel, ISO, NIST, OWASP, .NET docs, ISO 20022-style codes, internal practice) |
| Pattern library doc | `docs/Enterprise-Workflow-Pattern-Library.md` | all 40 families with states/roles/validations/approvals/audit/exceptions/DB sketch/service rule/anti-pattern |
| Code-generation standard | `docs/Workflow-Code-Generation-Standard.md` | entity/enum/state-machine/service/controller/UI/validation/authz/audit/notification/exception/migration/seed/test/security/rollback rules + the AVOID list + correct/anti-pattern snippets |

## Fixtures (6 files)

All under `benchmarks/fixtures/enterprise-workflows/`:

1. `workflow-core-schema.sql` — the engine schema: WorkflowDefinition, Role, Actor, Instance, Step,
   Transition, Approval, Comment, Attachment, AuditEvent, Exception, SlaRule, Notification,
   Escalation, Policy, RiskSignal, Evidence + the `usp_RecordTransition` audited-transition helper.
2. `maker-checker-workflow.sql` — distinct-actor enforcement, threshold-gated approver stage,
   mandatory rejection reason.
3. `payment-approval-workflow.sql` — validate -> screen-before-authorize -> limit-based authorization
   -> settle, with idempotency and a compliance-hold path on a screening hit.
4. `ticket-sla-workflow.sql` — SLA timers, calendar-aware breach, tiered escalation, resolution +
   closure confirmation.
5. `enterprise-workflow-services.cs` — C# service layer using `ExecuteSqlRaw` that names every SQL
   object (so the C#<->SQL bridge can answer "what touches X").
6. `enterprise-workflow-tests.cs` — xUnit-style behaviour sketch for the controls (distinct actor,
   screening order, limit enforcement, idempotency, terminal-state guards, SLA escalation,
   AI-patch self-approval block).

**Pack item count: 40. Fixture count: 6.**

## The 40 workflow families covered

maker/checker/approver, four-eyes approval, multi-level approval matrix, transaction release, payment
authorization, direct-debit mandate lifecycle, claim generation/response, rejection-code mapping, KYC
onboarding, CDD/EDD, AML screening, sanctions/PEP/adverse-media, customer risk scoring, account
opening, document collection/verification, cheque OCR review, signature/forgery review, exception
queue, incident management, change request, SLA escalation, asset intervention, parts consumption,
inventory adjustment, purchase-order approval, invoice approval, GL posting/finance approval, support
ticket, CRM lead-to-opportunity, ERP order-to-cash, ERP procure-to-pay, operational dashboard review,
daily ops-manager review, audit evidence export, backup/restore/rollback, release approval,
autonomous patch proposal, human-approval-before-AI-code-change, knowledge import/dedupe/approval,
production incident postmortem.

## What was learned

- **The shared spine.** Across all 40 families the same invariants recur: explicit lifecycle states
  (initial/allowed/terminal), segregation of duties, server-side validation, an immutable
  audit-event-per-transition, an exception path that is never a silent catch, and a control that is
  data-driven (policy/matrix) rather than hardcoded. The pack and standard make that spine explicit so
  generated code converges on it.
- **Order of controls matters.** Several families fail in practice not because a control is missing but
  because it runs in the wrong order — e.g. authorizing a payment *before* screening, or activating an
  account *before* KYC clears. The fixtures encode the correct ordering.
- **AI-change governance is a workflow too.** "Human approval before AI code change" and "autonomous
  patch proposal" reuse the exact maker/checker invariant: the proposing agent can never be the
  approver, and nothing is applied before an approved terminal state.
- **One audited-transition primitive** (`usp_RecordTransition`) keeps the audit guarantee from being
  re-implemented (and forgotten) per family.

## Honest limitations

- The fixtures are **synthetic prototypes** for benchmarking generated code. They are **not** a
  deployed or production-grade workflow engine, are **not** wired into the LocalAIFactory solution,
  and the test sketch is illustrative pseudocode that is not expected to compile/run as-is.
- Knowledge items are **original practical summaries**. They are not a regulatory control attestation
  and not legal, regulatory, AML/KYC, tax, audit, or compliance advice. States, thresholds, approval
  tiers, and any reason/return codes are **illustrative** and must be tuned to institution policy and
  applicable scheme rules.
- No vendor BPM/ERP/CRM schema, UI, or documentation, and no copyrighted standard text, is reproduced.
  Sources are registered references compiled from general/public knowledge; exact URLs and current
  text require verification before any citation.
