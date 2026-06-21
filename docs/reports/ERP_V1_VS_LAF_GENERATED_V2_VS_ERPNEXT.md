# ERP V1 (hand-built) vs V2 (LAF-generated) vs ERPNext

**Date:** 2026-06-21 · **Scores:** `benchmarks/erpnext-study/erp-v1-v2-erpnext-score.json`

| Dimension | ERPNext | V1 (hand-built) | V2 (LAF-generated) |
|---|---|---|---|
| Modules touched | 15+ | 9 | 9 (+3 generated catalog) |
| DB entities | hundreds | 35 | 38 |
| Maker/checker workflows | configurable | 4 doctypes | 4 doctypes (same engine) |
| REST APIs | auto per doctype | ~17 | ~23 (+6 catalog) |
| UI pages | full desk | 9 | 10 (+Catalog) |
| .NET tests | large | 74 | **82** |
| Playwright | large | 12 | **13** + login + screenshots |
| Real-life scenario test | — | implicit | **explicit, passing** |
| Accounting / stock / RBAC / audit controls | full | real + tested | real + tested (templated) |
| **How it was built** | product team | **hand-written by operator** | **emitted by a generator (100% file autonomy)** |
| Local LLM used | — | no | **yes (governed catalog layer)** |

## Scores (honest)

| Metric | Value |
|---|---:|
| ERPNext parity V1 | 36% |
| ERPNext parity V2 | 37% |
| V2 improvement vs V1 | **+1%** (parity essentially flat) |
| **LAF generation autonomy** | **100%** |
| Manual intervention (product source edits) | **0%** |
| Generator/template fixes | 3 |
| Test confidence V1 / V2 | 75% / 76% |
| Production readiness V1 / V2 | 35% / 35% |

## Did V2 improve over V1?

**On parity: marginally** (+1%, same engine + 3 generated catalog modules). **On autonomy: dramatically** —
V1 was hand-coded; V2 was *generated* by a LocalAIFactory tool with **zero hand edits to product source**,
plus a governed local-LLM that contributed real (validated, test-covered) catalog modules, with a collision
guard that rejected 3 hallucinated entities.

This is the honest outcome the sprint was testing: the **purpose of LocalAIFactory** — generating the app,
not having a human write it — is demonstrated. Parity did not jump, and **neither version is
production-grade** (both ~35%). No fake autonomy, no fake parity.
