# Enterprise Giant-Solution Reasoning Benchmark

**Status:** subordinate to `MASTER_VISION.md` (authoritative). This document describes a synthetic
benchmark and makes no product, certification, or compliance claim.

> **No vendor clone. No certification.** This benchmark uses **original, synthetic** schemas and
> services that model only the **public, high-level** pattern of each enterprise family. It is **NOT**
> a clone of, and makes **NO** certified-compatibility claim for, SAP, Microsoft Dynamics / Dataverse /
> Power Platform, Salesforce, ServiceNow, Oracle / NetSuite, Temenos, Finastra, Mambu, FIS, Fiserv,
> Jira, Confluence, Power BI, Tableau, GitHub Copilot, or Sourcegraph. No proprietary vendor schema,
> UI, API, or documentation is reproduced. No compliance, regulatory, financial, or fraud guarantee is
> expressed or implied.
>
> **LocalAIFactory is NOT a replacement for SAP, NOT a replacement for Microsoft Dynamics, NOT a
> replacement for Salesforce, NOT a replacement for any core-banking system, and NOT a replacement for
> ServiceNow.** It is a local-first code-and-knowledge reasoning platform. The only thing this
> benchmark measures is whether the platform can reason over public pattern families using its own
> structural code graph and grounded design knowledge.

---

## 1. What was tested, and why

LocalAIFactory's defining capability is a curated, approved project memory combined with a
deterministic **C#↔SQL symbol graph** (the same "code-symbols" bridge used elsewhere in the product).
The question this benchmark answers is narrow and honest:

> Given synthetic services and schemas that represent the *generic, widely-taught* shape of large
> enterprise solutions, can the platform reason like an enterprise solution consultant — locating code,
> tracing impact, and giving grounded design guidance — **without** cloning any proprietary product and
> **without** overclaiming?

This matters because the platform's intended use is importing and evolving real banking middleware
estates. Before trusting it on real systems, we need a reproducible, source-controlled measurement of
how far its reasoning actually reaches, and where it stops.

The benchmark deliberately separates two very different kinds of "answer":

1. **Structural answers** — mechanically derived from the symbol graph and **graph-proven**.
2. **Advisory answers** — grounded design reasoning over the fixture entities and the approved
   knowledge base, **not** graph-executed.

Keeping these apart is the whole point: it lets us claim the structural answers strongly (they are
reproducible and regression-guarded) while keeping the advisory answers honestly bounded.

---

## 2. Why synthetic fixtures

Real vendor products were **not** used, on purpose:

