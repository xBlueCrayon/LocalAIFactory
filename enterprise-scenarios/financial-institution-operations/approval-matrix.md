# Approval Matrix — Financial-Institution Operations

Original synthetic, illustrative approval routing for operational changes. Awareness-only; example
values, not policy.

## Change-class routing

| Change class | Example trigger | Maker | Checker (must differ) |
|---|---|---|---|
| Low-risk config | non-controlled lookup edit | Engineer | Team lead |
| Schema change (controlled table) | alter posting/screening/approval table | Engineer | Tech lead + Control owner |
| Procedure change (approval/segregation logic) | edit `usp_ApproveTransaction` | Engineer | Tech lead + Control owner |
| Limit/threshold change | edit `ApprovalLimit` rows | Operations maker | Operations checker |

## Amount/risk routing (illustrative, see KYCAML `approval-matrix.md`)

| Risk band | Approver role |
|---|---|
| Low | Team lead |
| Medium | Manager |
| High | Senior manager / committee |

## Enforcement honesty

- Segregation (maker ≠ checker) is enforced at the proc/constraint level in the backing fixtures;
  organisation-wide segregation depends on identity and role configuration described here, not
  guaranteed by the graph.
- These thresholds are example values for the scenario, not a sanctioned schedule.
