# Expected Capabilities — Meridian CRM Scenario

An honest separation of what the LocalAIFactory platform can demonstrate **today** versus what is a
**future** aspiration. This scenario is *inspired by* the relationship-management category and makes
no claim of compatibility, equivalence, or certification with any commercial product.

## Today (demonstrable now)

- **Reason about the domain model.** Produce the six-entity relational design (Account, Contact,
  Lead, Opportunity, Case, Activity) with relationships, keys, and concurrency fields.
- **Generate ASP.NET Core MVC + EF Core scaffolding** for the entities and controllers, consistent
  with the host project's conventions (lightweight read models, separate count queries, no
  group-by-constant aggregation).
- **Explain security design** — role-based + record-level ownership checks, deny-by-default,
  IDOR guarding, and an append-only audit log — and where each check belongs (service layer).
- **Design the lead-conversion transaction** and articulate idempotency + concurrency handling.
- **Specify reports** (weighted pipeline, win/loss, case aging, account-360) as efficient,
  projection-based queries that respect the "never materialize large text" rule.
- **Author tests** covering authorization matrices, conversion, concurrency, reporting, and audit.
- **Run fully on MSSQL only**, with optional embedding/RAG and notifications degrading gracefully.

## Future (aspirational — not claimed today)

- **Automated dedup/merge** of accounts using fuzzy matching and model-assisted suggestions.
- **Predictive forecasting** — model-driven opportunity scoring beyond static probability.
- **Conversational pipeline queries** answered from approved project knowledge ("show stalled deals").
- **Live two-way email/calendar sync** for automatic activity capture.
- **SLA breach prediction** from historical case-resolution patterns.
- **Cross-engagement knowledge reuse** surfacing similar prior cases via RAG at triage time.

## Explicit Non-Goals

- Not a clone of, nor compatible with, any specific commercial CRM product.
- No claim of certification, equivalence, or migration compatibility with any vendor system.
- No dependency on GPU, internet, Ollama, or a vector store for core functionality.
