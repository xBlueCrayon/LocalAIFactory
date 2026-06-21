# LAF Building-Block Composition Benchmark — Report

**Stamp:** 2026-06-21
**Tasks:** `benchmarks/software-reasoning/composition-tasks.json`
**Results:** `benchmarks/results/laf-building-block-composition-benchmark.json`

## The "2 + 2" proof

Just as `2 + 2 = 4` is composed from known parts, a feature is composed from known blocks. This
benchmark asks the composer to plan **20 real-life tasks** and checks two things per task:

1. the expected blocks (with dependencies) are composed; and
2. every capability with **no block** is **honestly flagged as a missing brick**.

A task passes only when **both** hold. **No model is required** — composition is deterministic.

## Result

| Metric | Value |
| --- | --- |
| Tasks | 20 |
| Passed | 20 |
| Score | 100% |
| Model required | NO |

## The 20 tasks

Secure login, login lockout, maker/checker, ERP document lifecycle, stock transfer, manufacturing
order, report endpoint, Playwright proof, EF migration, production smoke, Odoo connector,
WooCommerce mapper, cheque OCR, SFTP, ticketing, MCIB, Direct Debit, import/export, web scraper,
CRUD.

## Missing-brick honesty (the point of the benchmark)

Tasks with **no covering block** are correctly flagged as MISSING and their confidence is lowered:

| Task | Composed from | Missing brick |
| --- | --- | --- |
| Odoo inventory connector | stock-movement | odoo-inventory-connector |
| WooCommerce CSV mapper | import-export | woocommerce-csv-mapper |
| Cheque OCR pipeline | — | cheque-ocr-pipeline |
| SFTP file transfer | — | sftp-file-transfer |
| Ticketing asset workflow | maker-checker, audit-event | ticketing-asset-workflow |
| MCIB XML export | import-export | mcib-xml-export |
| Direct Debit mandate | maker-checker, audit-event | direct-debit-mandate |
| Web scraper knowledge proposal | — | web-scraper-knowledge-proposal |

Fully composable tasks (e.g. secure login → secure-login + password-hashing + audit-event +
anti-forgery) carry no missing brick and a high confidence.

## Honest limitations / not met

- "20/20 = 100%" means **every task was planned correctly, including correctly admitting what it
  cannot build** — it does **not** mean LAF can build all 20. Eight tasks have genuinely missing
  bricks.
- The benchmark validates **composition and honesty**, not generated, compiled, or running code.
- Matching is keyword-based; the score reflects the fixed task set, not arbitrary phrasings.
