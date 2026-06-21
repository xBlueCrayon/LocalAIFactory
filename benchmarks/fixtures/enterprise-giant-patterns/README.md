# Enterprise Giant-Solution Patterns — Reasoning Fixture

A **synthetic, original** benchmark fixture that tests whether LocalAIFactory can reason like an
enterprise solution consultant over **public, high-level enterprise pattern families** — architecture,
financial workflows, approvals, impact analysis, and operational controls.

> **No vendor clone. No certification.** Every schema and service here is original LocalAIFactory
> content modelling only the **generic, widely-taught** pattern of each family. This fixture is **NOT**
> a clone of, and makes **NO** certified-compatibility claim for, SAP, Microsoft Dynamics / Dataverse /
> Power Platform, Salesforce, ServiceNow, Oracle / NetSuite, Temenos, Finastra, Mambu, FIS, Fiserv,
> Jira, Confluence, Power BI, Tableau, GitHub Copilot, or Sourcegraph. No proprietary vendor schema,
> UI, API, or documentation is reproduced. No compliance, regulatory, financial, or fraud guarantee is
> expressed or implied.

## Pattern families covered

| # | Family | Public pattern modelled | Fixture surface |
|---|--------|-------------------------|-----------------|
| 1 | CRM (Dynamics/Dataverse) | customer, contact, account, opportunity, stage history | `enterprise-crm-schema.sql` |
| 2 | ERP (SAP/Business One/S4) | order-to-cash, procure-to-pay, inventory valuation, GL | `enterprise-erp-schema.sql` |
| 3 | CRM (Salesforce) | lead → opportunity → discount approval | `enterprise-crm-schema.sql` |
| 4 | ITSM (ServiceNow) | incident lifecycle, change request, SLA breach | `enterprise-itsm-schema.sql` |
| 5 | Finance/procurement (Oracle/NetSuite) | purchase order approval, goods receipt, GL posting | `enterprise-erp-schema.sql` |
| 6 | Core banking (Temenos/Finastra/Mambu/FIS/Fiserv) | payment instruction, sanctions screening, maker/checker, settlement | `enterprise-core-banking-schema.sql` |
| 7 | Workflow/docs (Jira/Confluence) | approval + review-gate workflow patterns | scenario questions (advisory) |
| 8 | Code-intelligence (Copilot/Sourcegraph) | find symbol, impact, review-before-migrate | the C#↔SQL bridge itself |
| 9 | Reporting (Power BI/Tableau) | report definition, dashboard widgets, daily snapshot | `enterprise-reporting-schema.sql` |
| 10 | Management-company / operating-manager | daily operations evidence, maker/checker proof | `enterprise-reporting-*.sql/.cs` |

## Files

- **Schemas** (`*.sql`): synthetic CRM / ERP / ITSM / core-banking / reporting tables + stored procedures.
- **Services** (`*.cs`): synthetic C# service layer; each method names its SQL objects in a query string so
  the deterministic C#↔SQL bridge links service → table/proc (the same pattern as the other committed fixtures).
- `scenario-questions.json`: the reasoning questions, each tagged **structural** (graph-proven) or
  **advisory** (design/consultant), with required entities, controls, evidence, and unacceptable overclaims.
- `expected-reasoning.json`: the answer key (grounded entities, controls, risks, evidence, limitations).

## How it is scored

Run the reproducible runner from the repo root:

```powershell
pwsh scripts\benchmark\run-enterprise-reasoning-benchmark.ps1
```

It validates the fixture, runs the **real** structural harness (the product's C#↔SQL bridge) scoped to this
fixture, and scores every question:

- **Structural** → graph-proven by the harness Proof-of-Vision; **100** when the POV passes and the answer
  carries evidence + limitations.
- **Advisory** → grounded design reasoning; **90** when every required entity exists in the fixture and the
  answer supplies controls + risks + evidence + limitations. Advisory answers are **not** graph-executed.

The structural portion is also wired into the main benchmark manifest (`benchmarks/benchmarks.json`, code
`ENTGIANT`) and regression-guarded by `benchmarks/golden/ENTGIANT.json`, so it runs in the standard suite too.
