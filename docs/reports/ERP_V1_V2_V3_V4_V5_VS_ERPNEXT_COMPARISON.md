# ERP V1–V5 vs ERPNext Comparison

Source: `benchmarks/erpnext-study/erp-v1-v2-v3-v4-v5-erpnext-score.json`.

## Progression

| Metric | V1 | V2 | V3 | V4 | V5 |
|---|---|---|---|---|---|
| ERPNext parity score | 36 | 37 | 42 | 45 | **48** |
| Production-grade score | 35 | 35 | 48 | 50 | **57** |
| Tests | 74 | 82 | 108 | 122 | **134** |
| Modules | 0 | 3 | 15 | 22 | **29** |

## Reading the trend

- **Parity** climbs steadily (36 -> 48) but remains well below ERPNext free-grade.
- **Production-grade** jumps from 50 to 57 at V5, driven by the create-form UI, the 6 new
  modules, and the broader test base.
- **Modules** reach 29 (24 spec + 5 governed local-LLM).

## Verdict

**V5 > V4** on every axis. But V5 is still **ERP_PILOT_READY**, **not** production-grade and
**not** ERPNext free-grade (`reachedErpNextFreeGrade=false`). The remaining distance is
edit/delete + list-detail UI, EF migrations, backup/restore, load testing, and deep modules
(MRP, payroll, POS, storefront, returns) — plus the external gates (auth, CA TLS, security
review, customer acceptance).
