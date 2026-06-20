# Market Intelligence / Forecast Module

> **Status: DESIGN + proposed entity skeleton. NOT implemented.** No live market-data connectors exist.
> This is the optional "financial weather forecast" module — **forecast and risk intelligence, never
> investment advice.** Read `docs/Market-Module-Disclaimers.md` first; it governs everything here.
> **Authority:** subordinate to `MASTER_VISION.md` and to
> `docs/Financial-AI-and-Market-Prediction-Limitations.md`.

## 1. Purpose and posture

The module is an **optional** capability that ingests governed market data and verified news, derives
signals (macro / fundamentals / technical / crypto), characterises **uncertainty and risk**, and presents
**scenario forecasts with confidence bands** — like a weather forecast: "conditions and probabilities",
not "do this trade". It is built to the same principles as the rest of the platform: MSSQL is the source
of truth, optional services degrade gracefully, every output is traceable, and **nothing is presented as
certain**.

It does **not**: give buy/sell/hold or allocation advice; imply guaranteed returns; treat a backtest as
future performance; or use a registered source to imply endorsement of a trading claim.

## 2. Architecture overview

```
governed sources (registry: reliability + freshness)
      │  connectors (rate-limited, terms-respecting)
      ▼
raw market data + verified news/events  (retained, immutable)
      │  signal derivation (macro / fundamentals / technical / crypto / sentiment)
      ▼
MarketSignal + MarketNewsEvent  (each with provenance + confidence)
      │  scenario engine
      ▼
MarketForecast (+ MarketScenario, MarketConfidence bands)
      │  validation: MarketBacktestRun + MarketPaperTrade (paper only)
      ▼
dashboard (charts, watchlists, alerts) — disclaimers always visible
```

All derived artefacts are rebuildable projections; the authoritative record (sources, raw data, signals,
forecasts, validation runs) lives in MSSQL. With connectors disabled the module shows only what it has
already governed — it never blocks a core page.

## 3. Capability areas (all design)

- **Market-data connectors** — pluggable, rate-limited adapters per source; respect API terms, robots,
  and licensing (see `docs/Market-Data-Source-Governance.md`). No fragile scraping as authoritative truth.
- **Verified web/news ingestion** — news/events captured with their source, timestamp, and reliability
  band; impact detection links an event to affected instruments as a **hypothesis**, not a fact.
- **Source registry + scoring** — every source carries a reliability and freshness score; stale or
  low-reliability data is flagged, never silently trusted.
- **Signals** — macro (rates, inflation prints), fundamentals (reported figures), technical
  (price/volume-derived), crypto (on-chain/market structure), each with confidence and provenance.
- **Sentiment / risk scoring** — sentiment is **awareness-level**, explicitly not assumed predictive;
  risk measures (volatility, drawdown, VaR concepts) are awareness-level, **not** regulatory capital.
- **Confidence bands** — forecasts are intervals/distributions, never bare point numbers (see
  `docs/Financial-Forecast-Confidence-Model.md`).
- **Scenario forecast** — multiple scenarios with probabilities; a scenario is **not a bet**.
- **Backtesting + paper trading** — validation discipline; paper trading only, no live execution (see
  `docs/Backtesting-and-Paper-Trading-Plan.md`).
- **Watchlists / alerts** — user-defined instruments and threshold alerts (informational).
- **Human-expert ingestion** — an expert's market note is captured as governed knowledge (a proposal,
  approved by a human) — the same propose-not-overwrite discipline as everywhere else.

## 4. Proposed entity skeleton

Additive design (a real migration would be additive only). **Proposed**, not present.

```csharp
class MarketSource {
    int Id; Guid Uid;
    string Name; string Kind;        // "PriceApi" | "NewsApi" | "Macro" | "OnChain" | "ExpertNote"
    string Endpoint;                 // or "manual" for expert notes
    double ReliabilityScore;         // 0..1, governed (see governance doc)
    double FreshnessScore;           // 0..1, decays with staleness
    string TermsRef;                 // licence/terms reference; rate-limit policy
    bool Enabled;
}

class MarketInstrument {
    int Id; Guid Uid;
    string Symbol; string AssetClass; // "Equity" | "FX" | "Crypto" | "Index" | "Rate"
    string DisplayName; string? Venue;
}

class MarketSignal {
    int Id; Guid Uid; int InstrumentId;
    string Kind;                     // "Macro" | "Fundamental" | "Technical" | "Crypto" | "Sentiment"
    DateTime AsOfUtc;
    double Value; string Unit;
    double Confidence;               // conservative
    int SourceId;                    // provenance
}

class MarketNewsEvent {
    int Id; Guid Uid;
    DateTime PublishedUtc; int SourceId;
    string Headline; string Summary; // extractive, no hallucination
    string? AffectedSymbols;         // hypothesised impact, flagged as hypothesis
    double ReliabilityBand;
}

class MarketForecast {
    int Id; Guid Uid; int InstrumentId;
    DateTime GeneratedUtc; DateTime HorizonUtc;
    string Method;                   // named, reproducible
    int ConfidenceId;                // band, never a bare point
    string DisclaimerVersion;        // which disclaimer text applied
}

class MarketScenario {
    int Id; Guid Uid; int ForecastId;
    string Name;                     // "Base" | "Upside" | "Downside" | ...
    double Probability;              // scenarios sum sensibly; a scenario is not a bet
    string Narrative; string Assumptions;
}

class MarketConfidence {
    int Id; int ForecastId;
    double Lower; double Upper;      // interval
    string Distribution;             // characterisation
    string Caveats;                  // regime-change / data-quality caveats
}

class MarketBacktestRun {
    int Id; Guid Uid; int InstrumentId;
    DateTime FromUtc; DateTime ToUtc;
    string Method; string Metrics;   // out-of-sample; leakage/overfitting guarded
    string Limitations;              // "past performance ≠ future results"
}

class MarketPaperTrade {
    int Id; Guid Uid; int InstrumentId;
    DateTime OpenedUtc; DateTime? ClosedUtc;
    double EntryPrice; double? ExitPrice;
    string Rationale;                // informational; PAPER only — no live execution
}
```

## 5. Governance hooks (reuse, not reinvent)

- **Provenance:** market knowledge promoted to durable memory carries a `ProvenanceEvent` (source,
  method, actor). Expert notes approved by a human write a `ProvenanceMethod.Human` event.
- **Permanence:** human-curated market notes are `Tier = Curated`; automated signal updates propose
  revisions via `IPermanenceGuard`, never overwrite.
- **Audit:** connector runs and approvals are audited.
- **Source registry:** mirrors the knowledge-pack `source-registry.json` discipline — no fabricated
  citations; reliability/freshness explicit (`docs/Market-Data-Source-Governance.md`).

## 6. Honest status

Everything in this document is **design + a proposed skeleton**. No connector, signal engine, forecast
engine, backtester, or dashboard is implemented. The scorecard records the market module as **Low /
design**, with the exact proof to advance: the skeleton entities plus **one** governed connector and a
backtest run, captured — with disclaimers enforced and nothing presented as advice.
