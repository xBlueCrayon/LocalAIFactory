# Expected Capabilities — Treasury Liquidity Analytics (HANA-style, inspired-by)

> **No in-memory engine claim.** LocalAIFactory is **MSSQL-authoritative**. It does **not**
> provide, embed, or emulate an in-memory column-store analytics engine, and it makes **no**
> claim of compatibility, equivalence, or certification with any commercial in-memory analytics
> product. Where this document says "columnstore" it means **MSSQL's on-disk, compressed,
> batch-mode columnstore index** — not an in-memory column store. The domain framing
> (sub-second consolidated analytics) is **inspiration**, used here to test architectural
> reasoning, not a statement of what the platform ships.

## What the platform can support **today**

- Modeling a **star schema** (conformed dimensions, additive facts) as ordinary MSSQL tables via
  EF Core entities and additive migrations.
- Building **indexed (materialized) views** and **columnstore indexes** in MSSQL through
  migrations / raw SQL, then querying them from the app.
- **ETL into MSSQL staging** driven by the existing import/ingestion pipeline patterns (pull-based,
  idempotent, per-batch tracking, quarantine on bad rows, gap reporting — no silent blind spots).
- **RBAC and append-only audit** reuse: deny-by-default, server-side project/entity access checks,
  append-only audit of sensitive actions (consistent with the current security model).
- **Lineage tracking** as first-class rows (`EtlBatch`, `LineageMap`) so every figure is traceable.
- Dashboards that obey the runtime rules: cached health snapshot, no blocking external calls on the
  request path, lightweight projection records, separate `CountAsync`, **no `GroupBy(_ => 1)`**.
- Surfacing **"data as of" timestamp**, **refresh latency**, and **stale-data banners** in the UI.
- Reasoning, by a local model when configured, about whether a given aggregate belongs in an
  indexed view vs a columnstore scan, and the trade-offs.

## What is **future / out of scope** for now

- An actual **in-memory column-store engine** or sub-second OLAP over billions of rows — **not
  provided** and not on the near-term roadmap; MSSQL columnstore is the ceiling here.
- **Automatic ETL scheduling/orchestration** at enterprise scale (CDC connectors, streaming) —
  current pipeline is pull/batch; streaming would be future work.
- **Live cross-system federation** at query time — explicitly avoided; we ETL into MSSQL first.
- **Push-based real-time** (event-driven sub-second refresh) — target here is minutes, not seconds.
- Built-in **BI semantic layer / self-service report designer** — out of scope; dashboards are coded.
- Multi-node / distributed query — single MSSQL instance is the assumed deployment.

## Honest gaps and caveats

- "Real-time" in this scenario means **refresh latency in minutes**, not in-memory instantaneity.
- Indexed views carry schemabinding/determinism constraints that limit which aggregates qualify.
- Columnstore maintenance (rebuild, delta-store) adds write cost; intraday writes need tuning.
- Everything must still render in **MSSQL-only mode** with Qdrant and Ollama absent.
- No claim that performance matches a dedicated analytics appliance; the win is consolidation,
  lineage, and auditability on owned infrastructure, not raw in-memory throughput.
