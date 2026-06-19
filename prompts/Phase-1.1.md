# Phase 1.1 — Usability & robustness (brief)

**Goal:** make the baseline genuinely usable day-to-day without changing the schema.

**Scope delivered:**
- **Bulk curation UI** on Knowledge: select-all, multi-select, bulk approve/deprecate/delete
  (`bulk-actions.js`, wired in the layout).
- Improved chat message metadata (model names, PromptRun links).
- Ingestion **job recovery** on startup (requeue interrupted jobs) and import status breakdowns.
- Modern card/table styling across pages.

**Constraints:** additive only; no schema changes; keep the solution buildable and deployable.
