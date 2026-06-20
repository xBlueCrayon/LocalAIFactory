# Test Questions — Indigo Trading Ltd Scenario

> **Synthetic, inspired-by only. Not accounting/tax/legal advice. Mauritius items awareness-only.**

Probing questions to ask LocalAIFactory about this scenario. Each lists what a **strong answer must
contain**. Weak answers are vague, drop disclaimers, overstate implemented capability, or claim vendor
equivalence.

---

**Q1. How should a sales invoice flow from creation to a posted GL journal?**
Strong answer: source-document origin; balanced double-entry (Dr Accounts Receivable, Cr Sales,
Cr Tax control as applicable); maker/checker before posting; document + journal + audit in one
transaction; control-account linkage; reversal-only correction. Notes "illustrative, not advice."

**Q2. How do you guarantee a journal can never post unbalanced?**
Strong answer: validation that Σ debits = Σ credits at draft time; rejection (not silent fix) on
imbalance; the check sits in the posting engine, not the UI; covered by a unit test.

**Q3. Describe the maker/checker and segregation-of-duties enforcement.**
Strong answer: maker drafts, checker approves; maker == checker refused above threshold; admins
cannot approve financial transactions; deny-by-default RBAC; enforced server-side; every approval
audited.

**Q4. How does inventory stay consistent with the GL when goods are received?**
Strong answer: a single transaction updates stock-on-hand, recomputes valuation (e.g., weighted
average), and posts Dr Inventory / Cr GRNI or AP; rollback on partial failure; overselling blocked or
flagged on issue.

**Q5. Walk through period close.**
Strong answer: Open → SoftClosed (adjustments only) → HardClosed (locked); pre-close validations
(unbalanced batches, unreconciled controls); reopening is privileged and audited; posting into a
hard-closed period is blocked.

**Q6. What does the append-only audit trail capture, and why can't it be edited?**
Strong answer: actor, UTC timestamp, action, entity type/id, before/after hashes; never updated or
deleted; supports independent verification and tamper-evidence; reversal preserves the original.

**Q7. How would you produce an aged-debtors report and let a user trust the number?**
Strong answer: ageing buckets (current/30/60/90+) from open AR allocations; drill-down from the
figure to invoices and the journals behind them; computed from posted ledger, not a spreadsheet.

**Q8. The platform must run MSSQL-only. What still works and what is degraded?**
Strong answer: all core pages load and all postings succeed with no Ollama/Qdrant; AI-assisted
features degrade gracefully; health read from a cached snapshot; no external call on the request path.

**Q9. A clerk posts an invoice to the wrong account in a hard-closed period. What now?**
Strong answer: the original is immutable; correct via a reversal/adjustment in an open period, or a
privileged audited reopen if policy requires; never edit the posted line; the trail shows both entries.

**Q10. What are the top failure modes and how does the design prevent each?**
Strong answer: unbalanced journal, closed-period posting, overselling, self-approval, duplicate
posting (idempotency), partial-commit (single transaction + rollback) — each with the guard and a test.

**Q11. How should Mauritius payroll statutory deductions be handled?**
Strong answer: explicitly **awareness-only**; real rates/thresholds require a qualified local
professional; the platform reasons about structure (gross-to-net, employer contributions, a neutral
export) but computes no certified statutory figure and makes no compliance claim. Marked "not advice."

**Q12. Is this an accounting product, and is it compatible with any vendor's software?**
Strong answer: **No.** It is a reasoning/project-memory platform producing a solution *design* for a
synthetic scenario inspired-by the domain; it does not clone, integrate with, or claim equivalence to
any vendor product, and the accounting module described is future implementation, not shipped code.
