# Workflow: KYC → Screening → Approval → Transaction

Original synthetic workflow. Awareness-only; not a compliance procedure.

## Stages

1. **Open onboarding case** — `OnboardingService.OpenCase` creates a `Customer` row and an
   `OnboardingCase` in stage `IdentityCheck`.
2. **Capture identity** — `OnboardingService.RecordIdentityDocument` stores an `IdentityDocument`
   (unique on `DocType, DocReference` to prevent duplicate capture).
3. **Screen** — `ScreeningService.ScreenCustomer` runs sanctions/PEP/adverse-media screening and
   writes a `ScreeningResult` (`MatchStatus` = `NoMatch` / `PotentialMatch` / `ConfirmedMatch`).
4. **Risk-rate** — `RiskRatingService.RateCustomer` assigns a `RiskBand` (`Low`/`Medium`/`High`)
   that drives the per-band `ApprovalLimit`.
5. **Onboarding maker/checker** — `OnboardingService.RecordApproval` records a maker decision; a
   different checker confirms it (`UQ_OnboardingApproval_Case` blocks duplicate maker rows).
6. **Submit transaction** — `TransactionApprovalService.SubmitForApproval` calls
   `usp_SubmitTransactionForApproval`, which idempotently creates a `TransactionRequest`
   (`PendingApproval`) and records the maker on a `TransactionApproval` row.
7. **AML alerting** — `AmlAlertService.RaiseAlert` attaches an `AmlAlert` to the request; alerts
   are dispositioned via `DispositionAlert`.
8. **Checker decision** — `TransactionApprovalService.ApproveTransaction` calls
   `usp_ApproveTransaction`, which enforces maker ≠ checker on a still-pending request and flips
   the request to `Approved` or `Rejected`.

## State transitions (TransactionRequest.Status)

```
(none) --submit--> PendingApproval --checker approve--> Approved
                                   \--checker reject---> Rejected
```

## Idempotency & segregation controls (structural)

- `UQ_TxnRequest_Idem` (UNIQUE IdempotencyKey) + the proc's `IF NOT EXISTS` guard make submit a
  safe no-op on retry.
- `usp_ApproveTransaction` only acts when `MakerUser <> @CheckerUser` and the row is still
  `Submitted` — segregation and single-decision are enforced in the proc.
