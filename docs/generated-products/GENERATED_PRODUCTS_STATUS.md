# Generated Products — Status

**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](../reports/CURRENT_STATUS.md)
**Verified:** 2026-06-21 · Commit `96fbbc4`

One-table status of every tree under `generated-products/`. Scores are local proofs, not commercial
GA claims.

| Product | Path | Current? | Status | Build | Tests | Scores |
|---|---|---|---|---|---|---|
| **LAF Enterprise ERP V5** | `LAF-EnterpriseERP-V5/` | ✅ current | `ERP_PILOT_READY` | ✅ 0 errors | 134 .NET + 14 Playwright | ERPNext parity ~48%, production-grade ~57%, 100% generation autonomy, 29 modules |
| **LAF ScreenStream Assist** | `LAF-ScreenStreamAssist/` | ✅ current | `LAN_READY` | ✅ 0 errors | 12 .NET + 4 Playwright | production-grade ~72% (capped by no TLS/WSS + no code-signing) |
| LAF Enterprise ERP (V1) | `LAF-EnterpriseERP/` | historical | reference baseline | — | — | hand-built baseline the generator later replaced |
| LAF Enterprise ERP V2 | `LAF-EnterpriseERP-LAFGenerated/` | historical | template-copy generation | — | — | first generator-emitted ERP |
| LAF Enterprise ERP V3 | `LAF-EnterpriseERP-V3/` | historical | data-driven generation | — | — | first data-driven gen (+P&L / Balance Sheet) |
| LAF Enterprise ERP V4 | `LAF-EnterpriseERP-V4/` | historical | expanded spec | — | — | expanded data-driven spec |

## Notes

- **Current** = the product that represents the live state of the line. Historical trees are kept for
  version-progression evidence and fresh-clone proofs; their scores are superseded.
- V1→V5 progression: ERPNext parity 36→48%, tests 74→134, modules 0→29
  ([`docs/reports/ERP_V1_V2_V3_V4_V5_VS_ERPNEXT_COMPARISON.md`](../reports/ERP_V1_V2_V3_V4_V5_VS_ERPNEXT_COMPARISON.md)).
- Build artifacts/EXEs are git-ignored; trees are source-only. Publish locally to run (see the
  per-product docs).
- Per-product detail: [`LAF_ENTERPRISE_ERP_V5.md`](LAF_ENTERPRISE_ERP_V5.md),
  [`LAF_SCREENSTREAM_ASSIST.md`](LAF_SCREENSTREAM_ASSIST.md).

**No commercial GA, no ERPNext parity claim, no internet-ready ScreenStream, no fake 100%.**
