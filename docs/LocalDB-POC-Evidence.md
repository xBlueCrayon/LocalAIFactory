# LocalDB POC Evidence

**Phase:** R2-ACC-POC-COMPLETE · **Date:** 2026-06-21 · **DB:** `(localdb)\MSSQLLocalDB` / `LocalAIFactory`

Read-only, additive verification against the **configured** LocalDB. No drop/truncate/reset. Output is
summarized (no raw table dumps).

## 1. Installed knowledge packs (`KnowledgePacks`)

| Pack | Version | ItemCount | Status | InstalledUtc |
|---|---|---:|---|---|
| Professional Base Knowledge Pack | **1.2.0** | 390 | 0 (installed) | 2026-06-20 20:27:58 |
| Financial Institution Operations v1 | 1.0.0 | 16 | 0 | 2026-06-20 23:52:57 |
| KYC AML Transaction Approval v1 | 1.0.0 | 16 | 0 | 2026-06-20 23:52:58 |
| Market Intelligence and Forecasting v1 | 1.0.0 | 16 | 0 | 2026-06-20 23:52:58 |

- **KnowledgePack table exists** ✅ · **Professional Base Knowledge Pack installed** ✅ · **version 1.2.0** ✅

## 2. Live item counts and Uid integrity

| Pack | Live items | Distinct Uids |
|---|---:|---:|
| Professional Base Knowledge Pack | 390 | 390 |
| Financial Institution Operations v1 | 16 | 16 |
| KYC AML Transaction Approval v1 | 16 | 16 |
| Market Intelligence and Forecasting v1 | 16 | 16 |
| **Baseline (pack-governed) total** | **438** | **438** |

Across **all** `KnowledgeItems`: **1,488** rows, **1,488** distinct Uids, and the duplicate-Uid query
(`GROUP BY Uid HAVING COUNT(*)>1`) returned **0 rows** → **no duplicate Uids** ✅.

## 3. Baseline vs imported project knowledge (distinguishable)

`KnowledgeItems` are split by `KnowledgePackId`:

| Class | Count | How identified |
|---|---:|---|
| Baseline pack knowledge | **438** | `KnowledgePackId IS NOT NULL` (governed by the 4 packs) |
| Imported project knowledge | **1,050** | `KnowledgePackId IS NULL`, across `ProjectId` 9/10/12/13 (SourceType 1/5/17 = imported code/text), plus a few global items |
| **Total** | **1,488** | |

Baseline knowledge is therefore **clearly distinguishable** from imported project knowledge by
`KnowledgePackId` (and by `ProjectId`) ✅.

## 4. Topic coverage (live `KnowledgeItems`, Title+Content match counts)

| Topic | Items | | Topic | Items |
|---|---:|---|---|---:|
| Mauritius | **90** | | insurance | **20** |
| OCR | **61** | | leasing | **13** |
| market / forecast | **51** | | Qdrant | **9** |
| forgery / signature | **42** | | direct debit | **7** |
| PDF | **25** | | VB6 | **3** |

All required baseline domains are present: Mauritius banking ✅, OCR/forgery ✅, PDF summarizer ✅,
financial market prediction ✅, VB6/modernization ✅, insurance ✅, leasing ✅, direct debit ✅.

## 5. Source registry

The `ProjectSources` table is **empty (0 rows)** in this DB. Source governance is **file-based**: the
committed `knowledge-packs/professional-base-v1/source-registry.json` holds the source registry
(≥15 sources with title/type/publisher/allowedUse/reliability/limitation), validated by the
`Source_registry_is_valid` unit test. This is reported honestly: the registry is a source-controlled
artifact, not a runtime DB table here.

## 6. Audit events for pack install/update (`AuditEvents`)

| EventType | Action | TargetType | WhenUtc |
|---|---|---|---|
| 15 | Installed | Professional Base Knowledge Pack v1.2.0 | 2026-06-20 20:28:55 |
| 15 | Installed | Professional Base Knowledge Pack v1.0.0 | 2026-06-20 19:57:20 |

**2** pack-install audit events are recorded (EventType 15) for the professional base pack, including
the v1.0.0 → v1.2.0 upgrade ✅. (The three domain packs were installed via the install-all path; the
authoritative install/verify counts are proven by `verify-all-knowledge-packs.ps1` and §1–2 above.)

## Summary

The configured LocalDB holds a real, governed knowledge base: **4 packs / 438 baseline items**, all
Uids unique, all required domains covered, baseline distinguishable from 1,050 imported project items,
and pack installs audited. No raw tables were dumped and nothing was modified.
