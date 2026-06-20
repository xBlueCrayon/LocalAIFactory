# Phase R2 — Acceleration Mode Plan

Acceleration Mode fast-tracks LocalAIFactory from a strong pilot code-understanding platform toward a
**deployable professional knowledge product**, while holding the enterprise quality bar: every phase ships
with tests, benchmark/live verification, database/UI evidence, documented limitations, no silent blind
spots, no overclaiming, and no destructive shortcuts.

## Operating standard (every phase)

A feature is complete only with: implementation · tests · benchmark or live verification where applicable ·
database evidence where applicable · UI evidence where applicable · gap/limitation disclosure · rollback /
safety story · updated acceptance criteria · no security regression · no benchmark regression. Every major
claim is proven by at least one of: test, benchmark, database, HTTP/API, UI, or live verification.

## Three lanes

- **Lane A — Persistent Online Benchmark Expansion.** Pinned public repos in Smoke / Standard / Extended
  tiers (every source: name, URL, pinned SHA/version, license note, category, stack, expected extraction &
  gap behaviour, proof-of-vision where possible, coverage/gap report, regression baseline, tier). C#/SQL
  repos score real tiers today; Python/WebForms/WPF repos are added as *gap-only* baselines until the Lane
  C extractors exist (they prove graceful unsupported handling, not capability). Strict rules: pin every
  repo, cache locally, never vendor source in, never hide unsupported files, unresolved external refs are
  informational, proof-of-vision regression is failure, missing coverage report is failure.
- **Lane B — Professional Base Knowledge Pack.** Baseline professional knowledge shipped with the app.
  **v1 delivered — see R2-ACC-B1 below and `Professional-Base-Knowledge-Pack-v1.md`.**
- **Lane C — Enterprise Product Hardening.** R2-P1 Python extractor, C#↔SQL bridge, R2-P2 shared-DB
  identity / cross-repository estate model, deployment hardening, backup/restore, supportability dashboard.

## Fast-track sequencing

1. **R2-ACC-B1 — Professional Base Knowledge Pack v1** ✅ (this slice).
2. R2-ACC-B1b — scale/curate baseline content; optional pack-management screen & RAG weighting.
3. Lane A tiering (`tier`/`tags` on `RepoSpec`, `--suite` filter) + more C#/SQL repos.
4. **C#↔SQL bridge** (highest-"wow" banking demo; builds on existing C# + T-SQL extractors).
5. **Python extractor** (unlocks Lane A multi-language tiers).
6. R2-P2 estate model → deployment hardening → backup/restore → supportability dashboard.

Parallelizable: Lane A harness changes, Lane B content authoring, Lane B importer/UI. Migration-requiring:
Lane B (one additive migration, done). Architecture decisions to flag when reached: Python extraction
mechanism under the offline constraint; whether baseline packs may carry project-scoped overlays.

---

# R2-ACC-B1 — Professional Base Knowledge Pack v1 (delivered)

**Status: complete.** Branch `ke-008-code-symbols`. Design and behaviour are documented in
`Professional-Base-Knowledge-Pack-v1.md`; this section records the slice outcome and evidence.

## What shipped

- **DB anchor (additive migration `AddKnowledgePack`):** `KnowledgePack` table + `KnowledgeItem.KnowledgePackId`
  (nullable FK) + `KnowledgeItem.LastReviewedUtc` (nullable). No existing column changed.
- **Portable pack** `knowledge-packs/professional-base-v1/` — `manifest.json` + 11 category files,
  **134 items** with stable GUID uids and full metadata; copied to build output for deployment.
- **Installer** `IKnowledgePackInstaller` (Rag) — validate-then-write, idempotent (per-`Uid`), transactional,
  no silent overwrite (proposes revisions for user-edited items), best-effort chunk + index.
- **Startup auto-install** (idempotent, non-blocking) + **Admin-only HTTP install** (server-side enforced,
  audited via `AuditEventType.KnowledgePackInstalled`).
- **Base Knowledge UI** — installed-packs list, searchable (multi-term AND) + category/scope filtered
  baseline list with origin badges, and a details page (rendered content incl. limitation, metadata,
  provenance). Lightweight row projection (never materializes `Content` in the list).
- **Docs** — this file + `Professional-Base-Knowledge-Pack-v1.md`.

## Category distribution (134 items)

Software Engineering 13 · CRUD/Web→MSSQL 13 · Database/SQL 13 · Python/AI/Data 12 · Security/Access
Control 13 · Governance/PM 13 · ISO-style Controls/Standards 12 · Finance/Accounting/Banking 14 ·
Operations/Sysadmin 11 · Product/Admin Patterns 10 · Reporting/Templates 10.

## Verification evidence

- **Build:** solution builds, 0 errors.
- **Tests:** 129/129 pass (+13 new `KnowledgePackTests` covering manifest/item validation, duplicate-uid
  rejection, confidence range, install-all, idempotent reinstall, baseline-vs-imported distinction, search,
  pack-origin provenance, upgrade-creates-version, **user-edited-not-overwritten**, malformed-pack-fails-safely,
  and **Admin-only install** server-side gate).
- **Benchmark:** PASS — `povFailures=0, regressions=0, coverageFailures=0` (WWI/eShopOnWeb/eShopOnAbp Gold,
  CleanArchitecture Bronze); the pack install does not perturb extraction goldens.
- **Live (LocalDB + real DI):** startup install created 134 items; DB shows 134 baseline items / 134 distinct
  uids / all Curated / 134 pack-origin provenance / 134 v1 versions, cleanly separated from imported-project
  items. A second startup reported `0 created, 134 unchanged, current=True` (idempotent, no duplicates).
  `/BaseKnowledge` returns 200 (~0.09 s, all 134 rows); search returns matches for MVC, MSSQL, RBAC, direct
  debit, Qdrant, report template, backup restore. Admin HTTP install returns 200 and writes an audit event.
- **Self-correction recorded:** the first startup install was *correctly rejected* (101 validation errors,
  zero DB writes) because the initial uids were malformed GUIDs — proving the validate-then-write guarantee.
  The uids were corrected to valid, unique GUIDs and the install then succeeded.

## Acceptance criteria (met)

Installs into MSSQL ✓ · ≥100 items (134) ✓ · categorized ✓ · searchable ✓ · visible in UI ✓ · version
metadata ✓ · limitation notes ✓ · no silent overwrite of curated/user-edited ✓ · tests prove install &
update behaviour ✓ · documented legal/source limitations ✓ · distinguishes baseline from imported ✓ ·
export/import-ready identity (`Uid`/`OriginPackUid`) ✓.

## Remaining limitations / next improvements

See `Professional-Base-Knowledge-Pack-v1.md` → *Limitations & next improvements* (RAG weighting of baseline
knowledge, category as a first-class facet, tag pruning on update, pack export, pack-management/diff screen).
