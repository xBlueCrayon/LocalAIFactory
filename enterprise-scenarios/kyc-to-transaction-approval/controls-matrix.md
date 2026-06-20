# Controls Matrix — KYC → Transaction Approval

Original synthetic mapping of design controls to their representation in the fixture. Awareness-only;
not a compliance attestation and not an assertion that these controls satisfy any law or scheme.

| Control (design intent) | Where represented | Mode |
|---|---|---|
| Customer identity captured before activity | `dbo.IdentityDocument` + `OnboardingCase.Stage` | Structural (table + stage field) |
| No duplicate identity capture | `UQ_IdentityDoc_Ref (DocType, DocReference)` | Graph-derived (UNIQUE) |
| Sanctions / PEP / adverse-media screening recorded | `dbo.ScreeningResult.ListType` + `MatchStatus` | Structural |
| Risk-based handling | `dbo.CustomerRiskRating.RiskBand` drives `dbo.ApprovalLimit` | Structural |
| Per-band / per-role approval limit | `UQ_ApprovalLimit_Band_Role` | Graph-derived (UNIQUE) |
| Onboarding maker/checker segregation | `OnboardingApproval` (maker + checker cols), `UQ_OnboardingApproval_Case` | Partly structural / partly advisory |
| Transaction submitted by a maker | `usp_SubmitTransactionForApproval` records `MakerUser` | Structural (proc logic) |
| Transaction approved by a different checker | `usp_ApproveTransaction` enforces `MakerUser <> @CheckerUser` | Graph-derived (proc guard) |
| Single decision per request | `UQ_TxnApproval_Request_Maker` + `Decision = 'Submitted'` precondition | Graph-derived |
| Duplicate-submit safe | `UQ_TxnRequest_Idem` + proc `IF NOT EXISTS` | Graph-derived |
| AML alert raised and dispositioned | `dbo.AmlAlert.AlertType` + `Disposition` | Structural |

## What is NOT claimed

- This matrix does not assert that the controls are *sufficient* for any regulatory regime.
- Screening here is a recorded outcome, not a real list-matching engine.
- Maker/checker enforcement is demonstrated at the proc/constraint level only; production
  segregation also depends on application identity and role configuration described in the
  operating manual.
