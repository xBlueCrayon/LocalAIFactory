# Expected Capabilities — Oracle PL/SQL Modernization Scenario

> Honest statement of what LocalAIFactory can support for this scenario **today** versus what is
> **future** work. Inspired-by the PL/SQL-to-.NET/MSSQL modernization domain; no vendor product is
> cloned and no compatibility or equivalence is claimed.

## Capability summary

| Capability | Today | Future | Notes |
|---|---|---|---|
| Import legacy source as a project (ZIP ingestion) | Yes | — | Source files ingest as text artifacts. |
| Coverage + gap reporting on import (no silent blind spots) | Yes | — | Unparseable items are reported, not hidden. |
| **MSSQL / T-SQL** symbol & dependency extraction | **Yes** | — | The current code extractor targets T-SQL. |
| **Oracle PL/SQL** parsing (packages, procs, triggers) | **No (gap)** | **Yes** | **Not yet supported — future extractor.** |
| Persistent, approval-gated project knowledge | Yes | — | Curated rules/contracts captured and approved. |
| Role-based access, project scoping, append-only audit | Yes | — | Matches the scenario's security controls. |
| Data-type mapping catalog (PL/SQL → MSSQL) | Partial (manual/assisted) | Automated | Manual capture today; auto-derivation is future. |
| Transaction-semantics cataloging | Partial (manual/assisted) | Automated | Documented by engineers today. |
| Parity-test authoring and tracking | Manual (captured as knowledge) | Tooling-assisted | Tests authored externally, tracked here. |
| Local-first / MSSQL-only operation | Yes | — | No GPU, no internet, no Qdrant/Ollama required. |

## What works today

- **Ingestion and gap-honest reporting.** Legacy source can be imported as a project. The pipeline
  reports what it could and could not process, so there are no silent blind spots.
- **T-SQL extraction.** For the **target-state** MSSQL artifacts, the code extractor identifies
  symbols and dependencies in T-SQL. This is the supported automated extraction path.
- **Curated, approval-gated knowledge.** Proc contracts, data-type mappings, and transaction
  semantics can be captured as knowledge items, reviewed, and approved into persistent memory.
- **Security and audit.** RBAC, deny-by-default project access, and append-only audit align with the
  scenario's controls.
- **Local-first guarantee.** Everything functions with only SQL Server present.

## What is explicitly NOT supported today (gap-only)

- **Oracle PL/SQL parsing is not implemented.** The current extractor does **not** parse Oracle
  PL/SQL packages, procedures, functions, or triggers. There is **no** automated symbol or dependency
  extraction for PL/SQL source at this time.
- Consequently, assessment of the **source** NightLedger logic relies on manual or assisted
  cataloging until a dedicated PL/SQL extractor exists.
- Automated PL/SQL→MSSQL data-type derivation and automated transaction-semantics inference are not
  available today.

## Future work (named, not promised as present)

- A **PL/SQL extractor** producing the same inventory/dependency outputs the T-SQL extractor produces.
- Automated data-type mapping suggestions with precision/nullability flags.
- Assisted transaction-semantics inference (autonomous transactions, implicit commits, isolation).
- Parity-test scaffolding generation from captured contracts.

## Honesty rule for any generated output

Any assessment, report, or answer produced for this scenario must state plainly that **Oracle PL/SQL
parsing is not yet supported** and that automated extraction currently covers **MSSQL / T-SQL only**.
Outputs must not imply Oracle parsing exists, and must not claim compatibility, equivalence, or
certification with any commercial database or tool.
