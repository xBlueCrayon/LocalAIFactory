# Expected Answers — Core-Banking Integration

Strong, evidence-aware answers for the 7 questions in `expected-questions.md`. Each names its
**bridge target and mode**. Graph-derived answers trace to a node/edge in the committed fixture
(`schema.sql`, `sample-csharp-middleware/CoreBankingMiddleware.cs`); advisory answers reason
from the contracts and are honest that the fixture does not prove them.

---

## 1. What code posts a transaction? — GRAPH-DERIVED

**Bridge:** `dependents("dbo.usp_PostTransaction")` → `PostingService.PostTransaction`.

`PostingService.PostTransaction` calls `EXEC dbo.usp_PostTransaction` (the only dependent of
that proc). The proc inserts into `dbo.Posting`. So the answer is unambiguous and traceable:
**`CoreBanking.Middleware.PostingService.PostTransaction`**, via the proc, writing `dbo.Posting`.
Related: `PostingService.Reverse` (→ `usp_ReverseTransaction` → `dbo.GLEntry`) for the reversal
path.

---

## 2. What tables are touched by mandate generation? — GRAPH-DERIVED

**Bridge:** `dependencies("...MandateService.GenerateMandate")` → `dbo.Mandate`.

`MandateService.GenerateMandate` inserts into **`dbo.Mandate`**. `dbo.Mandate` has
`FK_Mandate_Account → dbo.Account`, so the mandate cannot exist without its account — the
effective touched set is **`dbo.Mandate` (+ FK dependency on `dbo.Account`)**. This is why
`impact("dbo.Account")` includes `MandateService.GenerateMandate` (Q-impact, see Gold proof).

---

## 3. What changes if the claim response format changes? — GRAPH-DERIVED

**Bridge:** `dependents("dbo.Claim")` → `ClaimResponseService.ProcessResponse`.

The claim-response handler is **`ClaimResponseService.ProcessResponse`**. If the response file
format changes, this is the method that parses/applies it: it
`UPDATE dbo.Claim SET RejectionCode`, reads `dbo.RejectionCode`, and inserts `dbo.SuspenseQueue`.
So a format change has a **bounded blast radius**: `ProcessResponse` plus the three tables it
touches (`dbo.Claim`, `dbo.RejectionCode`, `dbo.SuspenseQueue`). `MandateService.GenerateClaim`
(the writer of `dbo.Claim`) is the upstream producer to check for field compatibility.

---

## 4. What retry / idempotency logic exists for SFTP file processing? — ADVISORY (with graph anchor)

**Bridge:** advisory (contract: `sample-sftp-contracts/settlement-file-contract.md`); graph
anchor on the idempotency control (`dbo.Posting.UQ_Posting_Idem` + `dbo.usp_PostTransaction`).

- **Idempotency (graph-derived):** every posting flows through `dbo.usp_PostTransaction`, which
  inserts only when the `IdempotencyKey` is new; `dbo.Posting` enforces
  `UNIQUE (IdempotencyKey)`. So **a replayed file cannot double-post.**
- **File-level replay (graph anchor):** `SftpFileProcessor.ArchiveFile` records
  `dbo.FileArchive (FileName, ArchivedUtc, Sha256)`; a re-arriving file matching an existing
  `Sha256` is recognised as a replay.
- **Retry/marker (advisory):** the `.done` marker gates pickup; transient SFTP errors retry with
  backoff; archive-after-success means a mid-file crash re-picks the file on the next run, and
  idempotency keeps the re-run safe (at-least-once delivery, exactly-once effect).

**Honest limit:** retry backoff windows and the SFTP poller are deployment config / contract
design — not present as code in the fixture.

---

## 5. What audit trail proves who approved a banking file? — ADVISORY (partial graph anchor)

**Bridge:** advisory (contracts: `sample-api-contracts/posting-api-contract.md` §4/§6,
`sample-sftp-contracts/settlement-file-contract.md` §7); graph anchor: `dbo.FileArchive`.

- **Maker/checker (advisory):** a file release requires `Approver-Id` distinct from
  `Submitter-Id`; self-approval is rejected.
- **Append-only audit (advisory):** submitter, approver, idempotency key, amount, outcome, and
  timestamps are written to an append-only audit row (never updated/deleted → tamper-evident).
- **Durable anchor (graph-derived):** `dbo.FileArchive (ArchivedUtc, Sha256)` is the committed
  record that a named file was processed, with a content hash for integrity.

**Honest limit:** the fixture ships no approval/audit table, so the *identity of the approver*
is advisory design; only the file-processed record (`dbo.FileArchive`) is graph-derived. A real
deployment would add an append-only approval table to make Q5 fully graph-derived.

---

## 6. What breaks if a rejection code changes? — GRAPH-DERIVED

**Bridge:** `dependents("dbo.RejectionCode")` → `ClaimResponseService.ProcessResponse`.

The only consumer of `dbo.RejectionCode` is **`ClaimResponseService.ProcessResponse`**, which
reads it (`SELECT Code, Description ... WHERE Code = ...`) when mapping a rejected claim and then
queues `dbo.SuspenseQueue`. So changing/removing a code, or adding one, affects exactly that
method's mapping behaviour and what lands in suspense. There is also a data dependency:
`dbo.Claim.RejectionCode` stores the code value, so stale/removed codes leave orphaned
references on existing claims. **Bounded blast radius: `ProcessResponse` + `dbo.Claim` +
`dbo.SuspenseQueue`.**

---

## 7. What controls prevent duplicate posting? — GRAPH-DERIVED

**Bridge:** `schema.sql` — `dbo.Posting CONSTRAINT UQ_Posting_Idem UNIQUE (IdempotencyKey)` and
`dbo.usp_PostTransaction` guard; `dependents("dbo.usp_PostTransaction")` →
`PostingService.PostTransaction`.

**Two layers, both in the fixture:**

1. **Unique constraint (DB last line of defence):** `UQ_Posting_Idem UNIQUE (IdempotencyKey)`
   makes a second posting for the same key impossible at the database, even under a race.
2. **Proc guard (graceful no-op):** `dbo.usp_PostTransaction` does
   `IF NOT EXISTS (SELECT 1 FROM dbo.Posting WHERE IdempotencyKey = @Idem) INSERT ...`, so a
   duplicate key is silently ignored rather than raising.

All posting code (`PostingService.PostTransaction`) goes through that proc, so the control is
not bypassable from the application path. **This is the duplicate-posting control.**

---

## Honesty footer

- Q1, Q2, Q3, Q6, Q7 are **graph-derived** — they trace to nodes/edges in the committed fixture
  and correspond to the Gold 6/6 bridge proofs.
- Q4 and Q5 are **advisory** with partial graph anchors (`dbo.Posting`/`usp_PostTransaction` for
  Q4 idempotency; `dbo.FileArchive` for Q5 file-processed record).
- This is an **integration-middleware** fixture, **not a core-banking product**. No vendor
  format/API is reproduced; no compatibility, equivalence, or certification is claimed.
  Regulatory content is **awareness-only**.
