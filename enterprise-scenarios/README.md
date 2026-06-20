# Enterprise Capability Simulation Suite

This suite tests whether LocalAIFactory can **reason about enterprise-grade solutions** — not just store
knowledge. Each scenario is an **original synthetic** business problem *inspired by* a domain. None of these
clone, reproduce, or claim compatibility/equivalence/certification with any vendor product. "Style" in a
folder name means *inspired-by*, never the product.

## How to use

For each scenario, pose the questions in `test-questions.md` to LocalAIFactory (or to a reviewer using it)
and score the answers with `docs/Enterprise-Solution-Evaluation-Rubric.md`. A strong answer is consultant-grade,
evidence-aware, and honest about limits. The `acceptance-criteria.md` file defines what "good" looks like;
`expected-capabilities.md` states honestly what the platform can do **today** (knowledge/design reasoning)
versus what needs future implementation.

## Each scenario contains

- `scenario.md` — business problem, current/target state, users & roles, data entities, integrations,
  security & audit controls, reporting, failure modes, acceptance criteria, expected architecture, expected
  tests, deployment concerns, rollback considerations, and a CEO/CTO summary.
- `expected-capabilities.md` — today vs future (honest line).
- `acceptance-criteria.md` — measurable review checklist.
- `test-questions.md` — probing questions, each with what a strong answer must contain.

## Scenarios

| Folder | Domain inspiration | Platform fit (honest) |
|---|---|---|
| `sage-style-accounting` | small-business accounting/payroll/inventory | strong knowledge fit; advisory/design |
| `sap-business-one-style-inventory` | ERP inventory / order-to-cash | strong fit; advisory/design |
| `sap-hana-style-analytics` | in-memory analytics / DW | reasoning over MSSQL analytics; **no in-memory engine claim** |
| `oracle-plsql-modernization` | Oracle PL/SQL → .NET/MSSQL | T-SQL supported; **Oracle PL/SQL parsing is gap-only** |
| `cisco-network-change-impact` | network change-impact governance | dependency/impact *reasoning* only; **does not manage devices** |
| `microsoft-dynamics-style-crm` | CRM / pipeline / cases | strong fit; advisory/design |
| `odoo-style-erp-module` | modular ERP module design | strong fit; advisory/design |
| `banking-reconciliation-platform` | direct-debit settlement reconciliation | **core strength**; deep knowledge + design |
| `insurance-claims-workflow` | claims lifecycle | strong fit; awareness-only on regulation |
| `leasing-arrears-platform` | leasing/arrears/collections | strong fit; ECL = accounting-interpretation caveat |
| `vb6-to-aspnet-migration` | VB6 → ASP.NET Core | migration *playbook*; **no VB6 parser (gap-only)** |
| `cheque-ocr-forgery-workflow` | cheque OCR + forgery-risk + human review | knowledge/design; **no shipped OCR engine; never fraud-proof** |
| `pdf-summarizer-with-provenance` | PDF intelligence with provenance | knowledge/design; **no shipped engine yet** |
| `financial-market-model-risk` | forecasting model-risk governance | governance reasoning; **not prediction, not financial advice** |

## Honesty rules (apply to every scenario)

- No vendor cloning, no verbatim manuals, no compatibility/equivalence/certification claims.
- Domain caveats stand: not legal/regulatory/tax/audit/financial advice; Mauritius items are awareness-only;
  OCR/forgery is probabilistic with mandatory human review and no accuracy claim; forecasting is analysis,
  not advice.
- Where a capability is not implemented, the scenario says so plainly (gap-only / design-only).
