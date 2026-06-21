# ERP-GOLD-DEPTH — Local Model (Ollama) Review

**Sprint:** ERP-GOLD-DEPTH · **Branch:** `ke-008-code-symbols` · **Stamp:** 2026-06-21
**Companion data:** `benchmarks/results/erp-gold-depth-ollama-review.json`

## Setup

- Ollama present.
- Reviewers: `qwen2.5-coder:14b` (reviewer) and `deepseek-r1:14b` (architect).
- **Authority:** local models **review only**. All committed code is human-authored deterministic
  templates and is tested. No model output was committed unreviewed.

## Findings and dispositions

| By | Finding | Disposition |
|----|---------|-------------|
| qwen | Manufacturing lacks traceability / predictive maintenance / IoT | BACKLOG (out of local scope) |
| qwen | Reports lack BI / ML analytics / customizable dashboards | ALREADY DOCUMENTED (known parity gap) |
| qwen | GL lacks budgeting / financial-planning tools | BACKLOG (future parity item) |
| deepseek | Incorrect moving-average costing with multiple batches / partial orders | PARTIALLY COVERED -> BACKLOG |
| deepseek | Production status not transitioning correctly to Completed | ALREADY TESTED |
| deepseek | Transfer fails to decrement the source warehouse | ALREADY TESTED |

### Notes on the architect's correctness concerns

- **Status transitions** — `ManufacturingTests` assert the full
  `Draft -> MaterialsIssued -> QualityPassed -> Completed` transition plus completion immutability.
- **Transfer decrement** — `Scenario_inventory_transfer_conserves_total_qty` asserts source
  decrement + destination increment = conserved total.
- **Moving-average across receipts** — `Scenario_inventory_issue_uses_moving_average` covers two
  receipts; the **partial-production-run costing edge** was added to the backlog.

## Outcome

No accepted finding contradicts the honest scoring. Two **new backlog items** were recorded:
partial-run costing edge, and budgeting/BI. The architect's correctness concerns (status
transitions, transfer decrement) are already covered by tests.

## Honest limitations / not done

The local-model review is **advisory**. Its accepted findings (partial-run costing edge,
budgeting/BI) are **backlog, not implemented** this sprint. The review does not constitute an
external audit and does not raise the parity or production scores.
