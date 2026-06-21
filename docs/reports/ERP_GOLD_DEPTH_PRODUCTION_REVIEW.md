# ERP-GOLD-DEPTH — Phase 14: Production Review

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-production-grade-score.json`,
`benchmarks/results/erp-gold-erpnext-parity-score.json`, `benchmarks/results/erp-gold-depth-score.json`

## Headline scores

| Measure | Before | After |
|---------|:-----:|:-----:|
| Production-grade mean (honest local self-assessment, 0–100) | 78 | **80.6** |
| ERPNext parity (clean-room self-assessment, 0–100) | 39 | **45** |
| .NET (xUnit) tests | 222 | **255** |
| Playwright tests | 38 | **51** |
| End-to-end scenarios | 13 | **26** |

## Targets vs outcome

| Target | Result |
|--------|--------|
| ERPNext parity 55% | **NOT MET** (45%) |
| Production score >= 78% | MET (80.6) |
| .NET tests 300+ | **NOT MET** (255) |
| Playwright 50+ | MET (51) |
| Scenarios 25+ | MET (26) |
| Reproduction >= 90% | MET (92.2% test, 100% Playwright, 100% deterministic surface) |

## Builds

0 errors across the generator, Gold, GoldGenerated-Depth, and the main `LocalAIFactory.sln`. The
main app passes 240/240 tests and its knowledge packs PASS.

## Classification

**ERP_LOCAL_PRODUCTION_READY** + measurable ERPNext-grade **depth**.

The product is **APPROACHING but NOT yet `ERP_NEXT_GRADE_DEPTH_READY_LOCAL`**.

### Exactly why it is not yet ERP_NEXT_GRADE_DEPTH_READY_LOCAL

1. **HR/payroll, POS and e-commerce remain CRUD skeletons** (parity 22 / 20 / 18) — no payroll
   engine, no POS sale flow, no cart/checkout.
2. **No return/delivery document chains** — DeliveryNote, CreditNote and DebitNote are CRUD
   skeletons that do not relieve or reverse stock/GL; no partial delivery/receipt.
3. **ERPNext parity is 45%, below the 55% bar.**
4. **.NET tests are 255, below the 300+ bar.**
5. Additional depth gaps: no multi-level BOM / routing / WIP / labour+overhead costing; no
   batch/serial/landed-cost stock; reporting is query-based with no BI / print-designer and aging
   by posting date rather than due-date terms.

## What genuinely improved

Real BOM-driven manufacturing with moving-average production costing and quality gating; a broad,
GL-reconciled report set with a REST API; 26 end-to-end scenarios; and a **live** SQL Server
(LocalDB) migration + app-login proof. All depth is reproduced by the deterministic generator
(92.2% test / 100% deterministic surface).

## Honest limitations / not done

No 100% claim. No ERPNext parity claim. No external-certification claim. The full
`ERP_NEXT_GRADE_DEPTH_READY_LOCAL` bar (55% parity, 300+ tests, all module flows) is **not** reached
in one sprint. External gates (SSO/OIDC, CA TLS, external security review, customer acceptance) and
the depth gaps listed above remain open.
