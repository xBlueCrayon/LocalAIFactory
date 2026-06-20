# Approval Matrix — KYC → Transaction Approval

Original synthetic illustrative thresholds. Awareness-only; figures are example values for the
fixture, not policy and not a recommendation.

## Maker/checker roles

| Step | Maker | Checker (must differ) | Backing object |
|---|---|---|---|
| Onboarding decision | Onboarding officer | Onboarding supervisor | `dbo.OnboardingApproval` |
| Transaction submit | Operations maker | — | `usp_SubmitTransactionForApproval` |
| Transaction decision | — | Operations checker | `usp_ApproveTransaction` |

## Example per-band approval limits (`dbo.ApprovalLimit`)

| Risk band | Approver role | Example max amount |
|---|---|---|
| Low | TeamLead | 50,000 |
| Medium | Manager | 250,000 |
| High | SeniorManager | 1,000,000 |
| High | Committee | above 1,000,000 |

These rows are illustrative seed values; the fixture stores the shape, not a sanctioned schedule.

## Enforcement points

- Segregation: `usp_ApproveTransaction` rejects a decision where `@CheckerUser` equals the
  recorded `MakerUser`.
- Single decision: the proc only acts while the approval row is `Submitted`.
- Limit lookup: `RiskRatingService.GetApprovalLimit` reads `ApprovalLimit` by `RiskBand`; the
  application is expected to compare the request amount to `MaxAmount` before routing to the right
  approver role (application-level, described here, not enforced by the proc).
