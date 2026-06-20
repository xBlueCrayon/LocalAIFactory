# Financial AI & Market-Prediction — Limitations and Guardrails

This note governs the **Financial Market Prediction and Quantitative Analysis** knowledge category and any
LocalAIFactory feature that touches forecasting, quantitative analysis, or financial decision support.

## Hard rule: analysis is not advice

LocalAIFactory may help with **analysis, model risk awareness, and validation discipline**. It does **not**
provide financial, investment, or trading advice, and it must never present a forecast as a guaranteed
outcome or claim trading profitability.

Every financial-market knowledge item must:

- separate **analysis** from **recommendation**;
- state **uncertainty** explicitly (no point forecast without an uncertainty characterization);
- require **backtesting / walk-forward validation** before any model output is trusted;
- include a **risk warning** and a **"not financial advice"** limitation where relevant.

## Model-risk principles encoded

- **Forecasting vs prediction vs scenario analysis** — different questions; scenario analysis is not a bet.
- **Validation discipline** — out-of-sample testing, walk-forward validation, and avoidance of **train/test
  leakage** and **overfitting**. A backtest is necessary but not sufficient (regime change breaks it).
- **Risk measures** — VaR / stress-testing concepts are **awareness-level** (see source `src-basel`), not a
  regulatory capital calculation.
- **Probabilistic forecasts** — prefer intervals/distributions over single numbers; disclose the interval.
- **Sentiment & alternative data** — limitations and data-quality risks are stated, not assumed predictive.
- **Governance & explainability** — model governance, documentation, and explainability are required for any
  model that informs a decision.

## What the platform must NOT do

- Present any forecast as certain, or imply guaranteed returns / profitability.
- Give buy/sell/hold or allocation advice, or position sizing.
- Treat backtest performance as future performance.
- Use registered sources to imply a specific peer-reviewed endorsement of a trading claim.

## Attribution

Concepts are attributed to registered source families (`src-research-ml`, `src-research-business`,
`src-basel`) — **topic families, not specific papers**; no DOIs/titles are asserted (see the source
registry). Basel/VaR/stress material is principle-level awareness, not regulatory advice.

> Bottom line: LocalAIFactory can make a team **more disciplined** about forecasting (validation, uncertainty,
> governance). It cannot and must not act as a financial adviser.
