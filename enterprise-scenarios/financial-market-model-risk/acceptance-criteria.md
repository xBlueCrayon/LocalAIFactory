# Acceptance Criteria — Model-Risk Review (Measurable Checklist)

> All criteria are about **governance and disclosure**, not market accuracy. The system must never
> output a trading recommendation or a profitability guarantee. **Analysis is not financial advice.**

## A. Register & Inventory
- [ ] Every model has an inventory record with owner, business purpose, and risk tier.
- [ ] Each model version binds the **exact** datasets and backtests used to evaluate it.
- [ ] The register can list models by governance status (validated / conditional / overdue),
      never by a profitability ranking.

## B. Workflow & Segregation of Duties
- [ ] State transitions follow draft → submitted → in-validation → decision → live/retired; illegal
      transitions are rejected server-side.
- [ ] The **model owner cannot validate or approve their own model** (enforced, not advisory).
- [ ] The validator cannot record the committee sign-off; the committee cannot edit findings.

## C. Validation Evidence (mandatory before approval)
- [ ] At least one **out-of-sample** backtest is present and bound to the version.
- [ ] At least one **walk-forward / rolling-origin** backtest with multiple folds is present.
- [ ] Explicit pass/fail validation records exist for: **leakage**, **look-ahead bias**,
      **overfitting**, **walk-forward stability**, **regime sensitivity**.
- [ ] Backtest metrics are reported **with confidence intervals**, in-sample vs out-of-sample shown.
- [ ] Point-in-time dataset snapshots are used; no feature references future-dated data.

## D. Uncertainty Disclosure (mandatory on every output)
- [ ] Every forecast output presents an **interval or distribution** and the method used.
- [ ] A bare point estimate with no uncertainty is rejected as non-compliant.
- [ ] Every report lists **stated limitations** and the regimes the model was *not* validated for.
- [ ] Every artifact carries: **"Model-risk analysis, not financial advice. Past backtest
      performance does not guarantee future results."**

## E. Sign-Off & Audit
- [ ] The sign-off trail is **append-only**: no edit or delete of prior decisions/findings.
- [ ] No model is marked **live** without an approved sign-off bound to passing validation evidence.
- [ ] For any version, the system reconstructs: who approved, when, on what evidence, with what
      conditions and stated limitations.
- [ ] Auditors have read-only access to the full history and can export evidence.

## F. Hard Prohibitions (must FAIL if violated)
- [ ] The system never emits a **buy/sell/hold** recommendation or position sizing.
- [ ] The system never claims a model **will be profitable** or guarantees any outcome.
- [ ] The system never marks a model approved on the strength of in-sample results alone.
- [ ] The system never substitutes a local-model narrative for a human-recorded sign-off.

## G. Runtime & Deployment (LocalAIFactory rules)
- [ ] Works MSSQL-only; Ollama/Qdrant optional and degrade gracefully.
- [ ] No external market-feed call on the request path; datasets are snapshots; health is cached.
- [ ] List views project to lightweight rows; large narrative columns are not materialized in lists.
- [ ] Schema changes are additive/backward-compatible; no destructive change without approval.
