# Market Data Source Governance

> **Status: DESIGN.** Governs how the (optional, not-yet-implemented) Market Intelligence module would
> acquire, score, and trust external market and news data. Companion to
> `docs/Market-Intelligence-Forecast-Module.md`; bound by `docs/Market-Module-Disclaimers.md`.
> **Authority:** subordinate to `MASTER_VISION.md`.

## 1. Why governance comes first

Market data is only as trustworthy as its source and its freshness. Before any signal or forecast is
derived, the module must know **where each datum came from, how reliable that source is, and how stale the
datum is** — and it must respect the legal terms under which the data was obtained. This mirrors the
knowledge-pack source registry: **no fabricated citations, reliability and freshness explicit, nothing
silently trusted.**

## 2. The source registry (`MarketSource`)

Every source is a governed `MarketSource` row (see the module doc's skeleton):

| Field | Meaning |
|---|---|
| `Name` / `Kind` | identity + type (PriceApi / NewsApi / Macro / OnChain / ExpertNote) |
| `Endpoint` | the API/feed, or `manual` for human-expert notes |
| `ReliabilityScore` (0..1) | governed estimate of source reliability (see §4) |
| `FreshnessScore` (0..1) | decays as data ages past the source's expected cadence |
| `TermsRef` | reference to the source's licence/terms and rate-limit policy |
| `Enabled` | a kill-switch per source |

A datum that cannot be attributed to an enabled, in-terms source is **not** ingested as authoritative.

## 3. Acquisition rules (legal + technical)

These are hard constraints, enforced at the connector boundary:

1. **Respect API terms and licensing.** Only acquire data the source's terms permit, for the permitted
   use. `TermsRef` records the basis. Where terms forbid storage/redistribution, the module honours it.
2. **Respect `robots.txt` and rate limits.** Connectors are rate-limited per source; they back off on
   429/503; they do not hammer endpoints.
3. **No fragile scraping as authoritative truth.** Scraped/unofficial data may be used as a **weak,
   clearly-flagged signal** only, never promoted to authoritative without a reliable corroborating
   source. Official APIs and licensed feeds are the authoritative tier.
4. **Attribution preserved.** Every ingested datum keeps its `SourceId`; derived signals/forecasts carry
   provenance back to it.
5. **No PII / no scope creep.** The module ingests market and public-event data only; it does not collect
   personal data.

## 4. Reliability scoring

`ReliabilityScore` is a **governed, transparent** estimate, not a magic number. It is shaped by:

- **Source class** — official exchange/regulator feed > licensed vendor API > reputable news API >
  aggregator > scraped/unofficial.
- **Track record** — consistency with corroborating sources over time (divergence lowers the score).
- **Transparency** — does the source disclose methodology and revisions?
- **Stability** — schema/endpoint stability; frequency of breaking changes.

The score is **interpretable** (a band, like the knowledge `QualityBand`) and **auditable** — its inputs
are recorded so a reviewer can see why a source is trusted to a given degree. It is never used to imply
endorsement of a trading claim.

## 5. Freshness scoring

`FreshnessScore` decays from 1.0 at acquisition toward 0 as the datum ages past the source's expected
cadence (a real-time price stales in seconds; a quarterly macro print stales in months). Stale data is:

- **flagged** in the UI and in any forecast that uses it;
- **down-weighted** in signal derivation;
- **never silently presented as current.**

A forecast built on stale inputs must say so in its caveats (`MarketConfidence.Caveats`).

## 6. News and event governance

- News/events (`MarketNewsEvent`) are stored with source, timestamp, and a `ReliabilityBand`.
- Summaries are **extractive** (no hallucination), like the documentation summariser.
- **Impact detection is a hypothesis.** Linking an event to affected instruments is labelled as a
  hypothesis with confidence, never asserted as a causal fact.
- Single-source, low-reliability news is not promoted to an authoritative signal without corroboration.

## 7. Human-expert ingestion

An expert's market note enters as a **proposal** (`MarketSource.Kind = ExpertNote`), reviewed and
approved by a human — writing a `ProvenanceMethod.Human` event and becoming `Tier = Curated`. Automated
updates then *propose* revisions via `IPermanenceGuard`; they never overwrite the expert's note. This is
the same propose-not-overwrite discipline used for the professional knowledge baseline.

## 8. Conflict and corroboration

- When two sources disagree on a value, both are retained; the higher-reliability source leads, the
  divergence is recorded, and the divergence itself lowers confidence in the affected signal.
- Corroboration across **independent** sources raises confidence; corroboration from sources that share
  an upstream does not (and the registry tracks upstream relationships where known).

## 9. Audit and rebuildability

- Connector runs, source enable/disable, and expert-note approvals are **audited** (`AuditEvent`).
- Raw acquired data is retained immutably so signals/forecasts can be **re-derived** if scoring or methods
  improve — derived artefacts are rebuildable, the raw record is not load-bearing on any one extraction.

## 10. Status

Design only. No connectors, no registry rows, no scoring engine exist. The governance rules here are the
contract any future implementation must satisfy before a single signal is derived. Scorecard: market
module **Low / design**.
