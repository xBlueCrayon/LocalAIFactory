# Scenario: Real-Time Treasury Liquidity Analytics (HANA-style, inspired-by)

> **Honesty note.** LocalAIFactory is **MSSQL-authoritative**. It does **not** ship an
> in-memory column-store engine, nor does it claim compatibility, equivalence, or
> certification with any commercial in-memory analytics product. This scenario exercises an
> agent's ability to **reason about analytical architecture over MSSQL** — star schemas,
> indexed/materialized views, ETL latency, and MSSQL clustered/nonclustered **columnstore
> index** awareness. The "in-memory, sub-second, single-source-of-truth analytics" framing is
> *domain inspiration only*, not a product claim.

---

## Business Problem

The bank's Treasury desk closes each day blind to its true intraday liquidity position.
Cash, nostro balances, repo positions, and FX exposures live in four operational systems
(BDM core ledger, MCIB correspondent banking, an FX booking engine, and a money-market
desk spreadsheet feed). Reconciliation is a nightly batch; the consolidated liquidity report
lands at **07:30 the next morning**, by which point the position it describes is ~14 hours
stale. During the 2024 rate volatility the desk twice breached an internal intraday buffer
without knowing until the morning report. Treasury wants a **near-real-time** consolidated
view (refresh latency measured in minutes, not the next business day) and the ability to slice
exposure by currency, counterparty, instrument, and legal entity.

## Current-State Process

1. Each source system exports a flat file at end-of-day (BDM at 18:00, MCIB at 19:00, FX at
   20:30, money-market spreadsheet emailed ad hoc).
2. An analyst manually loads the four files into a staging spreadsheet, hand-maps counterparty
   codes (BDM uses numeric IDs, MCIB uses SWIFT BICs), and pivots by currency.
3. Results are pasted into a formatted Excel workbook and emailed as the "Morning Liquidity Pack."
4. Errors are caught by eyeball; there is no lineage, no audit trail, and no historical store —
   yesterday's pack is just last email's attachment.

## Target-State Process

1. Each source feeds an **ETL** pipeline (incremental where the source supports change tracking,
   full-snapshot where it does not) into an MSSQL **staging schema**.
2. A nightly + intraday transform builds conformed **dimension** tables and appends to **fact**
   tables in a star-schema **analytics schema**.
3. Aggregations are served from **indexed (materialized) views** and columnstore-backed fact
   tables so dashboard queries hit pre-aggregated, compressed data rather than the raw ledger.
4. A liquidity dashboard renders position-by-currency, position-by-counterparty, and a
   limit-utilization gauge with a visible **"data as of" timestamp** and **refresh latency**.
5. Every figure is traceable to its source rows via a **lineage** key; breaches raise an alert.

## Users and Roles

- **Treasury Analyst** — reads dashboards, drills to counterparty detail, exports packs.
- **Head of Treasury** — approves intraday limits; sees breach alerts.
- **Risk Officer** — read-only across all entities; reviews lineage and audit.
- **Data Engineer** — owns ETL jobs, schema, refresh schedule; cannot approve limits.
- **Auditor** — read-only on audit log and lineage; no access to live limit changes.

Roles map onto LocalAIFactory's existing RBAC (deny-by-default, server-side project-access
checks). The analytics schema is **read-only** to everyone except the ETL service account.

## Data Entities

**Dimensions**
- `DimDate` (date key, business day flag, fiscal period)
- `DimCurrency` (ISO 4217 code, decimal precision, reporting flag)
- `DimCounterparty` (surrogate key, conformed from BDM numeric ID + MCIB BIC, risk rating, LEI)
- `DimInstrument` (cash / nostro / repo / FX spot / FX forward, tenor bucket)
- `DimLegalEntity` (booking entity, regulatory jurisdiction)

**Facts**
- `FactPosition` (grain: one row per counterparty × instrument × currency × entity × snapshot
  time; measures: notional, base-currency equivalent, accrued, mark-to-market)
- `FactCashflow` (grain: one row per expected cashflow; measures: amount, value date)
- `FactLimitUtilization` (grain: one row per limit × snapshot; measures: limit, used, headroom)

**Lineage / control**
- `EtlBatch` (batch id, source, start/end, rows in/out, status)
- `LineageMap` (fact row → source system + source natural key + batch id)

## Integrations

- **BDM core ledger** — incremental extract via change-tracking columns; numeric counterparty IDs.
- **MCIB correspondent banking** — daily nostro snapshot; SWIFT BIC keys.
- **FX booking engine** — trade-level extract; needs base-currency revaluation at load time.
- **Money-market feed** — semi-structured CSV; requires schema validation and quarantine on bad rows.
- All integrations are **pull-based ETL into MSSQL staging**; no live cross-system query at
  dashboard render time (consistent with the no-blocking-external-call rule).

## Security and Audit Controls

- Deny-by-default RBAC; analytics schema read-only except ETL service principal.
- Counterparty detail masked for roles without entity access (server-side filter, not UI hiding).
- **Append-only audit** of: limit changes, breach acknowledgements, pack exports, ETL batch runs.
- Every fact row carries a lineage key; no figure is shown without a resolvable source.
- No secrets in config; ETL credentials via environment / Data Protection, never committed.

