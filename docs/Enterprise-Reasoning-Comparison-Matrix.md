# Enterprise Reasoning Comparison Matrix

**Status:** subordinate to `MASTER_VISION.md` (authoritative). This document is an honest capability
matrix, not a product or compatibility claim.

> **No vendor clone. No certification.** The rows below name well-known products **only to identify a
> public pattern family**. LocalAIFactory is **NOT** a clone of, and makes **NO** certified-compatibility
> claim for, SAP, Microsoft Dynamics / Dataverse / Power Platform, Salesforce, ServiceNow, Oracle /
> NetSuite, Temenos, Finastra, Mambu, FIS, Fiserv, Jira, Confluence, Power BI, Tableau, GitHub Copilot,
> or Sourcegraph. No proprietary vendor schema, UI, API, or documentation is reproduced. No compliance,
> regulatory, financial, or fraud guarantee is expressed or implied.
>
> **LocalAIFactory is NOT a replacement for SAP, NOT a replacement for Dynamics, NOT a replacement for
> Salesforce, NOT a replacement for any core-banking system, and NOT a replacement for ServiceNow.**

---

## 1. How to read this matrix

For each pattern family the matrix records four honest columns:

- **Public pattern** — the generic, widely-taught shape that the synthetic fixture models.
- **LocalAIFactory structural answer (proven)** — questions answered **mechanically** from the C#↔SQL
  symbol graph and **graph-proven** by the benchmark harness (reproducible, regression-guarded). These
  reach a score of 100.
- **Advisory reasoning (design)** — grounded consultant-style design guidance over the fixture entities
  and the approved knowledge base. **Not graph-executed**, scored at most 90.
- **NOT claimed** — what this platform explicitly does **not** assert: the real vendor product,
  certified compatibility, and any compliance or regulatory guarantee.

The structural answers come from four graph modes: `find`, `dependents`, `dependencies`, `impact`.
The advisory answers are bounded design reasoning — never a delivered or certified solution.

---

## 2. Family-by-family matrix

| Family | Public pattern | LocalAIFactory structural answer (proven) | Advisory reasoning (design) | NOT claimed |
|--------|----------------|-------------------------------------------|-----------------------------|-------------|
| **CRM (Dynamics / Dataverse)** | customer, contact, account, opportunity, stage history | What data a customer-360 needs (`dependencies` → `dbo.Customer`, `dbo.Contact`, …); which services break if `dbo.Customer` changes (`impact`, n=11) | What breaks if contact email becomes mandatory (impact narrative) | The Dynamics/Dataverse product; certified integration; a real customer dataset |
| **CRM (Salesforce)** | lead → opportunity → discount approval | What code handles high-value discount approval (`dependents` of `dbo.DiscountApproval` → SubmitDiscount, ApproveDiscount) | Approval controls before a high-value discount: maker/checker, ApproverRole authorization | The Salesforce product; certified integration; guaranteed fraud prevention |
| **ERP (SAP / Business One / S4)** | order-to-cash, procure-to-pay, inventory valuation, GL | What is impacted if the GL account/posting changes (`impact` of `dbo.GlAccount` → PostJournal) | Order-to-cash module flow; impact of an inventory-valuation change | The SAP product; certified ERP module map; GAAP/IFRS correctness |
| **Finance / procurement (Oracle / NetSuite)** | purchase-order approval, goods receipt, GL posting | What services touch PO approval (`dependents` of `dbo.PurchaseOrder` → SubmitPurchaseOrder, ProcurementReport) | Procure-to-pay module flow | The Oracle/NetSuite product; certified integration; accounting-standard guarantee |
| **ITSM (ServiceNow)** | incident lifecycle, change request, SLA breach | What code creates/routes a change request (`dependents` of `dbo.ChangeRequest`); what is impacted by the incident table across lifecycle/SLA/dashboard (`dependents`, n=6) | Incident lifecycle; approvals before a change proceeds (CAB + maker/checker); audit evidence for a resolved incident | The ServiceNow product; certified ITIL conformance; a real production incident feed |
| **Core banking (Temenos / Finastra / Mambu / FIS / Fiserv)** | payment instruction, sanctions screening, maker/checker, settlement | What touches the payment instruction (`dependents`, n=10); what code runs sanctions screening; what breaks if a rejection-code mapping changes; what payment release depends on (`dependencies` → `dbo.usp_ReleasePayment`) | KYC → screening → approval/release flow; controls before payment release; impact of sanctions-rule changes | Any core-banking product; certified compatibility; settlement finality; AML/fraud guarantee; a real/current sanctions list |
| **Workflow / docs (Jira / Confluence)** | approval + review-gate workflow patterns | (covered structurally via the impact graph that drives the review-set) | What files must be reviewed before a shared-table migration; which test gates must run before approving a change | The Jira/Confluence product; certified workflow; external certification of the gates |
| **Code-intelligence (Copilot / Sourcegraph)** | find symbol, impact, review-before-migrate | Find the change-approval proc; find the code that resolves an incident; the impact/what-touches-X graph generally | Risk of an autonomous patch and how it is controlled (dry-run-by-default, allowlist-only, human approval, halt-on-failure) | The Copilot/Sourcegraph product; that autonomous changes reach production without review |
| **Reporting (Power BI / Tableau)** | report definition, dashboard widgets, daily snapshot | What reports depend on stock movement (`dependents` of `dbo.StockMovement` → StockMovementReport) | Which dashboard an operations manager should monitor | The Power BI/Tableau product; a certified BI connector |
| **Management-company / operating-manager** | daily operations evidence, maker/checker proof | (surfaced through the reporting + maker/checker structural edges) | What evidence the operations manager needs daily; what audit evidence proves maker/checker/approver segregation | A live production operations feed; legal proof of compliance |

