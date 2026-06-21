# Enterprise Consultant Reasoning Capability — By Role

**Status:** subordinate to `MASTER_VISION.md` (authoritative). This document explains how different
roles use the benchmark's answers. It is not a product, certification, or compliance claim.

> **No vendor clone. No certification.** Where products are named, it is **only** to identify a public
> pattern family. LocalAIFactory is **NOT** a clone of, and makes **NO** certified-compatibility claim
> for, SAP, Microsoft Dynamics / Dataverse / Power Platform, Salesforce, ServiceNow, Oracle / NetSuite,
> Temenos, Finastra, Mambu, FIS, Fiserv, Jira, Confluence, Power BI, Tableau, GitHub Copilot, or
> Sourcegraph. No proprietary vendor schema, UI, API, or documentation is reproduced. No compliance,
> regulatory, financial, or fraud guarantee is expressed or implied.
>
> **LocalAIFactory is NOT a replacement for SAP, NOT a replacement for Dynamics, NOT a replacement for
> Salesforce, NOT a replacement for any core-banking system, and NOT a replacement for ServiceNow.**

---

## 1. The two kinds of answer every role must keep apart

Throughout this document, two answer kinds appear. Every role needs to know which they are holding:

- **Structural (proven).** Derived **mechanically** from the C#↔SQL symbol graph (`find`,
  `dependents`, `dependencies`, `impact`), **graph-proven** by the harness, reproducible, and
  regression-guarded by `benchmarks/golden/ENTGIANT.json`. Scored up to 100.
- **Advisory (design).** Grounded consultant-style reasoning over the fixture entities and the approved
  knowledge base. **Not** executed; capped at 90. It is **grounded design guidance, not a delivered or
  certified solution.**

A structural answer tells you *what the code is and what it touches*. An advisory answer tells you
*what a competent consultant would consider* — it is not a sign-off, not a control attestation, and
not a compliance result.

---

## 2. Operating / operations manager

**Goal:** day-to-day control and evidence — "is the operation healthy and can I prove it?"

What the platform gives this role:

- **Which dashboard to monitor (advisory).** Grounded in
  `OperationsReportingService.OperationsDashboard` and `dbo.OperationsDailySnapshot`. This is design
  guidance about *which* surface carries the daily signal — not a live production feed.
- **Daily evidence (advisory).** What evidence is needed daily, grounded in
  `dbo.OperationsDailySnapshot` and the operations dashboard service. It identifies the evidence
  *shape*; it does **not** claim a live operations feed exists.
- **Maker/checker proof (advisory).** What audit evidence proves maker/checker/approver segregation,
  grounded in `dbo.MakerCheckerLog`. This points to where the segregation evidence lives; it is **not**
  legal proof of compliance.
- **Control checkpoints (advisory).** Controls required before sensitive actions — e.g. a clean
  sanctions screen and maker/checker/approver segregation before payment release.

**Honest boundary:** every item above is advisory. The platform shows the manager *where* the evidence
and controls are modelled, reproducibly grounded in the fixture — it does **not** run the operation,
attest the control, or guarantee compliance.

---

## 3. Technical architect

**Goal:** understand blast radius before committing to a change.

What the platform gives this role — mostly **structural, proven**:

- **Impact / blast-radius (structural).** "Which services are impacted if the Customer entity
  changes?" → `impact` over `dbo.Customer` returns the dependent services (n=11 in the current
  fixture). "What is impacted if the GL account/posting changes?" → `impact` of `dbo.GlAccount`.
- **Dependencies (structural).** "What does payment release depend on?" → `dependencies` of
  `PaymentApprovalService.ReleasePayment` resolves `dbo.usp_ReleasePayment` and the maker/checker log.
- **Reverse dependencies (structural).** "What touches the payment instruction?" → `dependents` of
  `dbo.PaymentInstruction` (n=10): submit, screen, reject, reconcile, report.

**Honest boundary:** the graph models **statically-named SQL only**. Dynamic / ORM-generated SQL is
out of scope and is reported as a gap. The architect should treat the impact set as a *proven lower
bound* on the blast radius, not a guaranteed-complete one.

---

## 4. Developer

**Goal:** find the code, know what it touches, review before changing.

What the platform gives this role — **structural, proven**:

- **Find a symbol (structural).** "Find the change-approval stored procedure." → `find` resolves
  `dbo.usp_ApproveChangeRequest`. "Find the code that resolves an incident." → resolves
  `IncidentService.ResolveIncident`.
