# Expected Capabilities — Honest Today vs Future

> **Inspired-by, not a clone.** This scenario is inspired by the inventory / procurement /
> order-to-cash problem space. It is original and fictional. It makes no claim of compatibility,
> equivalence, or certification with any commercial ERP product. The lists below are an *honest*
> account of what LocalAIFactory can reason about today versus what remains aspirational.

This file separates what the platform can **reason about and design today** from what is **future
work**. The goal is to avoid overclaiming: LocalAIFactory is a reasoning and software-engineering
assistant, not a shipped ERP.

---

## What the platform can do today

- **Comprehend the scenario.** Parse the entities, roles, and workflows above and explain the
  order-to-cash flow back coherently.
- **Reason about the data model.** Propose an EF Core schema (items, warehouses, movements,
  POs/GRNs, SOs/shipments, cycle counts, audit) with sensible types (`decimal` for money/quantity)
  and relationships.
- **Defend the "stock as a ledger" design.** Explain why on-hand should be a projection of
  append-only movements rather than a directly editable field, and why that makes audit and
  rollback safe.
- **Design authorization.** Map roles to deny-by-default policies and articulate separation-of-duties
  for approvals.
- **Identify failure modes.** Surface oversell-under-concurrency, double receipt, lost-movement, and
  privilege-creep risks, and propose transactional / idempotency / concurrency-token mitigations.
- **Apply LocalAIFactory engineering discipline.** Recommend lightweight projection rows for lists,
  avoid `GroupBy`-on-constant aggregates, keep external services off the request path, and keep
  migrations additive.
- **Produce tests and acceptance criteria.** Generate the unit/integration/concurrency/authorization
  test outline and a measurable acceptance checklist.

---

## What is future work (not claimed today)

- **No shipped ERP module.** The platform does not currently *implement and run* a complete
  inventory/procurement system end-to-end; it reasons about and can scaffold one.
- **No live financial posting.** General-ledger integration, tax engines, and statutory reporting
  are out of scope and not claimed.
- **No guaranteed lot/serial/expiry tracking** in the base scenario — batch traceability is a future
  extension.
- **No multi-currency or landed-cost engine** yet; valuation here is single-currency on a documented
  basis.
- **No warehouse hardware integration** (barcode scanners, conveyors, WMS robotics).
- **No production-grade performance certification** at high transaction volumes; targets are design
  intents, validated by tests, not benchmarked guarantees.

---

## The honest line

> **Today:** LocalAIFactory can *understand, design, justify, and test-plan* an
> inventory/procurement/order-to-cash solution inspired by this domain, applying its local-first,
> MSSQL-only, audit-first engineering rules.
>
> **Not yet:** It does not ship, run, or certify a complete ERP, and makes no compatibility or
> equivalence claim with any vendor product. Everything beyond reasoning and scaffolding is future
> work, stated plainly so expectations stay calibrated.
