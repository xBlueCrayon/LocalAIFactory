# Expected Capabilities — Honest Today vs Future

> Fictional scenario (Zephyr Mutual Assurance). Awareness-only on FSC/Mauritius.
> Not legal or insurance advice; no compliance or certification claim.

This file states honestly what a LocalAIFactory-style build could reasonably achieve **today**
versus what is **future / aspirational**. It avoids over-claiming.

## Today (achievable with the current stack)

- Register a claim against a policy and snapshot cover at registration time.
- Move a claim through an explicit state machine (FNOL → triage → assessment → reserve →
  decision → settlement → closure) with each transition logged.
- Enforce role-based access and separation of duties on approval and payment release.
- Authority thresholds by claim value band, evaluated server-side before commit.
- Append-only reserve history with reason codes and full change trail.
- Dual-control payment instruction (raise / approve / release by different actors).
- Append-only audit log written in the same transaction as each change.
- Standard operational reports (open claims, reserve movement, cycle time) from MSSQL.
- Full functionality in **MSSQL-only** mode with manual entry when feeds are absent.
- Optimistic concurrency to prevent silent overwrite of reserves.

## Near-future (incremental, low risk)

- Duplicate-FNOL detection heuristics (policy + loss date + claimant).
- Configurable SLA timers and exception dashboards.
- Read-only policy-admin and payments-file integrations behind interfaces.
- Document-store linkage for assessment evidence by reference.
- Reserve-vs-quantum drift alerts.

## Future / aspirational (explicitly NOT claimed today)

- Local-model assistance for triage suggestions and summarisation (advisory only, human-decided).
- Automated quantum estimation support from historical claims (decision support, not authority).
- Anomaly detection on settlement patterns.
- Straight-through processing for low-value, low-risk claim types.

## Explicitly out of scope / not claimed

- **No** sanctions screening, AML decisioning, or KYC verification product.
- **No** regulatory compliance, FSC conformance, or certification of any kind.
- **No** movement of funds — the platform produces an instruction file only.
- **No** legal or actuarial advice; reserving logic is operational, not actuarial sign-off.
- **No** guarantee of correctness of imported legacy data.

## Honesty notes

Where a capability depends on an external system (policy feed, payments file, directory), the
platform must degrade gracefully and never block page rendering. Any AI-assisted feature is
advisory and human-confirmed; the system of record remains MSSQL with a human-owned audit trail.
