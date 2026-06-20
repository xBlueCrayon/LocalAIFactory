# KYC → Transaction-Approval Scenario

**Original synthetic scenario.** This models the structural surface of a customer-due-diligence
(KYC) onboarding flow that feeds a maker/checker **transaction-approval** path with sanctions/PEP
screening and AML alerting. It is **NOT a vendor product**, **NOT a regulatory artifact**, and
makes **no compliance, certification, or legal-advice claims**. All control language here is
design/awareness only.

The point of the scenario is to prove that LocalAIFactory's **C#↔SQL bridge** can answer real
impact questions about a KYC/AML estate — "what code approves a transaction?", "what touches the
AML alert table?", "what breaks if the Customer table changes?" — by **deriving** the answer from
the dependency graph, not by guessing.

The backing fixture lives at `benchmarks/fixtures/kyc-aml-approval/` and is scored by the
benchmark harness as `KycAmlApproval` (code `KYCAML`).

---

## What this tests

Each C# service method names the SQL objects it touches (table / stored procedure), so the bridge
links service → table/proc and back.

| Surface | Code | SQL |
|---|---|---|
| Open onboarding (KYC) case | `OnboardingService.OpenCase` | `dbo.Customer`, `dbo.OnboardingCase` |
| Capture identity document | `OnboardingService.RecordIdentityDocument` | `dbo.IdentityDocument` |
| Maker/checker onboarding decision | `OnboardingService.RecordApproval` | `dbo.OnboardingApproval` |
| Sanctions/PEP/adverse-media screening | `ScreeningService.ScreenCustomer` | `dbo.ScreeningResult` |
| Risk band + approval limit lookup | `RiskRatingService.RateCustomer` / `GetApprovalLimit` | `dbo.CustomerRiskRating`, `dbo.ApprovalLimit` |
| Submit transaction for approval | `TransactionApprovalService.SubmitForApproval` | `dbo.usp_SubmitTransactionForApproval` |
| Checker decision (approve/reject) | `TransactionApprovalService.ApproveTransaction` | `dbo.usp_ApproveTransaction` |
| Raise / disposition AML alert | `AmlAlertService.RaiseAlert` / `DispositionAlert` | `dbo.AmlAlert` |

---

## How the bridge answers these questions

The bridge resolves four query modes over the merged C#+SQL graph:

- `find(target)` — locate a symbol (table, proc, service method).
- `dependents(target)` — who calls / depends on this object?
- `dependencies(target)` — what does this object call / depend on?
- `impact(target)` — transitive blast radius of a change into the C# services it reaches.

The fixture is scored **Gold 7/7** on these exact proofs:

| Bridge query | Resolves to |
|---|---|
| `find("dbo.usp_ApproveTransaction")` | the procedure symbol |
| `dependents("dbo.usp_SubmitTransactionForApproval")` | `TransactionApprovalService.SubmitForApproval` |
| `dependents("dbo.usp_ApproveTransaction")` | `TransactionApprovalService.ApproveTransaction` |
| `dependents("dbo.AmlAlert")` | `AmlAlertService.RaiseAlert` + `AmlAlertService.DispositionAlert` |
| `dependencies("...ScreeningService.ScreenCustomer")` | `dbo.ScreeningResult` |
| `dependencies("...TransactionApprovalService.ApproveTransaction")` | `dbo.usp_ApproveTransaction` |
| `impact("dbo.Customer")` | `OnboardingService.OpenCase` + `ScreeningService.ScreenCustomer` + `RiskRatingService.RateCustomer` (and more) |

These are **graph-derived** answers: deterministic, traceable to a node and edge in the fixture.

Note on `impact`: the engine reports FK-constraint dependents and flows FK ownership up the
child→parent chain, but it does **not** re-list the changed parent table itself. So an `impact`
proof asserts the **C# service symbols** the change reaches, not the parent table name.

---

## Honest split: graph-derived vs advisory

| Capability | Mode |
|---|---|
| Code↔table↔proc dependency / dependents / dependencies / impact | **GRAPH-DERIVED** (Gold 7/7) |
| Idempotent submit via `UNIQUE IdempotencyKey` + proc guard | **GRAPH-DERIVED** (`schema.sql`) |
| Maker/checker segregation (maker ≠ checker) | **PARTLY GRAPH-DERIVED** (proc logic + `UNIQUE` keys), partly advisory (operating manual) |
| Per-band approval limits | **PARTLY GRAPH-DERIVED** (`ApprovalLimit` table) + advisory (manual) |
| Regulatory / scheme-rule conformance | **ADVISORY / awareness-only** (no compliance claim) |

This is a structural fixture the bridge reasons about. It does not perform real screening, does
not block real money movement, and does not assert conformance to any law or scheme.

---

## How to run validation

```powershell
./validation-script.ps1
```

It invokes the benchmark harness against the committed `KYCAML` fixture (in-memory, no network)
and asserts **Gold `pov=7/7`**. Exit code `0` means the capability is proven live; non-zero means
the proofs regressed.
