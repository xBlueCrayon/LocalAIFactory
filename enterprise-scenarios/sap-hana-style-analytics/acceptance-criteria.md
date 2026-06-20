# Acceptance Criteria — Treasury Liquidity Analytics (HANA-style, inspired-by)

Measurable checklist. Each item is pass/fail and independently verifiable. No in-memory engine
is claimed; "columnstore" means MSSQL's on-disk columnstore index.

## Schema and model

- [ ] An `analytics` schema exists with ≥ 5 dimension tables and ≥ 3 fact tables as named in `scenario.md`.
- [ ] Every fact foreign key resolves to a dimension row; an integrity query returns **0 orphan facts**.
- [ ] `DimCounterparty` conforms BDM numeric IDs and MCIB BICs to a single surrogate key.
- [ ] Counterparty risk rating uses SCD Type 2 (effective-dated history rows).
- [ ] All schema changes are additive; migration applies cleanly on an empty database.

## ETL and lineage

- [ ] Each of the 4 sources lands in a distinct staging table with an `EtlBatch` record.
- [ ] Re-running the same batch yields **identical** fact state (0 duplicate fact rows) — idempotent.
- [ ] A partial-failure batch leaves **0** half-loaded fact rows (atomic per batch).
- [ ] 100% of a 20-row dashboard-figure sample resolves to a source row via `LineageMap`.
- [ ] Malformed money-market rows are quarantined with a reason; the batch reports the gap count.
- [ ] Unmapped counterparty keys land in an "Unmapped" bucket and raise a data-quality alert (not dropped).

## Aggregation and performance

- [ ] At least one heavy roll-up is served by an **indexed (materialized) view**; its result equals
      the equivalent ad-hoc `GROUP BY` on the same data.
- [ ] `FactPosition` (and/or `FactCashflow`) carries a **columnstore index**; the query plan for the
      dashboard aggregate shows a **columnstore / batch-mode scan**.
- [ ] No serving query uses `GroupBy(_ => 1)`; counts use separate `CountAsync` calls.
- [ ] Intraday refresh path completes within **15 minutes** on representative volume.

## Dashboard and reporting

- [ ] Dashboard shows a **"data as of"** timestamp and the current **refresh latency**.
- [ ] Liquidity-by-currency, counterparty top-N, limit-utilization gauge, and cashflow ladder all render.
- [ ] A past snapshot is reproducible from fact tables (not from an emailed file).
- [ ] With a missing source feed, the dashboard shows **last-good data + a stale banner** (never silent partial).

## Security and audit

- [ ] RBAC is deny-by-default; analytics schema is read-only to all roles except the ETL principal.
- [ ] A role without entity access **cannot** retrieve that entity's counterparty rows (server-side filter).
- [ ] Append-only audit captures limit changes, breach acknowledgements, pack exports, and ETL runs.
- [ ] No secrets in committed config; ETL credentials sourced from environment / Data Protection.

## Runtime and deployment

- [ ] Home, Projects, Knowledge, Models, and the new dashboard **all load** on an empty, a seeded,
      and an **MSSQL-only** database.
- [ ] Every core page returns in well under one second; no "started" log line without a "completed".
- [ ] App migrates and seeds on startup with **only SQL Server** present (no GPU/internet/Ollama/Qdrant).
- [ ] Rollback = dropping the analytics schema objects leaves all source systems untouched.
