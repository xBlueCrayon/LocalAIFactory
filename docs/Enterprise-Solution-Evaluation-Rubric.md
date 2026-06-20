# Enterprise Solution Evaluation Rubric

Use this rubric to score **any solution design** produced by LocalAIFactory (e.g. answers to the
`enterprise-scenarios/*/test-questions.md`). It keeps the platform honest: a design is only as good as its
evidence, risk disclosure, and operational practicality.

## Scoring scale (per area)

| Score | Meaning |
|---|---|
| 0 | cannot reason about it |
| 25 | generic answer only |
| 50 | useful partial design |
| 75 | strong consultant-grade design |
| 90 | implementation-ready design |
| 100 | tested, evidenced, deployable solution blueprint |

## Score areas (18)

| # | Area | What a strong (75+) answer shows |
|---|---|---|
| 1 | Business process understanding | Correct current/target state, actors, exceptions, edge cases |
| 2 | Data model quality | Sensible entities, keys, relationships, audit fields, normalization |
| 3 | Security and access design | RBAC, least privilege, server-side enforcement, segregation of duties |
| 4 | Audit and controls | Append-only audit, maker/checker/approver, evidence capture |
| 5 | Integration architecture | Clear boundaries, idempotency, file/API contracts, failure handling |
| 6 | Reporting and analytics | Required reports, lineage, parameters, export controls |
| 7 | Deployment plan | Environment config, MSSQL-only path, GPU optionality, packaging |
| 8 | Supportability | Health checks, diagnostics, logging, runbook thinking |
| 9 | Migration strategy | Strangler-fig/phased, parity tests, data migration, cutover |
| 10 | Testing strategy | Unit/integration/acceptance, golden-master where relevant |
| 11 | Rollback strategy | Concrete recovery path, data-safe, reversible steps |
| 12 | Executive summary quality | One-paragraph, decision-ready, value + cost + risk |
| 13 | Technical implementation clarity | Concrete .NET/MSSQL/EF Core shape a senior dev could build |
| 14 | Risk and limitation disclosure | Honest gaps, assumptions, "not advice", model-risk, human review |
| 15 | Commercial / pilot readiness | What a pilot needs; success criteria |
| 16 | Source / evidence quality | Registered sources where applicable; no fabricated citations |
| 17 | Domain fit | Correct domain vocabulary and controls (banking/insurance/etc.) |
| 18 | Operational practicality | Realistic for the team/hardware; no hand-waving |

## Scoring method

- Score each area 0–100; record one line of justification and the evidence relied on.
- **Overall = mean of the 18 areas.** Bands: <50 weak · 50–74 useful · 75–89 strong · 90+ deployable.
- **Hard caps (honesty gates):** if area 14 (risk/limitation disclosure) < 50, cap the overall at 60 — a
  confident design that hides its limits is not enterprise-grade. If the design overclaims compliance,
  vendor-equivalence, OCR accuracy, fraud-proof, or financial advice, cap the overall at 40 and flag it.

## Worked use

For each scenario, run its `test-questions.md`, score the answers with this rubric, and record the result.
A platform that consistently scores 75+ here — with honest area-14 disclosure — is demonstrably operating at
consultant grade for that domain, *as reasoning/design* (delivery is a separate, higher bar).
