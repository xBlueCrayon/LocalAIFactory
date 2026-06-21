# ERP Production Knowledge-Base Upgrade

**Date:** 2026-06-21

## What was added (honest)

This sprint added **2 new production-grade ERP knowledge packs** (a third was attempted but the drafting
agent stalled mid-write and its incomplete folder was removed to keep `verify-all-knowledge-packs` green):

| Pack | Focus |
|---|---|
| `erp-accounting-production-v1` | chart of accounts, JE, payment, GL, trial balance, P&L, balance sheet, AR/AP, cost centers, fiscal close, opening balances, taxes — as generation rules |
| `erp-selling-buying-stock-production-v1` | lead→quotation→SO→delivery→SI→payment→credit-note; supplier→PO→receipt→PI→payment→debit-note; item/warehouse/stock-ledger/receipt/issue/transfer/adjustment/valuation — as generation rules + controls |

These install by default (they live in `knowledge-packs/`, which the app + `verify-all-knowledge-packs`
enumerate) and are catalogued by the generator's knowledge-usage report.

## Verification

`verify-all-knowledge-packs.ps1` → **PASS: 20 packs, 852 items, 852 distinct UIDs, no collisions.**
The 240-test guard stays green.

## Honest note

The original plan listed more production packs (UI/API/workflow, deployment/security). Those topics are
already partly covered by existing packs (`erp-ui-api-report-generation`, `erp-deployment` content in
`production-grade-erp-controls-v1`, `laf-erp-generation-lessons-v1`); the two dedicated packs were not
completed this sprint and are recorded as a backlog item rather than claimed.
