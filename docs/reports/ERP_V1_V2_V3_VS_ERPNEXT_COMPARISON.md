# ERP V1 vs V2 vs V3 vs ERPNext — Comparison

**Date:** 2026-06-21
**Score file:** `benchmarks/erpnext-study/erp-v1-v2-v3-erpnext-score.json`

**Disclaimer:** None of V1/V2/V3 is an ERPNext clone or ERPNext-compatible. V3's gains over V2 are real but **modest in parity**; the structural win is the data-driven generator + accounting depth + knowledge base.

## Comparison table

| Dimension | V1 | V2 | V3 |
|-----------|----|----|----|
| ERPNext parity | 36% | 37% | **42%** |
| Production-grade | 35% | 35% | **48%** |
| .NET tests | 74 | 82 | **108** |
| Playwright tests | 12 | 13 | 13 |
| Generated CRUD modules | 0 | 3 | **15** |
| Accounting reports | GL, TB, AR, AP | GL, TB, AR, AP | GL, TB, AR, AP, **P&L, Balance Sheet** |
| How built | hand-written | generated (template-copy) | generated (data-driven module spec + LLM) |
| Generation autonomy | 0% | 100% | 100% |
| Manual product-source edits | 100% | 0% | 0% |
| Local LLM | no | qwen (catalog) | qwen, **winner of qwen-vs-deepseek eval** (catalog) |

## V3 deltas vs V2

- **+5% ERPNext parity** (37% → 42%).
- **+13% production-grade** (35% → 48%).
- **+26 .NET tests** (82 → 108).
- **15 data-driven modules vs 3** template-copy modules.
- **A real P&L and Balance Sheet** (balanced).

## Still missing (vs ERPNext)

Full manufacturing MRP; full HR/payroll; full POS; full website/eCommerce; quotation/delivery/receipt/returns; create/edit UI forms; real auth/TLS; EF migrations.

## Honest verdict

V3 > V2: +5% parity (42%), +13% production-grade (48%), +26 tests (108), 15 data-driven modules vs 3, and a real P&L/Balance Sheet. **Still PILOT-grade.** The generator became genuinely data-driven (module-spec JSON), and the local LLM was selected by a real qwen-vs-deepseek eval. **ERPNext remains far larger;** the remaining distance is documented, not faked.
