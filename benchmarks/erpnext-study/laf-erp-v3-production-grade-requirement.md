# Requirement — LAF Enterprise ERP V3 (production-grade target)

Same product family as V2 (`benchmarks/erpnext-study/laf-erp-v2-generation-requirement.md`), pushed toward
the production-grade definition (`docs/Production-Grade-ERP-Definition.md`) and gates
(`docs/reports/PRODUCTION_GRADE_ERP_TARGET_GATES.md`).

- **Product:** LAF Enterprise ERP V3 · clean-room · .NET 10 / EF Core / SQL Server (SQLite for portable proof).
- **Engine (templated LAF knowledge):** double-entry GL (+ P&L, Balance Sheet), stock ledger + valuation,
  maker/checker workflow, audit, RBAC, CRM/projects/support/assets, import/export, REST APIs, Razor UI, dev-auth.
- **Data-driven breadth (module spec `tools/LocalAIFactory.Generator/specs/erpnext-grade-modules.json`):**
  Manufacturing (BOM, Work Order, Quality Inspection), HR (Employee, Salary Component), POS (Pos Profile),
  eCommerce (Web Product), Maintenance (Schedule), Customization (Custom Field, Notification Rule) — each
  generated as a CRUD module.
- **Local LLM (governed):** catalog extension entities from the winning model (qwen2.5-coder:14b),
  collision-guarded.
- **Controls + tests:** as in the production-grade gates; expand .NET + Playwright + real-life scenarios.

Honest note: real authentication, TLS, MSSQL production-scale load, and an external security review are
**operator/external-owned** and cap the production-grade score (see the gates report).
