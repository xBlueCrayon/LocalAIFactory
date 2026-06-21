# ERP V1 / V2 / V3 / V4 vs ERPNext — Comparison

**Generated:** 2026-06-21
**Source:** `benchmarks/erpnext-study/erp-v1-v2-v3-v4-erpnext-score.json`

> **Honest disclaimer.** None of V1–V4 is an ERPNext clone or ERPNext-compatible. Gains are real but **modest in parity**. The structural wins are the data-driven generator, accounting depth, the knowledge base, and module breadth — not parity.

## Scorecard

| Metric | V1 | V2 | V3 | V4 |
|--------|----|----|----|----|
| ERPNext parity % | 36 | 37 | 42 | **45** |
| Production-grade % | 35 | 35 | 48 | **50** |
| .NET tests | 74 | 82 | 108 | **122** |
| Playwright tests | 12 | 13 | 13 | **13** |
| Generated CRUD modules | 0 | 3 | 15 | **22** |
| Generation autonomy % | 0 | 100 | 100 | **100** |
| Manual product-source edits % | 100 | 0 | 0 | **0** |
| Accounting reports | GL,TB,AR,AP | GL,TB,AR,AP | +P&L,BalanceSheet | +P&L,BalanceSheet |
| How built | hand-written | generated (template-copy) | generated (data-driven spec + LLM) | generated (expanded spec + LLM + knowledge-usage report) |

## V4 vs V3 deltas

- Parity **+3%** → 45%
- Production-grade **+2%** → 50%
- Tests **+14** → 122
- Modules **+7** → 22
- New modules: Quotation, DeliveryNote, PurchaseReceipt, MaterialRequest, StockTransfer, AttendanceRecord, Timesheet
- New capability: a generator **knowledge-usage report** cataloguing the ERP knowledge packs
- Engine depth (P&L / Balance Sheet) carried from V3

## Missing vs ERPNext

Full manufacturing MRP, full HR/payroll, full POS, full website/eCommerce, create/edit UI forms, real auth/TLS, EF migrations.

## Verdict

V4 > V3 by a **modest** margin: +3% parity (45%), +2% production-grade (50%), +14 tests (122), 22 modules vs 15, plus a knowledge-usage report proving the generator catalogues the ERP knowledge packs. **Still PILOT-grade.** ERPNext remains far larger; the remaining distance is documented, not faked.
