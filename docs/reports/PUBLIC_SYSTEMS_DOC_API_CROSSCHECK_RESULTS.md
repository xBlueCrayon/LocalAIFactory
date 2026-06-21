# Public Systems — Docs / API Cross-Check Results

**Date:** 2026-06-21 · `scripts/benchmark/fetch-public-system-docs.ps1`

What was **actually fetched and verified** against official docs (a respectful sample — not aggressive
scraping of all 113 systems).

## Sampled official-docs fetch (live HTTP)

| System | Official docs URL | HTTP | Bytes | Expected topics found |
|---|---|---:|---:|---:|
| odoo | odoo.com/documentation | 200 | 417,826 | 1/8 |
| wordpress | developer.wordpress.org | 200 | 159,569 | 1/7 |
| erpnext | docs.frappe.io/erpnext | 200 | 2,282,875 | 2/8 |
| airflow | airflow.apache.org/docs | 200 | 67,089 | 2/6 |
| grafana | grafana.com/docs | 200 | 391,342 | 2/6 |
| keycloak | keycloak.org/documentation | 200 | 8,230 | 0/7 |
| superset | superset.apache.org/docs | 200 | 313 | 0/7 |
| drupal | drupal.org/docs | 200 | 3,036 | 0/7 |

**5 of 8** systems had expected topics present in the fetched official-doc HTML (odoo, wordpress, erpnext,
airflow, grafana) — these questions are credited as **doc-grounded**. The other 3 returned small
JS-rendered landing pages (topics not in the static HTML) — honestly **not** credited as content-verified.

## Honest scope

- This is a **sampled** cross-check (8 systems). The full 113-system docs registry
  (`benchmarks/public-systems-docs-registry.json`) records official docs/API URLs as **metadata**; their
  **content was not deep-read**, so the understanding benchmark scores those at the metadata level only.
- Fetched HTML is cached under the **git-ignored** `.tmp-public-system-docs/` and **not** committed.
- We do **not** claim API/doc understanding beyond what was fetched and topic-verified. Extending the
  cross-check to more systems (politely, respecting rate limits) is the path to raise the doc-grounded count.
