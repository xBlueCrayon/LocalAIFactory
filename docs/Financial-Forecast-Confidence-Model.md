# Financial Forecast Confidence Model

> **Status: DESIGN.** How the (optional, not-yet-implemented) Market Intelligence module would express
> uncertainty. Companion to `docs/Market-Intelligence-Forecast-Module.md`; bound by
> `docs/Market-Module-Disclaimers.md` and `docs/Financial-AI-and-Market-Prediction-Limitations.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. The core commitment

> **No point forecast without an uncertainty characterisation.** Every forecast is an interval or a
> distribution with explicit caveats — never a bare number, and never "certain". A confidence band is a
> statement of *uncertainty*, not a promise of accuracy, and **not** advice.

This implements the limitations note verbatim: forecasting ≠ prediction ≠ scenario analysis; probabilistic
forecasts preferred; backtest performance is not future performance; regime change breaks models.

## 2. What a confidence band is (and is not)

A `MarketConfidence` (see the module skeleton) carries:

- `Lower` / `Upper` — the interval bounds for the horizon.
- `Distribution` — a characterisation (e.g. the shape/spread used), not just two numbers.
- `Caveats` — the conditions under which the band is invalid (regime change, stale inputs, thin data).

It is **not**:

- a guarantee the outcome falls in the band;
- a confidence "score" implying skill;
- a basis for a buy/sell/hold decision.

## 3. Inputs to confidence

Confidence is a **transparent function of observable factors**, never model self-assurance:

| Factor | Effect on band width |
|---|---|
| **Source reliability** (governance doc) | low reliability → wider band |
| **Data freshness** | stale inputs → wider band + caveat |
| **Horizon length** | longer horizon → wider band (uncertainty compounds) |
| **Historical volatility** of the instrument | higher volatility → wider band |
| **Corroboration** across independent sources/signals | more corroboration → narrower band |
| **Backtest stability** (out-of-sample) | unstable history → wider band |
| **Regime indicators** | regime shift detected → widen + flag, or withhold the forecast |

The mapping from factors to band width is recorded so a reviewer can see *why* a forecast is as confident
(or uncertain) as it is — interpretable, like the knowledge `QualityBand`.

## 4. Scenario forecasts

Where a single distribution is misleading, the module presents **scenarios** (`MarketScenario`): Base /
Upside / Downside (and others), each with a **probability** and an explicit **assumptions** narrative.

- A scenario is **not a bet** and not a recommendation.
- Probabilities are honest estimates with their own uncertainty; they are not implied to be precise.
- The assumptions are stated so a reader can judge whether they still hold.

## 5. Validation discipline (gates confidence)

A forecast method earns confidence only through validation (see
`docs/Backtesting-and-Paper-Trading-Plan.md`):

- **Out-of-sample / walk-forward testing** — no in-sample-only claims.
- **No train/test leakage; no overfitting** — guarded explicitly; a suspiciously good backtest is treated
  as a red flag, not a triumph.
- **A backtest is necessary but not sufficient** — regime change breaks it; this caveat travels with every
  forecast that relies on a backtested method.

A method with no validation produces forecasts at the **lowest** confidence and a prominent "unvalidated"
caveat — or is withheld.

## 6. Risk measures are awareness-level

Volatility, drawdown, and VaR-style measures may be **surfaced for awareness**, with the explicit
limitation that they are **not** a regulatory capital calculation and **not** a risk certification. They
inform the band's width; they do not turn a forecast into advice.

## 7. Sentiment is not assumed predictive

Sentiment and alternative-data signals contribute only as **weak, clearly-flagged** inputs with stated
data-quality risks. They never narrow a band on their own and never drive a recommendation (there are no
recommendations).

## 8. Presentation rules

- Always show the **interval/distribution and caveats** alongside any central tendency.
- Always show the **disclaimer** (`MarketForecast.DisclaimerVersion` pins which text applied).
- Never render a forecast as a single confident number.
- Never render language that could read as buy/sell/hold or position sizing.

## 9. Status

Design only. No forecast or confidence engine exists. This model is the contract any future
implementation must satisfy: uncertainty is mandatory, transparent, validated, and never dressed up as
advice or certainty. Scorecard: market module **Low / design**.
