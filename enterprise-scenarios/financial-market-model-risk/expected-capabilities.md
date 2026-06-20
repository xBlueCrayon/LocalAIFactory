# Expected Capabilities — Honest Scope

> **Read this first.** This scenario tests **model-risk governance reasoning**, not market
> prediction. LocalAIFactory is **not** a forecasting engine, **not** a trading system, and
> **does not give financial advice.** Every capability below is about *assessing and governing*
> models — never about calling the market.

## What it CAN do today

- **Maintain a model-risk register** over MSSQL: inventory, versions, datasets, backtests,
  validation results, and an append-only sign-off trail.
- **Enforce a governance workflow**: state transitions (draft → submitted → in-validation →
  decision → live/retired) with server-side guards and **segregation of duties** (owner ≠
  validator ≠ approver).
- **Reason over approved governance knowledge**: inject curated validation standards and prior
  findings as first-class context, and help a validator draft challenge questions and checklists.
- **Demand validation evidence**: refuse to treat a model as approved/live without bound datasets,
  out-of-sample + walk-forward backtests, and a completed validation checklist.
- **Surface uncertainty**: require interval/distribution disclosure and stated limitations on every
  forecast output and report.
- **Flag classic failure modes** in reasoning and checklists: overfitting, leakage, look-ahead
  bias, regime change, survivorship/selection bias, staleness.
- **Reconstruct the decision trail**: who approved what, when, on what evidence, with what caveats.

## What it CANNOT do (and will not pretend to)

- **It cannot predict the market.** It is not a price/return forecaster and does not generate
  forecasts. It evaluates the *risk* of models that others build.
- **It does not give trading or investment advice.** No buy/sell/hold, no allocation, no signal.
- **It cannot guarantee profitability or any outcome.** It explicitly rejects profitability claims;
  past backtest performance does not guarantee future results.
- **It cannot replace independent human validation or committee accountability.** A local model may
  draft narratives; humans record findings and sign-offs.
- **It cannot certify a model.** There is no certification, accreditation, or regulatory approval
  implied — only an internal, documented governance opinion.
- **It cannot validate against data it was not given.** It judges only the snapshotted datasets and
  backtests actually bound to a version.

## Discipline this scenario enforces

- **Analysis ≠ advice.** Every output is framed as model-risk analysis with a standing
  not-financial-advice disclaimer.
- **Uncertainty is mandatory.** A bare point estimate is non-compliant; intervals/distributions and
  stated limitations are required.
- **Backtesting is mandatory.** No approval without out-of-sample and walk-forward evidence, with
  leakage and look-ahead checks.
- **Honesty about limits.** When the system lacks evidence, it says so and withholds approval rather
  than inferring confidence.
