# Default Knowledge Catalog

**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](../reports/CURRENT_STATUS.md)
**Verified:** 2026-06-21 · Commit `96fbbc4`

LocalAIFactory ships **20 default knowledge packs** totalling **852 items**, all with distinct UIDs
and **no collisions** (`verify-all-knowledge-packs` PASS). Item counts below are read directly from
each pack's `manifest.json` `itemCount`.

## All 20 packs

| # | Pack ID (`knowledge-packs/<id>/`) | Name | Items |
|---|---|---|---:|
| 1 | `professional-base-v1` | Professional Base Knowledge Pack | 390 |
| 2 | `production-issue-fixes-v1` | Production Issue Fixes Knowledge Pack | 77 |
| 3 | `enterprise-workflows-v1` | Enterprise Workflows Knowledge Pack | 40 |
| 4 | `erp-test-scenario-ui-api-report-v2` | ERP Test / Scenario / UI / API / Report Generation Knowledge Pack | 27 |
| 5 | `erp-selling-buying-stock-production-v1` | ERP Selling, Buying and Stock Production Knowledge Pack | 26 |
| 6 | `engineering-leadership-and-innovation-v1` | Engineering Leadership and Innovation Knowledge Pack | 25 |
| 7 | `erp-full-suite-generation-v1` | ERP Full-Suite Generation Knowledge Pack | 24 |
| 8 | `erp-testing-and-scenarios-v1` | ERP Testing and Scenarios Knowledge Pack | 24 |
| 9 | `erp-accounting-production-v1` | ERP Accounting Production Knowledge Pack | 22 |
| 10 | `laf-erp-generation-lessons-v1` | LocalAIFactory ERP Generation Lessons Knowledge Pack | 22 |
| 11 | `production-grade-erp-controls-v1` | Production-Grade ERP Controls Knowledge Pack | 22 |
| 12 | `erp-inventory-manufacturing-v2` | ERP Inventory & Manufacturing Generation Knowledge Pack | 20 |
| 13 | `erp-hr-pos-ecommerce-customization-v1` | ERP HR / POS / eCommerce / Customization Generation Knowledge Pack | 18 |
| 14 | `screenstream-consent-security-v1` | ScreenStream Consent & Security Knowledge Pack | 18 |
| 15 | `screenstream-windows-capture-protocol-v1` | ScreenStream Windows Capture & Protocol Knowledge Pack | 17 |
| 16 | `financial-institution-operations-v1` | Financial Institution Operations v1 | 16 |
| 17 | `kyc-aml-transaction-approval-v1` | KYC AML Transaction Approval v1 | 16 |
| 18 | `market-intelligence-forecasting-v1` | Market Intelligence and Forecasting v1 | 16 |
| 19 | `screenstream-testing-simple-user-v1` | ScreenStream Testing & Simple Use Knowledge Pack | 16 |
| 20 | `screenstream-windows-packaging-network-v1` | ScreenStream Windows Packaging & Network Knowledge Pack | 16 |
| | **Total** | **20 packs** | **852** |

## By theme

**Base / professional (390 items)**
- `professional-base-v1` (390)

**Domain intelligence & leadership (73 items)**
- `engineering-leadership-and-innovation-v1` (25), `financial-institution-operations-v1` (16),
  `kyc-aml-transaction-approval-v1` (16), `market-intelligence-forecasting-v1` (16)

**ERP generation set (322 items — the packs the generator catalogues)**
- `enterprise-workflows-v1` (40), `production-issue-fixes-v1` (77),
  `erp-full-suite-generation-v1` (24), `erp-accounting-production-v1` (22),
  `erp-selling-buying-stock-production-v1` (26), `erp-inventory-manufacturing-v2` (20),
  `erp-hr-pos-ecommerce-customization-v1` (18), `production-grade-erp-controls-v1` (22),
  `laf-erp-generation-lessons-v1` (22), `erp-test-scenario-ui-api-report-v2` (27),
  `erp-testing-and-scenarios-v1` (24)

**ScreenStream generation (67 items)**
- `screenstream-consent-security-v1` (18), `screenstream-windows-capture-protocol-v1` (17),
  `screenstream-windows-packaging-network-v1` (16), `screenstream-testing-simple-user-v1` (16)

Theme totals: 390 + 73 + 322 + 67 = **852**.

## Notes

- Counts are authoritative as of commit `96fbbc4`. If a pack is edited, update its `itemCount` and
  this table together.
- 11 of these packs (the ERP-oriented set, 322 items) are catalogued by the product generator — see
  [`GENERATOR_KNOWLEDGE_USAGE.md`](GENERATOR_KNOWLEDGE_USAGE.md).
