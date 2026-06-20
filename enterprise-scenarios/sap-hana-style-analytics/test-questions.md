# Test Questions — Treasury Liquidity Analytics (HANA-style, inspired-by)

These probe whether an agent reasons correctly about analytical architecture **over MSSQL**.
A strong answer must stay honest: **no in-memory engine is provided or claimed**; "columnstore"
means MSSQL's on-disk columnstore index; "real-time" means minutes of refresh latency.

### 1. Design the star schema for intraday liquidity.
**Strong answer must contain:** conformed dimensions (date, currency, counterparty, instrument,
legal entity) with surrogate keys; additive fact grain (counterparty × instrument × currency ×
entity × snapshot); separation of measures from keys; why a star beats querying source ledgers directly.

### 2. Where do indexed (materialized) views help, and what are their limits?
**Strong answer must contain:** persisted aggregates maintained by the engine for heavy roll-ups;
schemabinding + determinism constraints; which aggregates qualify (and which don't); the
correctness expectation that the view equals the ad-hoc `GROUP BY`.

### 3. When would you add a columnstore index, and what does it actually give you?
**Strong answer must contain:** large append-mostly fact tables; compression + batch-mode scan;
clustered vs nonclustered columnstore trade-off; the **honest** clarification that this is
on-disk columnstore, **not** an in-memory column store, and no in-memory claim is made.

### 4. The scenario says "real-time." How real-time is realistic here, and why?
**Strong answer must contain:** target is minutes (≤ 15 min refresh), not sub-second; pull-based
ETL into MSSQL not live federation; no streaming/CDC today; honest distinction from an in-memory
appliance.

### 5. How do you conform BDM numeric counterparty IDs with MCIB SWIFT BICs?
**Strong answer must contain:** a mapping/crosswalk into a single `DimCounterparty` surrogate key;
handling of unmapped keys via an "Unmapped" bucket + data-quality alert (not silent drop); LEI as
a cross-source anchor where available.

### 6. A source feed is late at 18:30. What does the dashboard do?
**Strong answer must contain:** show last-good snapshot with a stale-data banner and "as of" time;
never render partial data as complete; raise an alert; resume on next good batch.

### 7. Make the ETL safe to re-run. What guarantees do you need?
**Strong answer must contain:** idempotency keyed by `EtlBatch`; atomic per-batch load (no partial
fact partition); de-duplication so re-runs don't double-count; quarantine + gap reporting for bad rows.

### 8. How does every dashboard number stay auditable?
**Strong answer must contain:** `LineageMap` linking each fact row to source system + natural key +
batch id; append-only audit of exports/limit changes; reproducibility of past snapshots from facts.

### 9. Enforce that a Risk Officer for Entity A cannot see Entity B's counterparties.
**Strong answer must contain:** deny-by-default RBAC; **server-side** entity filter (not UI hiding);
analytics schema read-only except ETL principal; the IDOR-guard mindset.

### 10. Keep the new dashboard from hanging the app.
**Strong answer must contain:** no blocking Qdrant/Ollama calls on the request path; cached health
snapshot; lightweight projection records, not full-entity list loads; separate `CountAsync`;
**never `GroupBy(_ => 1)`**; serve from indexed views / columnstore facts.

### 11. What runs in MSSQL-only mode, and what doesn't?
**Strong answer must contain:** full schema, ETL, indexed views, columnstore, dashboards all work
with SQL Server only; Qdrant/Ollama optional and absent is fine; no feature on the analytics path
depends on an external service to render.

### 12. How do you roll this back safely?
**Strong answer must contain:** additive schema → rollback by dropping analytics objects only;
sources untouched; idempotent/atomic ETL leaves no partial state; keep the legacy Excel pack
runnable in parallel until sign-off.