- **No vendor cloning.** Reproducing a proprietary schema, UI, API, or documentation would be both a
  legal and an honesty problem. The fixtures model only the public, high-level pattern that any
  textbook or architecture course would describe (e.g. "order-to-cash", "maker/checker", "incident
  lifecycle"). No proprietary artefact is copied.
- **Reproducibility.** A synthetic fixture is deterministic. The same harness over the same fixture
  produces the same graph every time, so the score is a stable regression signal, not a one-off demo.
- **Source control.** The fixtures live in the repository
  (`benchmarks/fixtures/enterprise-giant-patterns`) and are guarded by a golden file, so any
  regression in reasoning shows up as a failing benchmark rather than a quiet drift.
- **No data risk.** No real customer, payment, sanctions, or incident data is present. Every record
  shape is invented.

---

## 3. The 10 pattern families considered

The fixture models the public pattern of ten enterprise families. Each names a few well-known products
**only to identify the pattern family**, never as a compatibility claim.

| # | Family | Public pattern modelled | Fixture surface |
|---|--------|-------------------------|-----------------|
| 1 | CRM (Dynamics / Dataverse) | customer, contact, account, opportunity, stage history | `enterprise-crm-schema.sql` |
| 2 | CRM (Salesforce) | lead → opportunity → discount approval | `enterprise-crm-schema.sql` |
| 3 | ERP (SAP / Business One / S4) | order-to-cash, procure-to-pay, inventory valuation, GL | `enterprise-erp-schema.sql` |
| 4 | Finance / procurement (Oracle / NetSuite) | purchase-order approval, goods receipt, GL posting | `enterprise-erp-schema.sql` |
| 5 | ITSM (ServiceNow) | incident lifecycle, change request, SLA breach | `enterprise-itsm-schema.sql` |
| 6 | Core banking (Temenos / Finastra / Mambu / FIS / Fiserv) | payment instruction, sanctions screening, maker/checker, settlement | `enterprise-core-banking-schema.sql` |
| 7 | Workflow / docs (Jira / Confluence) | approval + review-gate workflow patterns | scenario questions (advisory) |
| 8 | Code-intelligence (Copilot / Sourcegraph) | find symbol, impact, review-before-migrate | the C#↔SQL bridge itself |
| 9 | Reporting (Power BI / Tableau) | report definition, dashboard widgets, daily snapshot | `enterprise-reporting-schema.sql` |
| 10 | Management-company / operating-manager | daily operations evidence, maker/checker proof | `enterprise-reporting-*.sql / .cs` |

The schemas (`*.sql`) define synthetic tables and stored procedures. The services (`*.cs`) provide a
synthetic C# service layer where each method names its SQL objects in a query string, so the
deterministic C#↔SQL bridge can link service → table/proc — the same pattern as the other committed
fixtures.

---

## 4. How the benchmark uses code, schema, and knowledge evidence

Every answer must be **grounded in evidence that exists in the fixture**:

- **Code evidence** — the synthetic C# service methods (`enterprise-*-services.cs`,
  `enterprise-approval-workflows.cs`).
- **Schema evidence** — the synthetic tables and stored procedures (`enterprise-*-schema.sql`).
- **Knowledge evidence** — the approved knowledge base entries that supply controls, risks, and
  limitations for the advisory answers.

For structural questions, the harness builds the real symbol graph and verifies the answer against it.
For advisory questions, the runner verifies that **every required entity actually exists** in the
fixture before the answer is accepted, and that the answer key carries controls, risks, evidence, and
limitations. An answer that references something not present in the fixture is not accepted.

---

## 5. Structural vs advisory — the central distinction

This is the most important honesty boundary in the benchmark.

### Structural (graph-proven, scored up to 100)

Structural questions are answerable **mechanically** from the C#↔SQL symbol graph using four query
modes: `find`, `dependents`, `dependencies`, and `impact`. They are verified by the harness
Proof-of-Vision and are reproducible and regression-guarded.

Examples:

- *"Find the change-approval stored procedure."* → `find` resolves `dbo.usp_ApproveChangeRequest`.
- *"Which services are impacted if the Customer entity changes?"* → `impact` over `dbo.Customer`
  returns the dependent services (n=11 in the current fixture).
- *"What code runs sanctions screening on a payment?"* → `dependents` of `dbo.SanctionsScreening`
  resolves `SanctionsScreeningService.ScreenPayment`.
- *"What does payment release depend on?"* → `dependencies` of `PaymentApprovalService.ReleasePayment`
  resolves `dbo.usp_ReleasePayment`.

These answers describe **what the code does**, not **what should be done**. They never claim to have
*performed* the action (approved a change, resolved an incident, released a payment).

### Advisory (grounded design reasoning, scored at most 90)

Advisory questions cover controls, audit evidence, lifecycle, risk, and module flow. They are grounded
in the fixture entities and the approved knowledge base, but they are **not** graph-executed. They are
capped at 90 by design, because they are consultant-style design guidance — not a mechanically proven
fact and not a delivered or certified solution.

Examples:

- *"What approval controls are required before a high-value discount is applied?"* → maker/checker
  segregation, ApproverRole authorization (grounded in `dbo.DiscountApproval`, `dbo.usp_ApproveDiscount`).
- *"What controls are required before payment release?"* → clean sanctions screen, maker/checker/approver
  segregation of duties.
- *"What is the incident lifecycle?"* → grounded narrative over `dbo.Incident`, `dbo.IncidentAudit`,
  `dbo.SlaBreach`, with no claim of certified ITIL conformance.

---

## 6. Scoring model and result