## Reporting Requirements

- **Liquidity-by-currency** stacked bar, base-currency equivalent, "as of" timestamp.
- **Counterparty exposure** top-N table with drill to instrument grain.
- **Limit utilization** gauge (used / limit) with amber/red thresholds and breach history.
- **Cashflow ladder** by value-date bucket (today, T+1, T+2, week, month).
- Historical replay: any past snapshot reproducible from fact tables, not from emailed files.
- Refresh latency target: intraday dashboards **< 15 minutes** behind source events.

## Failure Modes

- **Source feed late or missing** — dashboard must show last-good snapshot with a stale-data
  banner, never silently render partial data as if complete.
- **Counterparty mapping gap** — unmapped source key routes to a "Unmapped" bucket and raises a
  data-quality alert; it must not be dropped.
- **Currency revaluation rate missing** — affected rows quarantined, not zero-filled.
- **ETL batch partial failure** — batch marked failed atomically; no half-loaded fact partition.
- **Columnstore/index rebuild in progress** — queries fall back to the rowstore base table.
- **Spreadsheet feed malformed** — bad rows quarantined with reason; batch still completes for
  the good rows and reports the gap (no silent blind spots).

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. In summary: a star schema exists over
MSSQL; aggregations are served by indexed/materialized views and/or columnstore-backed facts;
every dashboard figure resolves to source lineage; stale/partial data is surfaced not hidden;
refresh latency and "as of" time are displayed; and the whole thing renders with **MSSQL only**.

## Expected Architecture (analytical model over MSSQL)

- **Star schema** in an `analytics` schema: conformed dimensions + additive fact tables; surrogate
  integer keys; slowly-changing dimension Type 2 on counterparty risk rating.
- **ETL**: staging schema per source → conformed transform → fact append. Incremental via source
  change tracking where available; idempotent re-runnable batches keyed by `EtlBatch`.
- **Aggregation layer**:
  - **Indexed (materialized) views** for the heavy currency/counterparty roll-ups, so the
    aggregate is persisted and maintained by the engine rather than recomputed per query.
  - **MSSQL columnstore index awareness**: a **clustered columnstore index** on the large
    `FactPosition`/`FactCashflow` tables for compression and batch-mode scan; **nonclustered
    columnstore** considered where the table also serves point lookups. Honest caveat: this is
    MSSQL's on-disk columnstore (compressed, batch-mode) — **not** an in-memory column store, and
    we make no in-memory claim.
- **Serving**: dashboard queries hit indexed views / columnstore facts only; no `GroupBy(_ => 1)`
  patterns, separate `CountAsync` calls, lightweight projection records for list views (per the
  repo's runtime rules).
- **Lineage**: `LineageMap` joins any fact row back to source system + natural key + batch.

## Expected Tests

- Star-schema integrity: every fact FK resolves to a dimension; no orphan facts.
- Idempotent ETL: re-running a batch produces identical fact state (no duplicate rows).
- Indexed-view correctness: materialized aggregate equals the equivalent ad-hoc `GROUP BY`.
- Columnstore present and used: query plan shows columnstore/batch-mode scan on the fact table.
- Stale-data handling: with a missing feed, dashboard shows last-good + stale banner.
- Lineage resolvability: 100% of sampled dashboard figures trace to source rows.
- Latency: intraday refresh path completes within the 15-minute target on representative volume.
- Security: a role without entity access cannot retrieve that entity's counterparty rows (server-side).

## Expected Deployment Concerns

- Columnstore + indexed views increase write/maintenance cost — schedule rebuilds off intraday peaks.
- Indexed views constrain the underlying query (schemabinding, deterministic) — document the limits.
- Staging volume growth needs a retention/partition strategy.
- All schema changes additive and backward-compatible; stabilization phases are schema-frozen.
- Must migrate/seed cleanly on startup and run with **SQL Server only** (no GPU/internet/Ollama/Qdrant).

## Rollback Considerations

- Schema changes are additive; rollback = drop the new analytics schema objects, leaving sources untouched.
- ETL batches are idempotent and atomic — a failed/withdrawn batch leaves no partial fact partition.
- Indexed views and columnstore indexes are droppable without data loss (definitions are rebuildable).
- Keep the legacy Excel pack process runnable in parallel until the new path is signed off.

## CEO/CTO Summary

Treasury currently steers intraday liquidity using a report that is half a day old, assembled by
hand in a spreadsheet with no audit trail. This scenario reasons about a **single, traceable,
near-real-time liquidity view built on the bank's existing MSSQL estate** — a proper star schema,
ETL with lineage, and MSSQL's own columnstore/indexed-view aggregation to keep dashboards fast —
**without** taking on a separate in-memory analytics appliance. The value is risk reduction
(catch breaches in minutes, not the next morning) and auditability (every number traces to its
source), delivered on infrastructure the bank already owns and operates.
