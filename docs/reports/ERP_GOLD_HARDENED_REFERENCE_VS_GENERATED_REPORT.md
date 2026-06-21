# ERP Gold — Hardened Reference vs Generated Report

**Sprint:** ERP-GOLD HARDENING · **Stamp:** 2026-06-21

Compares the hand-hardened reference product against a deterministic generator re-run, to prove the hardening lives in the generator templates/specs and is reproducible.

- **Reference:** LAF Enterprise ERP Gold (`generated-products/LAF-EnterpriseERP-Gold`)
- **Reproduction:** GoldGenerated-Hardened (`generated-products/LAF-EnterpriseERP-GoldGenerated-Hardened`)

## Comparison

| Dimension | Gold (reference) | GoldGenerated-Hardened | Reproduction |
|-----------|------------------|------------------------|--------------|
| Build | 0 errors | 0 errors | identical |
| xUnit | 210 PASS | 190 PASS | 90.5% (190/210) |
| Playwright | 38 PASS | 38 PASS | 100% |
| Deterministic surface | full | full | 100% |
| Spec modules | 28 | 23 | 23/28 = 82% |
| Generator autonomy | — | 100% | — |

## Why the gap is expected

The 20-test xUnit gap = **5 non-deterministic local-LLM-proposed catalog modules × 4 tests each**. A pure deterministic generator run does not regenerate LLM-proposed content, so:

- 100% of the **deterministic surface** reproduces (engine, real auth, cookie login, role claims, audit, deployment scripts, double-entry GL, maker/checker, RBAC, migrations, edit/soft-delete UI).
- The only Gold-exclusive content is the 5 LLM-proposed catalog modules (`CustomerSegment`, `ProductCategory`, `EmployeeRole`, `MarketingCampaign`, `VendorContract`) and their tests.

## Discipline

Every manual hardening action (auth, lockout, anti-forgery, secure cookie, EF migrations, edit/soft-delete UI, deployment) was written **back into the generator templates / specs / `Program.cs`** — never left as a one-off product edit. That is what lets GoldGenerated-Hardened reproduce the hardened reference.

## Honest limitations

- Module reproduction is **82% (23/28)**, not 100%, because LLM-proposed modules are non-deterministic by nature.
- Reproduction was measured by build + test counts, not by byte-for-byte file diff.
- Both products build and test green locally; neither was applied against a live SQL Express here.
