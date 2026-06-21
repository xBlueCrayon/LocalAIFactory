# LAF Reasoning V2 — Real Case Studies

**Stamp:** 2026-06-21
**Data:** `benchmarks/results/laf-reasoning-v2-real-case-studies.json`

## Purpose

Show the V2 composer on **real cases** drawn from the actual ERP Gold product, the ScreenStream
product, and LocalAIFactory itself. For each case the engine composes the **present** blocks (with
dependencies) and **honestly flags genuinely-missing bricks** — that honesty is the deliverable.
Each case composes deterministically (no model).

## Case studies

| Case | Composed blocks | Missing bricks | Tests | Key risk |
| --- | --- | --- | --- | --- |
| ERP Gold authentication hardening | secure-login, password-hashing, login-lockout, anti-forgery, audit-event | — | AuthTests + AuthHardeningTests + login.spec | session/cookie security |
| ERP Gold manufacturing depth | manufacturing-order, stock-movement, audit-event | — | ManufacturingTests | costing edge cases |
| ERP Gold report depth | report-endpoint, accounting-posting | — | ReportsDepthTests | GL reconciliation |
| ScreenStream server/client generator | production-smoke | screenstream-capture | n/a (separate product) | consent + LAN security |
| LocalAIFactory reasoning engine itself | xunit-service-test | codegraph-reasoning | Reasoning + CodeBlocks suites | syntax-only graph |
| Cheque OCR pipeline | import-export | cheque-ocr-pipeline | to-be-built | OCR accuracy |
| Odoo inventory connector | stock-movement | odoo-inventory-connector | to-be-built | external API auth |
| WooCommerce CSV mapper | import-export | woocommerce-csv-mapper | to-be-built | schema mapping |
| Direct Debit mandate approval | maker-checker, audit-event | direct-debit-mandate | to-be-built | mandate compliance |
| Ticketing asset workflow | maker-checker, crud-module | ticketing-asset-workflow | to-be-built | SLA + audit |

## What the cases demonstrate

For each case the engine composes the present blocks (with their dependencies) and flags the
genuinely-missing bricks (odoo, woocommerce, cheque-ocr, direct-debit, ticketing, screenstream)
rather than pretending they exist. **LAF knows what it can compose and what it still lacks.**

## Honest limitations / not met

- Several cases have **real missing bricks** and tests marked **"to-be-built"** (cheque-ocr, odoo,
  woocommerce, direct-debit, ticketing). These are **not** implemented; they are correctly reported
  as gaps.
- The case studies validate **composition + honest gap-reporting**, not generated or running code.
- ScreenStream and the reasoning engine itself reference blocks/capabilities
  (`screenstream-capture`, `codegraph-reasoning`) that are **not in the 16-block catalogue** and are
  flagged accordingly.
