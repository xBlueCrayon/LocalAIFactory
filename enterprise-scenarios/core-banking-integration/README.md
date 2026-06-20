# Core-Banking Integration Scenario

**Original synthetic integration-middleware scenario.** This models a BDM-style
direct-debit / settlement **integration layer** that talks to a core banking system over
SFTP file exchange and stored-procedure calls. It is **NOT a core-banking product**, not a
ledger engine, and makes **no vendor compatibility, equivalence, or certification claims**.
All regulatory content is **awareness-only**, not legal/compliance advice.

The point of the scenario is to prove that LocalAIFactory's **C#↔SQL bridge** can answer
real impact questions about a banking integration estate: "what code posts a transaction?",
"what breaks if a rejection code changes?", "what controls prevent duplicate posting?" — by
**deriving** the answer from the dependency graph, not by guessing.

---

## What this tests

The fixture is a small but representative integration surface. Each C# method names the SQL
objects it touches, so the bridge can link integration code → core tables/procedures and back.

| Integration surface | Code | Core SQL |
|---|---|---|
| **Posting** (idempotent debit/credit) | `PostingService.PostTransaction` | `dbo.usp_PostTransaction` → `dbo.Posting` |
| **Reversal** | `PostingService.Reverse` | `dbo.usp_ReverseTransaction` → `dbo.GLEntry` |
| **Balance read** | `PostingService.AccountBalance` | `dbo.Account` |
| **Mandate** (direct-debit authority) | `MandateService.GenerateMandate` | `dbo.Mandate` (→ `dbo.Account`) |
| **Claim** (collection against mandate) | `MandateService.GenerateClaim` | `dbo.Claim` |
| **Claim response** (rejection handling) | `ClaimResponseService.ProcessResponse` | `dbo.Claim`, `dbo.RejectionCode`, `dbo.SuspenseQueue` |
| **Settlement reconciliation** | `SettlementReconciliationService.Reconcile` | `dbo.SettlementFile`, `dbo.Posting` |
| **Suspense** (exception queue) | `ClaimResponseService.ProcessResponse` | `dbo.SuspenseQueue` |
| **GL** (general-ledger lines) | `PostingService.Reverse` | `dbo.GLEntry` |
| **File archive / replay** | `SftpFileProcessor.ArchiveFile` / `RegisterIncoming` | `dbo.FileArchive`, `dbo.SettlementFile` |

Fixture source of truth:
- `schema.sql` — `dbo.Account`, `AccountHold`, `Mandate`, `Claim`, `RejectionCode`,
  `SettlementFile`, `Posting` (`UNIQUE IdempotencyKey`), `SuspenseQueue`, `GLEntry`,
  `FileArchive`; procs `dbo.usp_PostTransaction` (idempotent insert) and
  `dbo.usp_ReverseTransaction`.
- `sample-csharp-middleware/CoreBankingMiddleware.cs` — namespace `CoreBanking.Middleware`.

---

## How the bridge answers core-banking impact questions

The bridge resolves four query modes over the merged C#+SQL graph:

- `dependents(target)` — who calls / depends on this object?
- `dependencies(target)` — what does this object call / depend on?
- `impact(target)` — transitive blast radius of a change.

The fixture is scored **Gold 6/6** on these exact proofs:

| Bridge query | Resolves to |
|---|---|
| `dependents("dbo.usp_PostTransaction")` | `PostingService.PostTransaction` |
| `dependencies("...MandateService.GenerateMandate")` | `dbo.Mandate` |
| `dependents("dbo.Claim")` | `ClaimResponseService.ProcessResponse` |
| `dependents("dbo.RejectionCode")` | `ClaimResponseService.ProcessResponse` |
| `dependents("dbo.FileArchive")` | `SftpFileProcessor.ArchiveFile` |
| `impact("dbo.Account")` | `PostingService.AccountBalance` + `MandateService.GenerateMandate` |

These are **graph-derived** answers: deterministic, traceable to a node and edge in the
fixture, not advisory prose.

---

## How banking controls are represented

The fixture deliberately encodes the controls a banking integration reviewer asks about:

- **Idempotency / duplicate-posting control.** `dbo.Posting` has
  `CONSTRAINT UQ_Posting_Idem UNIQUE (IdempotencyKey)` — a hard DB guarantee that one
  idempotency key yields at most one posting. `dbo.usp_PostTransaction` adds a proc-level
  guard (`IF NOT EXISTS (... WHERE IdempotencyKey = @Idem) INSERT ...`) so a duplicate key is
  silently ignored rather than erroring. **Two layers**: unique constraint (last line of
  defence) + proc guard (graceful no-op). See `sample-api-contracts/posting-api-contract.md`.
- **Maker/checker.** Represented as an **advisory** control in the API and SFTP contracts:
  a posting/file-release requires a separate approver from the submitter. The fixture does not
  ship an approval table, so this is design-level, not graph-derived (stated honestly in
  `expected-answers.md`).
- **Audit / who-approved.** Represented as **append-only audit fields** in the contracts
  (submitter, approver, timestamps, idempotency key, file SHA-256). `dbo.FileArchive` carries
  `ArchivedUtc` + `Sha256` as the durable record that a file was processed.
- **Retry.** SFTP retry/replay is described in the file contract: `.done` marker gates pickup,
  archive-after-success, and idempotency key prevents re-processing from double-posting.
- **File replay.** `dbo.FileArchive.Sha256` lets the processor detect a previously archived
  file and replay safely; `RegisterIncoming` / `ArchiveFile` bracket the lifecycle.

---

## Honest split: graph-derived vs advisory

| Capability | Mode |
|---|---|
| Code↔table↔proc dependency, dependents, impact | **GRAPH-DERIVED** (Gold 6/6) |
| Idempotency / duplicate-posting via `UNIQUE IdempotencyKey` + proc guard | **GRAPH-DERIVED** (constraint + proc in `schema.sql`) |
| Rejection-code / claim-response blast radius | **GRAPH-DERIVED** (`ProcessResponse` dependents) |
| Maker/checker approval flow | **ADVISORY** (contract design; no approval table in fixture) |
| Append-only audit of approver identity | **ADVISORY** (contract field design; partial graph anchor via `FileArchive`) |
| SFTP retry / host-key / auth policy | **ADVISORY** (contract design) |
| Regulatory / scheme-rule conformance | **ADVISORY / awareness-only** (no compliance claim) |

**This is not a core-banking replacement.** It is an integration middleware fixture that the
bridge reasons about. It does not run a ledger, does not settle real money, and does not assert
conformance to any payment scheme or vendor format.

---

## How to run validation

From the scenario folder (or anywhere — it resolves the repo root):

```powershell
./validation-script.ps1
```

It runs the benchmark harness (`tools/LocalAIFactory.Benchmark`, in-memory, standard suite),
filters for the `CoreBankingIntegration` fixture, and asserts **Gold `pov=6/6`**. Exit code `0`
means the capability is proven live against the committed fixture; non-zero means the proofs
regressed.
