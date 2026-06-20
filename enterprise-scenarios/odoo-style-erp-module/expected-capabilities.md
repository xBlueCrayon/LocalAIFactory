# Expected Capabilities — Honest Today vs. Future

This is a deliberately honest assessment of what an analysis/implementation pass against
this scenario can produce **today** with LocalAIFactory, versus what is **aspirational**.
No equivalence with any commercial ERP is claimed.

## Today (realistically achievable now)

- **Scenario comprehension & design reasoning.** Read the scenario and produce a coherent
  module design: entities, boundaries, roles, and failure modes.
- **Modular ASP.NET Core MVC + MSSQL skeleton.** Scaffold a self-contained module project
  with its own EF Core entities, schema-prefixed tables, and additive migrations.
- **Clean boundary enforcement (by review).** Identify and flag direct cross-module
  foreign-key reach-ins and recommend service-interface contracts instead.
- **Billing logic units.** Implement and unit-test deterministic proration and schedule
  generation in isolation.
- **Role-based authorization stubs.** Wire deny-by-default authorization checks per command.
- **Append-only audit pattern.** Implement an audit-event write on each consequential action.
- **Static analysis of the design.** Answer the test questions and grade strong vs. weak
  answers using the rubric in `test-questions.md`.

## Partially achievable (with caveats)

- **End-to-end integration with a real Accounting module.** Achievable only against a
  *stub/contract* in this suite; a real ERP's posting API is out of scope here.
- **Background scheduling worker.** Pattern can be scaffolded, but production-grade
  scheduling (retries, dead-letter, observability) needs hardening.
- **Concurrency correctness.** Optimistic concurrency can be added, but full proof under
  load requires dedicated stress testing not run here.

## Future (aspirational — not available today)

- **Live multi-module ERP runtime** with real Accounting/Inventory/CRM modules installed
  and interoperating.
- **Automated migration safety proofs** that guarantee non-destructive upgrades.
- **Usage telemetry ingestion** from real IoT meters on physical assets.
- **Revenue-recognition compliance reporting** validated against an accounting standard.
- **Self-service module marketplace install/uninstall** with dependency resolution.

## Explicit Non-Goals / Honesty Notes

- This is **inspired-by** modular open-source ERP design; it is not that product and
  claims no compatibility, certification, or equivalence.
- No vendor manuals, schemas, or text are reproduced.
- "Done" for this scenario means the design and a testable module skeleton, not a
  production-certified financial system.
