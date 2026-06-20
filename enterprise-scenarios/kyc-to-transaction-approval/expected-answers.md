# Expected Answers — KYC → Transaction Approval

Graph-derived answers from the committed `KYCAML` fixture (benchmark code `KYCAML`, scored Gold 7/7).
All `dbo.*` names refer to objects in `benchmarks/fixtures/kyc-aml-approval/schema.sql`; all
`KycAml.Services.*` names refer to `KycAmlApprovalServices.cs`.

| # | Question | Mode | Answer (must contain) |
|---|---|---|---|
| 1 | Where is `usp_ApproveTransaction`? | find | `dbo.usp_ApproveTransaction` (n ≥ 1) |
| 2 | What submits a transaction for approval? | dependents | `KycAml.Services.TransactionApprovalService.SubmitForApproval` |
| 3 | What approves a transaction? | dependents | `KycAml.Services.TransactionApprovalService.ApproveTransaction` |
| 4 | What touches the AML alert table? | dependents | `KycAml.Services.AmlAlertService.RaiseAlert`, `KycAml.Services.AmlAlertService.DispositionAlert` |
| 5 | What does `ScreeningService.ScreenCustomer` depend on? | dependencies | `dbo.ScreeningResult` |
| 6 | What does `TransactionApprovalService.ApproveTransaction` depend on? | dependencies | `dbo.usp_ApproveTransaction` |
| 7 | What breaks if `Customer` changes? | impact | `KycAml.Services.OnboardingService.OpenCase`, `KycAml.Services.ScreeningService.ScreenCustomer`, `KycAml.Services.RiskRatingService.RateCustomer` |

## Why the impact answer lists C# services, not the Customer table

`impact("dbo.Customer")` seeds the BFS with the `Customer` symbol. The engine reports the FK
constraints that reference `Customer` as dependents and flows FK ownership **up** from each child
table (e.g. `CustomerRiskRating`, `ScreeningResult`, `OnboardingCase`) — then surfaces the C#
services that access those child tables. The changed parent (`Customer`) is in the visited seed and
is intentionally **not** re-listed. The proof therefore asserts the reached **C# service symbols**,
matching how the COREBANK and ERPCRM impact proofs are written.

## Honest limits

- The maker/checker and approval-limit answers above the proc/constraint level are advisory
  (`controls-matrix.md`, `approval-matrix.md`); they are not graph proofs.
- No regulatory sufficiency is asserted.
