# docs/reports — Reading Guide

## Start here

**[`CURRENT_STATUS.md`](CURRENT_STATUS.md) is the single authoritative current status.** Where any
other report's numbers differ, `CURRENT_STATUS.md` wins.

## What this directory is

`docs/reports/` is the program's **audit trail**: ~170 point-in-time evidence reports accumulated
across the near-GA, ERP V1–V5, ScreenStream, knowledge-engine, deployment, and load-testing sprints,
plus a small set of **living** documents that are kept current.

- **Living (kept current):**
  - [`CURRENT_STATUS.md`](CURRENT_STATUS.md) — authoritative current status.
  - [`HISTORICAL_REPORT_INDEX.md`](HISTORICAL_REPORT_INDEX.md) — index of the point-in-time reports.
  - Cleanup/validation docs from this sprint: [`REPOSITORY_STRUCTURE_DECISION.md`](REPOSITORY_STRUCTURE_DECISION.md),
    [`LOCAL_AND_REPO_CLEANUP_REPORT.md`](LOCAL_AND_REPO_CLEANUP_REPORT.md),
    [`GIT_REPOSITORY_CLEANLINESS_REPORT.md`](GIT_REPOSITORY_CLEANLINESS_REPORT.md),
    [`MARKDOWN_AUDIT_REPORT.md`](MARKDOWN_AUDIT_REPORT.md),
    [`POST_CLEANUP_VALIDATION_REPORT.md`](POST_CLEANUP_VALIDATION_REPORT.md),
    [`KNOWLEDGE_ENGINE_READY_REPORT.md`](KNOWLEDGE_ENGINE_READY_REPORT.md).
- **Point-in-time evidence (superseded):** everything else. These captured the state at the moment
  they were written. They are **not** updated; treat their scores as historical and defer to
  `CURRENT_STATUS.md`.

## How to use it

1. For the current position, read [`CURRENT_STATUS.md`](CURRENT_STATUS.md).
2. To find historical evidence on a theme (ERP version, ScreenStream, near-GA gate, deployment/load),
   use [`HISTORICAL_REPORT_INDEX.md`](HISTORICAL_REPORT_INDEX.md).
3. Do not cite a historical report's score as the current state — cross-check against
   `CURRENT_STATUS.md` first.

## Why not delete the old reports?

They are the verifiable record of how the program reached its current state (the V1→V5 progression,
the near-GA gate runs, the load/deployment drills). They are small markdown text and cost nothing to
keep, and removing them would erase the evidence trail. They are **indexed and labelled historical**,
not deleted.