- **What-touches-X (structural).** "What code handles high-value discount approval?" → `dependents` of
  `dbo.DiscountApproval` resolves `SubmitDiscount` and `ApproveDiscount`. "What code runs sanctions
  screening?" → resolves `SanctionsScreeningService.ScreenPayment`.
- **The impact graph for review (structural feeding advisory).** Before a migration that changes a
  shared table (`dbo.PaymentInstruction`, `dbo.Customer`), the structural impact graph produces the
  set of files to review; the advisory answer wraps that set with the review and test-gate guidance.
- **Review-before-migrate (advisory).** "Which test gates must run before approving a change?" →
  build, tests, benchmark, UI smoke, security audit — cross-linked to the project's verify-poc /
  ui-smoke / security-audit gates.

**Honest boundary:** "find" and "what-touches-X" are proven; the review-set and test-gate guidance are
advisory (design), driven by the structural graph but not themselves graph-executed. The platform
never claims to have *applied* a change — autonomous patches are dry-run-by-default, allowlist-only,
human-approved, and halt-on-failure.

---

## 5. Compliance / audit reviewer

**Goal:** trace audit evidence, confirm segregation of duties, and understand the limits.

What the platform gives this role — **advisory, grounded**:

- **Audit-evidence trails (advisory).** "What audit evidence is needed for a resolved incident?" →
  grounded in `dbo.IncidentAudit`. "What audit evidence proves maker/checker/approver segregation?" →
  grounded in `dbo.MakerCheckerLog`. The platform shows *where the evidence is modelled*; it does
  **not** assert a tamper-proof or legally admissible record.
- **Segregation of duties (advisory).** Controls before payment release (clean sanctions screen,
  maker/checker/approver segregation) and before a high-value discount (maker/checker, ApproverRole).
  These are grounded design controls, not a control attestation.
- **Flow tracing (advisory).** "What is the KYC → screening → transaction-approval / payment-release
  flow?" → grounded in `dbo.PaymentInstruction`, `dbo.SanctionsScreening`, `dbo.MakerCheckerLog`,
  `dbo.usp_SubmitPayment`, `dbo.usp_ReleasePayment`.
- **Stated limitations (always).** Every advisory answer carries its limitations explicitly. The
  reviewer should rely on the platform to *surface* the evidence model and controls — and on the
  reviewer's own judgement and the institution's processes for the actual attestation.

**Honest boundary:** none of these answers is a compliance result, a regulatory sign-off, or a
guarantee of AML / fraud detection. They are grounded design reasoning over a synthetic fixture.

---

## 6. Why the structural / advisory split protects every role

- **Structural answers are reproducible.** Any role can re-run the harness and get the same graph; the
  golden file fails the build if it drifts. This is the strongest claim the platform makes, and it is
  bounded to statically-named SQL.
- **Advisory answers are grounded but not certified.** They are accepted only when every required
  entity exists in the fixture and the answer supplies controls, risks, evidence, and limitations —
  but they are design guidance, not delivered or certified outcomes.

Keeping the two apart means no role is misled into treating a design suggestion as a proven fact, or a
proven code-graph result as a compliance attestation.

---

## 7. Disclaimers (explicit)

- **NOT** a SAP replacement.
- **NOT** a Microsoft Dynamics / Dataverse / Power Platform replacement.
- **NOT** a Salesforce replacement.
- **NOT** a core-banking replacement (Temenos / Finastra / Mambu / FIS / Fiserv).
- **NOT** a ServiceNow replacement.
- **NOT** a certified integration with any named product.
- **No** proprietary vendor compatibility claim.
- **Public-pattern reasoning only**, over original synthetic fixtures, with no compliance /
  regulatory / financial / fraud guarantee.

---

## 8. Cross-references

- Benchmark overview: `docs/Enterprise-Giant-Solution-Reasoning-Benchmark.md`
- Comparison matrix: `docs/Enterprise-Reasoning-Comparison-Matrix.md`
- Results report: `docs/reports/ENTERPRISE_REASONING_BENCHMARK_RESULTS.md`
- Known limitations: `docs/Known-Limitations.md`
- Readiness scorecard: `docs/Enterprise-Readiness-Scorecard.md`, `docs/readiness-scorecard.json`
- Authoritative vision: `MASTER_VISION.md`
