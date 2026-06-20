# Market Dashboard UX Specification

> **Status: DESIGN.** UX for the (optional, not-yet-implemented) Market Intelligence module. Every surface
> here must keep the disclaimer visible and must never read as advice. Bound by
> `docs/Market-Module-Disclaimers.md`.
> **Authority:** subordinate to `MASTER_VISION.md`. UI stack matches the rest of the app: Bootstrap 5 +
> bootstrap-icons, server-rendered MVC, client-side markdown via `marked.js`; charts client-side over
> server-provided data.

## 1. Design principles

1. **Forecast, not advice.** No buy/sell/hold buttons, no "recommended action", no position sizing. The
   tone is a weather forecast: conditions, probabilities, caveats.
2. **Uncertainty is always visible.** No forecast is shown as a bare number; intervals/scenarios and
   caveats are first-class, not buried.
3. **Provenance one click away.** Every datum links to its `MarketSource` with reliability + freshness.
4. **Degrades gracefully.** With connectors disabled the dashboard shows last-governed state and a clear
   "data not live" banner — it never blocks or hangs (the platform's core rule: pages must always load
   quickly).
5. **Disclaimer persistent.** A non-dismissable disclaimer strip is present on every market surface; the
   pinned `DisclaimerVersion` is recorded against any forecast shown.

## 2. Surfaces

### 2.1 Market overview

- A grid of watched instruments with current value, freshness indicator, and a small trend sparkline.
- Stale data is visibly flagged (greyed + "as of …").
- No ranking that implies "best to buy" — sort is by user choice (symbol, asset class, freshness), never
  by an implied recommendation.

### 2.2 Instrument detail

- Price/value chart with the **confidence band** overlaid (shaded interval), never a single forecast line
  alone.
- **Scenario panel**: Base / Upside / Downside cards, each with probability, assumptions, and narrative.
- **Signal panel**: macro / fundamental / technical / crypto / sentiment signals, each with its
  confidence and source link; sentiment explicitly labelled "awareness-level, not predictive".
- **Caveats block**: regime-change / staleness / thin-data caveats from `MarketConfidence.Caveats`.

### 2.3 News & events

- Chronological feed of `MarketNewsEvent`s with source, timestamp, reliability band, and an **extractive**
  summary (no hallucinated text).
- Any event→instrument impact is labelled **"hypothesised impact"**, never asserted as causal.

### 2.4 Watchlists

- User-defined lists of instruments. Informational only.
- Add/remove is a personal organisation tool, not a portfolio.

### 2.5 Alerts

- Threshold alerts (e.g. "value crosses X", "freshness lapses", "band breached").
- Alerts are **informational notifications**, worded neutrally ("X crossed your threshold"), never
  "consider selling".

### 2.6 Validation view

- Read-only display of `MarketBacktestRun` and `MarketPaperTrade` results with their **limitations** and a
  prominent "past performance ≠ future results" banner.
- No control that turns a validation result into an action.

### 2.7 Expert notes

- Human-expert market notes (approved, curated) displayed with author and approval provenance.
- Editing is a propose-not-overwrite flow (an automated update proposes; the human's note stands until
  approved), consistent with the rest of the platform.

## 3. Chart conventions

- Forecasts: shaded confidence band + central tendency; never a lone line implying certainty.
- Scenarios: distinct, labelled paths with probabilities; clearly marked "scenario, not a bet".
- Staleness: explicit "as of" timestamps; stale series visually de-emphasised.
- Colour is not used to imply "good/bad to trade" — it encodes data category and freshness only.

## 4. Copy rules (wording guardrails)

- Banned phrasing: "buy", "sell", "hold", "recommended", "you should", "guaranteed", "sure thing",
  "allocate", "position size".
- Required phrasing where relevant: "forecast", "scenario", "probability", "uncertainty", "as of",
  "hypothesised", "not financial advice", "past performance ≠ future results".
- Every forecast/validation surface shows the disclaimer strip and the pinned disclaimer version.

## 5. Access and audit

- The market module follows the platform's RBAC posture; market surfaces are visible to authenticated
  users per role, and any administrative action (enabling a source, approving an expert note) is audited.
- Project/tenant isolation rules apply if market data is scoped to a customer.

## 6. Status

Design only. No market dashboard exists. This spec is the contract a future UI must satisfy: uncertainty
visible, provenance reachable, disclaimers persistent, and **no surface that reads as advice**. Scorecard:
market module **Low / design**.
