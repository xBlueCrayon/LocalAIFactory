# Cross-Repo Estate Model — Prototype

**Date:** 2026-06-21 · `benchmarks/results/public-systems-estate-map.json`

A **prototype** aggregation across the benchmark repos — **not** a full enterprise estate graph.

## What it aggregates

| Dimension | Value |
|---|---|
| Repos analyzed (with real extraction) | 51-repo run |
| Systems catalogued | 113 |
| Languages | 8 (Python, C#, TypeScript, Java, JavaScript, Go, SQL, Clojure) |
| Domains | 82 distinct |
| Total C# symbols | **123,849** |
| Total Python symbols | **241,730** |
| Total SQL objects | **1,733** |
| Top nodes (by symbols) | 20 (efcore, django, pandas, saleor, PowerShell, superset, airflow, erpnext, …) |

Each node records: id, language, size tier, supported-file count, C#/Python symbols, SQL objects, status.

## What this is — and is NOT

- **Is:** a cross-repo *metric* aggregation — a map of "what languages/domains/symbol volumes exist across the
  attempted estate," built from real extraction results. Useful for portfolio-level sizing.
- **Is NOT:** a cross-repo **dependency/impact graph**. It does **not** link symbols *across* repositories by
  shared database identity (the real "estate model" goal — e.g. BDM↔MCIB↔ETAMS sharing a DB). That requires
  shared-DB identity resolution across imported projects and is **not** built here.

## Path to a real estate model

1. Import multiple real estate repos into the same MSSQL instance.
2. Resolve shared-DB object identity across them (a SQL object referenced by repo A and repo B is one node).
3. Build cross-repo `AccessesSql` edges → an estate-wide impact query.
4. Validate against a known cross-system dependency.

The within-repo C#↔SQL bridge already does (3) inside one repo; extending it across repos by shared-DB identity
is the remaining capability. This prototype is the **metric** layer beneath that.
