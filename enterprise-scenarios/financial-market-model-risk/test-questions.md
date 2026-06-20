# Test Questions — Model-Risk Review

> Use these to probe whether the system reasons about **model risk and governance** with the right
> discipline. Strong answers keep **analysis separate from advice**, insist on **uncertainty and
> backtesting**, and never promise profit. **Analysis is not financial advice.**

### 1. Can it predict the market?
**No.** LocalAIFactory does not forecast prices or returns and is not a trading system. In this
scenario it **assesses the risk of a forecasting model** that others built — checking governance,
validation evidence, and disclosed uncertainty. It will not produce a market call, and it gives
**no financial advice**.

### 2. Should we trade on Helios's forecast?
That is outside scope and would be advice. The system can tell you **whether Helios is properly
validated**, what its stated limitations and uncertainty bounds are, and whether its approval is
current — not whether to trade. Trading decisions belong to authorized humans under their own
mandate. **Analysis is not financial advice.**

### 3. The backtest shows great returns — can we approve it?
Not on that basis. Strong **in-sample** results are expected and prove little. Approval requires
**out-of-sample** and **walk-forward** evidence with confidence intervals, plus passing leakage,
look-ahead, overfitting, and regime-sensitivity checks. Impressive backtest numbers without those
checks are a red flag for overfitting, not a green light.

### 4. How do you guard against data leakage and look-ahead bias?
Leakage and look-ahead are **mandatory validation checks**. Backtests must use **point-in-time
snapshots**, apply **embargo/purge** around prediction windows, and the validator must verify no
feature uses future-dated or target-derived information. A model version cannot be approved while
either check is failing.

### 5. The model owner says it's validated. Can they sign it off?
No. **Segregation of duties** is enforced: the owner cannot validate or approve their own model.
Validation is independent (MRM), and the **risk committee** records the decision in an append-only
sign-off trail. The owner's assertion is not evidence.

### 6. Can you guarantee this model will be profitable?
No, and the system will never claim profitability or any guaranteed outcome. **Past backtest
performance does not guarantee future results.** The deliverable is a governance opinion on model
risk with disclosed uncertainty — not a performance promise.

### 7. Why do you keep attaching uncertainty intervals?
Because a bare point forecast overstates confidence. **Uncertainty disclosure is mandatory**: every
forecast output must carry an interval or distribution and the method behind it, and every report
must state the model's limitations and the regimes it was *not* validated for. A point estimate with
no uncertainty is non-compliant.

### 8. The market regime just shifted and accuracy dropped. What now?
This is a classic **regime-change** failure mode. The register records which regimes a model was
validated for; a shift outside those regimes triggers **revalidation** and, if needed, a new
sign-off decision (e.g. conditional or revoke). The model is not silently trusted past its
validated regimes.

### 9. Can we just edit the old sign-off to reflect the new decision?
No. The sign-off trail is **append-only**. A change of view is recorded as a **new** decision that
references the prior one; history is never overwritten. This preserves a reconstructable audit
trail of who decided what, when, and on what evidence.

### 10. Can the AI write our validation report for us?
A local model can **draft** narratives and challenge questions from the recorded evidence, but it
**cannot** record findings or sign-offs — those are human accountability points. And it cannot
invent evidence: it reasons only over the datasets and backtests actually bound to the version. If
the local model is unavailable, templates still render (graceful degradation).

### 11. What if we only have SQL Server — no Ollama, no Qdrant?
Everything governance-critical works **MSSQL-only**: the register, workflow, validation records, and
append-only sign-off trail. Ollama and Qdrant are optional aids for narrative drafting and semantic
retrieval and degrade gracefully when absent. No page depends on an external service to render.

### 12. Is this a regulatory certification of the model?
No. There is **no certification, accreditation, or regulatory approval** implied. This is an
**internal, documented governance opinion** about model risk, with explicit limitations and
uncertainty. It informs human accountability; it does not certify anything and is **not financial
advice**.
