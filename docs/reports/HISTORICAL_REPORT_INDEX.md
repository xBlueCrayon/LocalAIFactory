# Historical Report Index

**Authoritative current status:** [`CURRENT_STATUS.md`](CURRENT_STATUS.md)

`docs/reports/` holds ~170 **point-in-time evidence reports** plus a handful of living documents
(see [`README.md`](README.md)). **Every report below is a snapshot, superseded by
[`CURRENT_STATUS.md`](CURRENT_STATUS.md)** where numbers differ. This index groups them by theme/
filename-prefix rather than listing all of them.

## Groups (by filename prefix / theme)

### Near-GA, production-readiness, and release gates (~45)
Prefixes: `NEAR_*`, `PRODUCTION_*`, `MODE_*`, `FINAL_*`, `PREFINAL_*`, `POST_*`, `HIGH_*`,
`RELIABILITY_*`, `PERFORMANCE_*`, `DRAFT_*`.
The near-GA scoring model, production-readiness gate runs (V1/V2/V3), mode/profile evaluations, and
the final/prefinal release-candidate sweeps. **Current gate:**
`NEAR_GA_READY_WITH_EXTERNAL_PROOF_MODEL` (see `CURRENT_STATUS.md`).

### LAF factory / autonomy / fix-loop (~31)
Prefix: `LAF_*`.
LocalAIFactory factory runs: autonomous fix-loop proofs, generation loops, attribution and summary
reports, and factory-level evaluations.

### ERP V1–V5 vs ERPNext (~26)
Prefixes: `ERP_*`, `ERPNEXT_*`, `ENTERPRISE_*`.
The ERP version progression and ERPNext comparisons. **Current product:** ERP V5 = `ERP_PILOT_READY`,
~48% parity / ~57% production-grade. Canonical comparison:
[`ERP_V1_V2_V3_V4_V5_VS_ERPNEXT_COMPARISON.md`](ERP_V1_V2_V3_V4_V5_VS_ERPNEXT_COMPARISON.md).
See [`docs/generated-products/LAF_ENTERPRISE_ERP_V5.md`](../generated-products/LAF_ENTERPRISE_ERP_V5.md).

### ScreenStream (~21)
Prefix: `SCREENSTREAM_*`.
ScreenStream Assist generation, capture/protocol, packaging/network, adaptive loops, and test
evidence. **Current product:** `LAN_READY`, ~72% production-grade. See
[`docs/generated-products/LAF_SCREENSTREAM_ASSIST.md`](../generated-products/LAF_SCREENSTREAM_ASSIST.md).

### Deployment, IIS, load, operator, public-systems (~24)
Prefixes: `DEPLOYMENT_*`, `IIS_*`, `LOAD_*`, `OPERATOR_*`, `PUBLIC_*`.
IIS deployment drills, smoke/load runs, operator interaction, and public-systems understanding/estate
mapping.

### Knowledge engine, human-interaction, and cross-cutting (~25)
Prefixes/themes: `HUMAN_*`, `THEORY_*`, `MULTI_*`, `CROSS_*`, `COMBINED_*`, `UNLIMITED_*`, `TECH_*`,
`SECURITY_*`, `OFFICIAL_*`, `FULL_*`, `EXTERNAL_*`, `FRESH_*`, `AUTONOMOUS_*`, `GENERATED_*`,
`INTERRUPTION_*`, `LOCAL_*`, `REPOSITORY_*`, `LOCALAIFACTORY_*`.
Knowledge-engine evidence, human-interaction impact, theory/innovation notes, security audits,
fresh-clone proofs, and other cross-cutting reports. For the current knowledge state see
[`KNOWLEDGE_ENGINE_READY_REPORT.md`](KNOWLEDGE_ENGINE_READY_REPORT.md).

## How to find a specific report

The filenames are descriptive and prefix-grouped as above. List `docs/reports/*.md` and filter by
the prefix for the theme you need. Group counts are approximate and shift as living docs are added;
the **total** of historical reports is ~170, and all of them defer to
[`CURRENT_STATUS.md`](CURRENT_STATUS.md).
