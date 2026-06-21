# Repository Structure Decision

**Date:** 2026-06-21 · **Commit:** `96fbbc4` · **Branch:** `ke-008-code-symbols`
**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](CURRENT_STATUS.md)

## Decision

**Keep the existing physical paths unchanged.** Mark current-versus-historical status through
README index files (already in place), **not** by moving or renaming the historical generation
artifacts (ERP V1–V4).

## Context

`generated-products/` contains six trees:

| Tree | Path | Role |
|---|---|---|
| LAF Enterprise ERP (V1) | `generated-products/LAF-EnterpriseERP/` | historical (hand-built reference) |
| LAF Enterprise ERP V2 | `generated-products/LAF-EnterpriseERP-LAFGenerated/` | historical |
| LAF Enterprise ERP V3 | `generated-products/LAF-EnterpriseERP-V3/` | historical |
| LAF Enterprise ERP V4 | `generated-products/LAF-EnterpriseERP-V4/` | historical |
| **LAF Enterprise ERP V5** | `generated-products/LAF-EnterpriseERP-V5/` | **current** |
| **LAF ScreenStream Assist** | `generated-products/LAF-ScreenStreamAssist/` | **current** |

A natural tidy-up impulse is to move V1–V4 into an `archive/` or `historical/` subfolder. We
explicitly chose **not** to do that.

## Why keep paths unchanged

1. **Scripts reference fixed paths.** Publish, generation-attribution, and benchmark scripts
   (e.g. `scripts/erp-v5/publish-local-production.ps1`, the generation-summary and
   generation-attribution emitters under `benchmarks/results/`) point at these directories by
   name. Moving the trees would break those scripts and invalidate the recorded evidence.
2. **Fresh-clone proofs depend on stable paths.** The V1→V5 progression evidence
   (`docs/reports/ERP_V1_V2_V3_V4_V5_VS_ERPNEXT_COMPARISON.md` and the per-version attribution
   JSON) is only reproducible on a fresh clone if the source trees stay exactly where the
   recorded runs found them.
3. **Git history stays intact.** A path move rewrites blame/history continuity for those trees.
   Status is metadata, not topology — a README marker conveys it without churning the tree.
4. **Source-only, low cost.** Build artifacts and EXEs are git-ignored; the historical trees are
   source-only and small. There is no storage pressure that would justify the risk of moving them.

## How current-vs-historical is signalled instead

- [`generated-products/README.md`](../../generated-products/README.md) — marks each tree current or
  historical with a one-line role.
- [`docs/generated-products/README.md`](../generated-products/README.md) — documentation index of
  current vs historical products.
- [`docs/generated-products/GENERATED_PRODUCTS_STATUS.md`](../generated-products/GENERATED_PRODUCTS_STATUS.md)
  — single status table with scores.
- [`docs/reports/CURRENT_STATUS.md`](CURRENT_STATUS.md) — authoritative current status; supersedes
  any score in a historical tree or report.

## Outcome

Structure is documented, not relocated. Scripts and fresh-clone proofs continue to work; readers
can tell current from historical from the READMEs and the authoritative status file.