The scoring band is fixed and explicit:

| Score | Meaning |
|------:|---------|
| 0 | cannot answer |
| 25 | generic answer |
| 50 | partially grounded |
| 75 | grounded in fixture / code / schema / knowledge |
| 90 | grounded + controls + risks + evidence + limitations |
| 100 | grounded + tested (graph-proven) + reproducible + no overclaiming |

A structural answer reaches **100** only when its Proof-of-Vision passes and the answer carries
evidence and limitations. An advisory answer reaches at most **90**, and only when every required
entity exists and the answer supplies controls, risks, evidence, and limitations.

### Result

| Metric | Value |
|--------|-------|
| Total questions | 31 |
| Structural (graph-proven) | 14 |
| Advisory (design / consultant) | 17 |
| Mean score | **94.5** / 100 |
| Target | 90 |
| Structural POV failures | 0 |
| Questions scoring < 50 | 0 |
| **Result** | **PASS** |

Harness tier for the fixture: **Gold** — symbols = 329, edges = 79, Proof-of-Vision 14/14 passed.
The structural portion is also wired into the main benchmark manifest (`benchmarks/benchmarks.json`,
code **ENTGIANT**) and regression-guarded by `benchmarks/golden/ENTGIANT.json`, so it runs in the
standard suite as well as the standalone runner.

---

## 7. How to reproduce

Two commands, both from the repository root:

```powershell
# 1. Standard benchmark suite — includes the structural ENTGIANT code benchmark,
#    scored by the real structural harness and guarded by benchmarks/golden/ENTGIANT.json.
pwsh scripts\benchmark\run-benchmarks.ps1   # (or the project's standard suite entry point)

# 2. Standalone enterprise-reasoning runner — scores all 31 scenario questions
#    (14 structural + 17 advisory) and writes the results report.
pwsh scripts\benchmark\run-enterprise-reasoning-benchmark.ps1
```

The standalone runner validates the fixture, runs the **real** structural harness (the product's
C#↔SQL bridge) scoped to this fixture, scores every question against the scoring model above, and
emits `docs/reports/ENTERPRISE_REASONING_BENCHMARK_RESULTS.md`.

---

## 8. Limitations

- **Synthetic fixture.** The graph models **statically-named** SQL only. Dynamic / ORM-generated SQL
  is out of scope and is reported as a gap, not silently covered.
- **Advisory answers are not executed.** They are grounded design reasoning, capped at 90, and carry
  no certification.
- **No proprietary artefacts.** No vendor schema, UI, API, or documentation was used or reproduced.
- **Pattern-level only.** The fixtures model the public, high-level pattern of each family — not any
  specific product's behaviour, edge cases, or compliance posture.
- **No compliance / regulatory / financial / fraud guarantee** is expressed or implied by any score.

---

## 9. Disclaimers (explicit)

- **NOT** a SAP replacement.
- **NOT** a Microsoft Dynamics / Dataverse / Power Platform replacement.
- **NOT** a Salesforce replacement.
- **NOT** a core-banking replacement (Temenos / Finastra / Mambu / FIS / Fiserv).
- **NOT** a ServiceNow replacement.
- **NOT** a certified integration with any named product.
- **No** proprietary vendor compatibility claim.
- **Public-pattern reasoning only**, over original synthetic fixtures.

---

## 10. Cross-references

- Results report: `docs/reports/ENTERPRISE_REASONING_BENCHMARK_RESULTS.md`
- Comparison matrix: `docs/Enterprise-Reasoning-Comparison-Matrix.md`
- Consultant-role capability: `docs/Enterprise-Consultant-Reasoning-Capability.md`
- Known limitations: `docs/Known-Limitations.md`
- Readiness scorecard: `docs/Enterprise-Readiness-Scorecard.md`, `docs/readiness-scorecard.json`
- Fixture: `benchmarks/fixtures/enterprise-giant-patterns/` (and related
  `erp-crm-industrial`, `core-banking`, `kyc-aml-approval` fixtures)
- Authoritative vision: `MASTER_VISION.md`
