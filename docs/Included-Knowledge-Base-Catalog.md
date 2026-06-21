# Included Knowledge Base — Catalog

Generated from the source-controlled knowledge packs under `knowledge-packs/`. MSSQL is the
runtime source of truth; these JSON packs are the seed/import format. The application installs all
packs on startup (idempotent). All content is original professional summaries with explicit
limitation notes — no proprietary/regulatory text is reproduced, and no compliance/financial/fraud
certainty is claimed.

**Packs: 10 · Total items: 648**

Validated by `scripts/knowledge/verify-all-knowledge-packs.ps1` — PASS (10 packs, 648 items,
648 distinct UIDs, no collisions). See `Knowledge-Pack-Validation-Report.md`.

| # | Pack | Items | Review |
|--:|------|------:|--------|
| 1 | Professional Base Knowledge Pack (`professional-base-v1`) | 390 | Approved |
| 2 | Production Issue Fixes Knowledge Pack (`production-issue-fixes-v1`) | 77 | Approved |
| 3 | Enterprise Workflows Knowledge Pack (`enterprise-workflows-v1`) | 40 | Approved |
| 4 | Engineering Leadership and Innovation (`engineering-leadership-and-innovation-v1`) | 25 | Approved |
| 5 | ERP Testing and Scenarios (`erp-testing-and-scenarios-v1`) | 24 | Approved |
| 6 | Production-Grade ERP Controls (`production-grade-erp-controls-v1`) | 22 | Approved |
| 7 | LAF ERP Generation Lessons (`laf-erp-generation-lessons-v1`) | 22 | Approved |
| 8 | Financial Institution Operations v1 (`financial-institution-operations-v1`) | 16 | Approved |
| 9 | KYC AML Transaction Approval v1 (`kyc-aml-transaction-approval-v1`) | 16 | Approved |
| 10 | Market Intelligence and Forecasting v1 (`market-intelligence-forecasting-v1`) | 16 | Approved |
| | **Total** | **648** | |

## New in the ERP V3 self-improvement sprint

Packs 5–7 were added in this sprint:

- **`erp-testing-and-scenarios-v1` (24)** — ERP testing patterns + business scenarios.
- **`production-grade-erp-controls-v1` (22)** — controls expected of a production-grade ERP.
- **`laf-erp-generation-lessons-v1` (22)** — generator failure patterns, LLM hallucination
  patterns, collision guards, module-spec rules, validation gates.

A pre-existing `itemCount` discrepancy in `engineering-leadership-and-innovation-v1` (26 declared
vs 25 actual) was corrected to **25**.

See `reports/ERP_DEFAULT_KNOWLEDGE_BASE_UPGRADE_REPORT.md` and
`reports/LAF_ERP_GENERATION_LESSONS_INSTALLED_REPORT.md`.
