# Approval Matrix — Management-Company Workflows

Original synthetic, illustrative approval authorities for a management company overseeing the
estate. Awareness-only; example values, not policy.

## Governance approval authorities

| Decision | Maker | Checker (must differ) | Escalation |
|---|---|---|---|
| Accept a low-risk change | Service manager | Oversight lead | — |
| Accept a controlled-schema change | Service manager | Oversight lead + Control owner | Risk committee |
| Accept an approval/segregation logic change | Engineering lead | Oversight lead + Control owner | Risk committee |
| Maintain approval authorities | Oversight lead | Head of operations | — |

## Risk-tier routing (illustrative)

| Risk tier | Authority |
|---|---|
| Low | Service manager |
| Medium | Oversight lead |
| High | Head of operations / risk committee |

## Enforcement honesty

- Maker ≠ checker is enforced at the proc/constraint level in the backing fixtures; management-level
  segregation depends on identity and role configuration described here.
- These authorities and tiers are example values for the scenario, not a sanctioned schedule.