---

## 3. What "structural / proven" means here

A structural answer is **mechanically derived** from the symbol graph the product builds and is
verified by the harness Proof-of-Vision. In the current fixture the graph carries **329 symbols** and
**79 edges**, with **14/14** Proof-of-Vision checks passing (Gold tier). These answers are:

- **Reproducible** — re-running the harness over the fixture yields the same graph.
- **Regression-guarded** — `benchmarks/golden/ENTGIANT.json` fails the build if the graph drifts.
- **Bounded** — they describe what the code *references*, not what it *should be*, and they cover
  statically-named SQL only.

A structural answer never claims to have *performed* the action. "Find the proc that approves a change"
locates the procedure; it does not approve anything.

---

## 4. What "advisory / design" means here

An advisory answer is grounded consultant-style reasoning over the fixture entities and the approved
knowledge base. It is **not** executed and is capped at 90. The runner accepts it only when every
required entity exists in the fixture and the answer supplies controls, risks, evidence, and
limitations. It is design guidance — not a delivered, tested, or certified workflow.

---

## 5. How this differs from vendor certification

This is the boundary that must not be blurred:

- **A vendor certification** asserts compatibility with a specific commercial product's actual schema,
  API, behaviour, versions, and edge cases, and is backed by that vendor's testing and (often)
  regulatory posture.
- **This benchmark** asserts only that LocalAIFactory can reason over an **original synthetic model of
  a public pattern**. It says nothing about any real product's internals, and it is backed by the
  product's own structural harness, not by any vendor.

Concretely, a passing score here means: *"the platform can find, trace, and reason about the generic
pattern, reproducibly."* It does **not** mean: *"the platform integrates with, is compatible with, or
can replace product X."* The structural answers are proven against the synthetic graph; they are not
proven against any vendor system.

---

## 6. Disclaimers (explicit)

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

## 7. Cross-references

- Benchmark overview: `docs/Enterprise-Giant-Solution-Reasoning-Benchmark.md`
- Results report: `docs/reports/ENTERPRISE_REASONING_BENCHMARK_RESULTS.md`
- Consultant-role capability: `docs/Enterprise-Consultant-Reasoning-Capability.md`
- Known limitations: `docs/Known-Limitations.md`
- Readiness scorecard: `docs/Enterprise-Readiness-Scorecard.md`, `docs/readiness-scorecard.json`
- Authoritative vision: `MASTER_VISION.md`
