# Market Module Disclaimers

> **Status: GOVERNING CONSTRAINT.** This document binds the entire (optional, not-yet-implemented) Market
> Intelligence module. Every other market doc is subordinate to it. If any market design, code, or UI
> conflicts with this document, **this document wins** — and the conflicting item must be corrected.
> **Authority:** subordinate only to `MASTER_VISION.md` and
> `docs/Financial-AI-and-Market-Prediction-Limitations.md`, which it operationalises.

## 1. The one sentence that governs everything

> **LocalAIFactory's Market Intelligence module provides forecast and risk *intelligence*. It is NOT
> investment, financial, trading, tax, or legal advice. It produces no buy/sell/hold signals, no
> allocations, and no guarantees. Past performance is not a guide to future results.**

This sentence (or an approved equivalent) must be visible on every market surface and recorded as the
`DisclaimerVersion` against every forecast and validation result.

## 2. What the module must NOT do

- **No advice.** No buy / sell / hold, no allocation, no position sizing, no leverage guidance, no "you
  should".
- **No guarantees.** Never present a forecast as certain or imply guaranteed returns or profitability.
- **No backtest-as-future.** Never present backtest or paper-trade performance as expected future return.
- **No live trading.** No broker integration, no order routing, no real-money execution. Paper trading is
  simulation only.
- **No fabricated sources or endorsements.** Never invent a citation; never use a registered source to
  imply it endorses a trading claim.
- **No compliance/financial/legal certainty.** Risk measures (volatility, drawdown, VaR-style) are
  awareness-level only — **not** a regulatory capital calculation, risk certification, or compliance
  attestation.
- **No PII collection.** Market and public-event data only.

## 3. What the module MAY do

- Present **forecasts as intervals/distributions** with explicit uncertainty and caveats.
- Present **scenarios** with probabilities and stated assumptions (a scenario is not a bet).
- Surface **signals** (macro / fundamentals / technical / crypto / sentiment) with confidence and
  provenance, with sentiment explicitly flagged as awareness-level and not assumed predictive.
- Provide **backtesting and paper trading** for validation discipline, with limitations always attached.
- Maintain **watchlists and informational alerts** worded neutrally.
- Ingest **human-expert notes** as governed, human-approved knowledge.

## 4. Data-acquisition obligations (legal)

- **Respect API terms and licensing.** Acquire only what a source's terms permit, for the permitted use;
  honour storage/redistribution restrictions. The basis is recorded in `MarketSource.TermsRef`.
- **Respect `robots.txt` and rate limits.** Connectors are rate-limited and back off; they do not hammer
  endpoints.
- **No fragile scraping as authoritative truth.** Scraped/unofficial data is a weak, clearly-flagged
  signal at most, never promoted to authoritative without a reliable corroborating source.
- **Attribution preserved.** Every datum keeps its source; derived signals/forecasts trace back to it.

## 5. Honesty obligations

- **Uncertainty is mandatory.** No point forecast without an uncertainty characterisation.
- **Staleness is disclosed.** Forecasts built on stale inputs say so in their caveats.
- **Validation is honest.** Out-of-sample / walk-forward only; leakage and overfitting guarded; a
  suspiciously good result is a red flag, not a headline.
- **Limits travel with results.** "Past performance ≠ future results" and regime-change caveats accompany
  every backtest/forecast surface.

## 6. Wording guardrails

- **Banned:** buy, sell, hold, recommended, you should, guaranteed, sure thing, allocate, position size,
  risk-free.
- **Required where relevant:** forecast, scenario, probability, uncertainty, hypothesised, as of, not
  financial advice, past performance ≠ future results.

## 7. Enforcement

- The disclaimer strip is **non-dismissable** on every market surface.
- Each `MarketForecast` pins the `DisclaimerVersion` that applied when it was generated, so the exact
  wording shown is auditable after the fact.
- Administrative market actions (enabling a source, approving an expert note) are **audited**.
- This document is the acceptance gate: a market feature cannot ship until it complies with every clause
  here.

## 8. Status

The module is **design only** — no connectors, signals, forecasts, dashboard, or validation pipeline
exist. These disclaimers are pre-committed so that *if and when* the module is built, it is built honest
from the first line. Scorecard: market module **Low / design**.
