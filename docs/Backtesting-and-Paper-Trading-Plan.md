# Backtesting and Paper-Trading Plan

> **Status: DESIGN.** Validation discipline for the (optional, not-yet-implemented) Market Intelligence
> module. **Paper trading only — no live execution, ever, in this module.** Bound by
> `docs/Market-Module-Disclaimers.md` and `docs/Financial-AI-and-Market-Prediction-Limitations.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. Purpose

A forecast method may only earn confidence through honest validation. This plan defines how the module
would **backtest** methods on historical data and **paper-trade** them forward — without ever executing a
real trade. The output is *evidence about a method's behaviour and its limits*, not a trading
recommendation.

## 2. Non-negotiables

1. **No live execution.** `MarketPaperTrade` is simulation only. The module has no broker integration, no
   order routing, no real money path — by design.
2. **A backtest is necessary but not sufficient.** Past performance is not future performance; regime
   change breaks backtested methods. This caveat travels with every result.
3. **No leakage, no overfitting.** Train/test separation is enforced; suspiciously good results are
   treated as a defect to investigate, not a win to publish.
4. **No invented metrics.** Every reported number is computed from the actual run and reproducible.
5. **Not advice.** No backtest or paper-trade result is presented as a buy/sell/hold signal.

## 3. Backtesting method (`MarketBacktestRun`)

- **Out-of-sample / walk-forward.** The method is fitted on a training window and evaluated on a
  subsequent, untouched window; the window walks forward. In-sample-only results are not reported as
  performance.
- **Leakage guards.** No future data, no look-ahead in features, no survivorship-biased universe where
  avoidable; the run records what guards applied.
- **Transaction realism (simulated).** Where a method implies trades, simulate plausible costs/slippage so
  results are not flattered by frictionless assumptions.
- **Metrics with limits.** Report the method's behaviour (e.g. error of the forecast vs realised,
  hit-rate of intervals) with their **limitations** field stating regime-dependence and sample size.
- **Stability over single results.** A method that only works in one window is flagged unstable; stability
  across windows raises (and instability lowers) the confidence the forecast model assigns (see
  `docs/Financial-Forecast-Confidence-Model.md`).

## 4. Paper trading (`MarketPaperTrade`)

Forward, simulated validation of a method's *forecasts*, not a recommendation engine:

- A paper trade records an `EntryPrice`, optional `ExitPrice`, and a `Rationale` — **informational**.
- It is opened/closed against live or delayed prices from a governed source, simulated only.
- Performance is tracked to see whether forecast confidence bands held forward (an interval that is
  breached far more often than its stated probability is evidence the method is over-confident).
- **No position sizing advice, no allocation, no leverage** is produced. Paper trades exist to *test the
  forecast*, not to suggest action.

## 5. What validation feeds back

- **Confidence calibration.** If realised outcomes fall outside stated bands more often than the band's
  probability, the confidence model widens future bands or flags the method as poorly calibrated.
- **Method retirement.** A method that decays out-of-sample is demoted/withdrawn; its history is retained
  (negative knowledge — methods that stopped working are worth remembering).
- **Regime awareness.** Detected regime shifts during a paper-trade window are recorded as caveats on any
  forecast that used the method.

## 6. Governance and rebuildability

- Backtest and paper-trade runs are **audited** and carry **provenance** to their source data and method.
- Raw historical data is retained immutably, so runs can be **re-executed** if methods or data improve —
  results are rebuildable, never load-bearing on a single run.
- Expert critique of a method enters as a human-approved proposal (propose-not-overwrite), not a silent
  override.

## 7. Presentation rules

- Always pair a result with its **limitations** and the **disclaimer** version.
- Never present a backtest curve as expected future return.
- Never present a paper-trade record as a recommendation to trade.
- Make "**past performance ≠ future results**" prominent wherever results are shown.

## 8. Status

Design only. No backtester, no paper-trading simulator, no metrics pipeline exists. This plan is the
contract a future implementation must satisfy: validation is mandatory, honest, reproducible,
simulation-only, and never advice. Scorecard: market module **Low / design**.
