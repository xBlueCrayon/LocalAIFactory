# Knowledge-Pack Validation Report — LocalAIFactory

This report records the **validation results for the included knowledge base** and how to
**reproduce them** with a single committed script. Validation is offline-first: the source-controlled
JSON packs are the authoritative seed format, and MSSQL is the runtime source of truth. No content
here makes any compliance, regulatory, financial, or fraud-detection claim.

The validating script is `scripts/knowledge/verify-all-knowledge-packs.ps1`. It is **read-only** and
exits non-zero if any pack is invalid, so it can gate a release.

---

## 1. What is validated

For every pack under `knowledge-packs/`, the script checks (offline, no database required):

- `manifest.json` is present and valid JSON; `packUid` is a valid GUID.
- Every file referenced by the manifest exists and is valid JSON.
- Every item `uid` is a valid GUID.
- No duplicate UIDs **within** a pack.
- No duplicate UIDs **across** packs (cross-pack collision check).
- Every item carries a **limitation** note and **tags**.
- The manifest's declared `itemCount` matches the actual item count.

If a database is reachable (and `sqlcmd` is present), it also reports the **live installed counts**
(`KnowledgePacks` rows and pack-origin `KnowledgeItems`) so the source packs and the seeded database
can be reconciled. The live check is optional; the offline validation remains authoritative.

---

## 2. Validation results (10 packs / 648 items)

| Pack (folder) | Items | UIDs valid GUID | Within-pack dups | Limitation + tags |
|---|---:|---|---|---|
| Professional Base Knowledge Pack (`professional-base-v1`) | 390 | yes | none | every item |
| Production Issue Fixes Knowledge Pack (`production-issue-fixes-v1`) | 77 | yes | none | every item |
| Enterprise Workflows Knowledge Pack (`enterprise-workflows-v1`) | 40 | yes | none | every item |
| Engineering Leadership and Innovation (`engineering-leadership-and-innovation-v1`) | 25 | yes | none | every item |
| ERP Testing and Scenarios (`erp-testing-and-scenarios-v1`) | 24 | yes | none | every item |
| Production-Grade ERP Controls (`production-grade-erp-controls-v1`) | 22 | yes | none | every item |
| LAF ERP Generation Lessons (`laf-erp-generation-lessons-v1`) | 22 | yes | none | every item |
| Financial Institution Operations v1 (`financial-institution-operations-v1`) | 16 | yes | none | every item |
| KYC AML Transaction Approval v1 (`kyc-aml-transaction-approval-v1`) | 16 | yes | none | every item |
| Market Intelligence and Forecasting v1 (`market-intelligence-forecasting-v1`) | 16 | yes | none | every item |
| **Total** | **648** | **648 distinct, all valid GUIDs** | **0** | **all 648** |

Cross-pack check: **648 distinct UIDs for 648 items → no within-pack and no cross-pack collisions.**
Each item's `manifest.itemCount` matches its actual item count. (A pre-existing `itemCount`
discrepancy in `engineering-leadership-and-innovation-v1` — 26 declared vs 25 actual — was corrected
to 25 as part of this sprint.)

**Result line printed by the script:** `VERIFY-ALL-KNOWLEDGE-PACKS: PASS`

### New packs in this sprint

Three packs were added in the ERP V3 self-improvement sprint and validate cleanly:
`erp-testing-and-scenarios-v1` (24), `production-grade-erp-controls-v1` (22), and
`laf-erp-generation-lessons-v1` (22). See `docs/Included-Knowledge-Base-Catalog.md` for the full
per-pack catalog.

---

## 3. Live-database verification (LocalDB)

On the build host, with the database seeded, the live checks corroborate the offline results:

- `scripts/knowledge/verify-all-knowledge-packs.ps1` live block: **4 installed packs / 438 pack items**.
- `database/verify-knowledge-base.ps1`: **VERIFIED** — 438 baseline items, all curated (Tier 1),
  438 provenance events, 17 `src:` tags, and the baseline is distinct from the 1035 imported-project
  items (`KnowledgePackId IS NULL AND ProjectId IS NOT NULL`).
- `database/verify-full-install.ps1`: **PASS** — 14 EF migrations applied, knowledge base verified,
  all source packs validated and matched to the live installed counts.

These confirm the seeded MSSQL state matches the validated source packs.

---

## 4. How to reproduce

### Offline validation (no database needed)

```powershell
# From the repository root. Pass -Server "" to skip the optional live check.
pwsh scripts/knowledge/verify-all-knowledge-packs.ps1
```

Expected tail:

```
Packs: 4 | items: 438 | distinct UIDs: 438
  [ OK ] no cross-pack UID collisions
VERIFY-ALL-KNOWLEDGE-PACKS: PASS
```

### Offline + live counts (database reachable)

```powershell
pwsh scripts/knowledge/verify-all-knowledge-packs.ps1 `
  -Server "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

Adds:

```
== Live installed counts ((localdb)\MSSQLLocalDB / LocalAIFactory) ==
  [ OK ] DB has 4 installed pack(s), 438 pack item(s)
```

### Full-install reconciliation

```powershell
pwsh database/verify-full-install.ps1
pwsh database/verify-knowledge-base.ps1
```

Expected: `VERIFY-FULL-INSTALL: PASS` and `KNOWLEDGE-BASE: VERIFIED`.

---

## 5. Scope and honesty notes

- These are **structural and integrity** checks (valid GUID UIDs, no collisions, manifest/count
  agreement, presence of a limitation note and tags). They do **not** assess the factual accuracy of
  any item's content; items are original professional summaries with explicit limitations, not
  authoritative regulatory text.
- The packs reproduce **no** proprietary or regulatory wording and claim **no** compliance,
  financial, or fraud-detection certainty.
- The live check is best-effort: if `sqlcmd` or the database is absent, the script reports that and
  the offline result stands on its own.

---

## See also

- `docs/Knowledge-Pack-Install-Runbook.md` — how packs install and how to regenerate the catalog.
- `docs/Knowledge-Pack-Authoring-Guide.md` — pack format and authoring rules.
- `docs/Included-Knowledge-Base-Catalog.md` — generated per-category catalog.
