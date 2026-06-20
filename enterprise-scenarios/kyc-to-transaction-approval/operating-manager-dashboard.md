# Operating-Manager Dashboard — KYC → Transaction Approval

Original synthetic dashboard sketch for an operating manager. Awareness-only; the metrics below
are illustrative views over the fixture's tables, not live figures or KPIs.

## Panels

1. **Onboarding pipeline** — count of `OnboardingCase` by `Stage` and `Outcome`.
   Source: `dbo.OnboardingCase`.
2. **Screening posture** — `ScreeningResult` grouped by `ListType` × `MatchStatus`; highlight any
   `ConfirmedMatch`. Source: `dbo.ScreeningResult`.
3. **Risk distribution** — customers by `RiskBand`. Source: `dbo.CustomerRiskRating`.
4. **Pending approvals** — `TransactionRequest` where `Status = 'PendingApproval'`, with age.
   Source: `TransactionApprovalService.ListPendingApprovals` → `dbo.TransactionRequest`.
5. **Maker/checker throughput** — approved vs rejected `TransactionApproval` rows over a window.
   Source: `dbo.TransactionApproval`.
6. **Open AML alerts** — `AmlAlert` where `Disposition IS NULL`, by `AlertType`.
   Source: `dbo.AmlAlert`.

## Manager actions the bridge can support

- "What code do I need to change to add a new screening list type?" → `dependents("dbo.ScreeningResult")`.
- "If we change the Customer table, what breaks?" → `impact("dbo.Customer")`.
- "Who approves a transaction?" → `dependents("dbo.usp_ApproveTransaction")`.

Each answer is graph-derived from the committed fixture, with provenance to a file and line span.

## Caveats

The dashboard reflects fixture structure only. It does not compute real risk, does not score real
customers, and must not be read as an operational or regulatory status.
