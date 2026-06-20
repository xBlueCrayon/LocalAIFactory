# Posting API Contract (original, conceptual)

**Original synthetic contract.** A generic, conceptual API for the posting/reversal/balance/hold
surface of the integration middleware. **No vendor API is copied or implied.** Endpoints, headers,
and field names are illustrative and original. Awareness-only design material — not compliance,
legal, or scheme advice.

Backing code: `CoreBanking.Middleware.PostingService`. Backing SQL:
`dbo.usp_PostTransaction`, `dbo.usp_ReverseTransaction`, `dbo.Posting`, `dbo.Account`,
`dbo.AccountHold`, `dbo.GLEntry`.

---

## 1. Conventions

- All mutating requests **must** carry an idempotency header:
  `Idempotency-Key: <unique-string>` (max 64 chars → maps to `dbo.Posting.IdempotencyKey`,
  `NVARCHAR(64) UNIQUE`).
- All requests carry `Submitter-Id`; release of a posting carries `Approver-Id`
  (maker/checker — advisory).
- Money is `decimal(18,2)`; currency/account identifiers are strings.
- Errors return a structured body: `{ code, message, rejectionCode? }`.

---

## 2. Endpoints

### `POST /postings` — post a transaction
Posts a debit/credit to the core system via `PostingService.PostTransaction`
→ `EXEC dbo.usp_PostTransaction`.

Request:
```
Idempotency-Key: k-2026-0001
Submitter-Id: maker.jdoe
{ "accountId": 1, "amount": 100.00 }
```

Behaviour:
- The proc inserts the posting **only if** `IdempotencyKey` is not already present.
- The `UQ_Posting_Idem` unique constraint is the hard guarantee.
- A **replayed** request with the same key is a safe **no-op** that returns the original
  posting (`200`/`409`-as-success-idempotent), never a second posting.

### `POST /postings/{id}/reversals` — reverse a posting
`PostingService.Reverse` → `EXEC dbo.usp_ReverseTransaction` → writes a `REVERSAL`
`dbo.GLEntry`. Reversal is itself recorded in the GL for audit.

### `GET /accounts/{id}/balance` — read balance
`PostingService.AccountBalance` → `SELECT Balance FROM dbo.Account`.

### `POST /accounts/{id}/holds` — place a hold
Conceptual: reserves funds against `dbo.AccountHold`. (Read-side hold visibility reduces the
available balance; advisory — not exercised by the bridge proofs.)

---

## 3. Idempotency / duplicate-posting control

**Two layers, both real in the fixture:**

1. **Proc guard** — `dbo.usp_PostTransaction` does
   `IF NOT EXISTS (SELECT 1 FROM dbo.Posting WHERE IdempotencyKey = @Idem) INSERT ...`.
   A duplicate key is a graceful no-op.
2. **Unique constraint** — `dbo.Posting CONSTRAINT UQ_Posting_Idem UNIQUE (IdempotencyKey)`.
   Even under a race, the DB rejects a second row for the same key.

This is the control that prevents duplicate posting (see `expected-answers.md`, Q7).

---

## 4. Maker/checker approval (advisory)

A posting above a threshold (or any file-driven batch release) requires a **separate approver**
from the submitter:

- `Submitter-Id` (maker) creates the request.
- `Approver-Id` (checker) — must differ from submitter — releases it.
- Self-approval is rejected.

This is **advisory** in this fixture: there is no approval table in `schema.sql`, so the
maker/checker flow is contract design, not a graph-derived proof. The contract specifies the
fields; an implementation would persist them in an append-only approval/audit table.

---

## 5. Error & rejection mapping

- Validation/business errors → `{ code, message }`.
- Items that fail downstream (returns/rejections from the host) are mapped through
  `dbo.RejectionCode` and queued to `dbo.SuspenseQueue` by
  `ClaimResponseService.ProcessResponse` — the same path used for settlement response files.
- A rejection code that is not in `dbo.RejectionCode` is itself an exception (unknown-code →
  suspense), never a silent pass.

---

## 6. Audit fields (append-only)

Every mutating call is expected to record an **append-only** audit row:

| Field | Source |
|---|---|
| `IdempotencyKey` | request header → `dbo.Posting.IdempotencyKey` |
| `SubmitterId` | `Submitter-Id` |
| `ApproverId` | `Approver-Id` (maker/checker) |
| `Amount`, `AccountId` | request body → `dbo.Posting` |
| `OccurredUtc` | server time |
| `Outcome` | posted / no-op-replay / rejected (+ `rejectionCode`) |

For file-driven flows the durable anchor is `dbo.FileArchive (ArchivedUtc, Sha256)`. Audit is
**append-only**: rows are never updated or deleted, so the trail of who-submitted/who-approved
is tamper-evident (advisory beyond `FileArchive`).

---

## 7. Honesty / limits

- Conceptual API only; **no vendor API contract is reproduced**.
- Maker/checker and approver audit are advisory design (no approval table in the fixture).
- No scheme conformance, no certification, no compliance claim. Regulatory notes are
  awareness-only.
