# Settlement File Exchange Contract (original, conceptual)

**Original synthetic contract.** This describes a generic, conceptual settlement file exchange
for a BDM-style direct-debit integration. **No vendor file format is copied or implied.** Field
names and layouts are illustrative and original. This is awareness-only design material, not a
payment-scheme specification.

The contract describes how the integration middleware (`CoreBanking.Middleware.SftpFileProcessor`
and `SettlementReconciliationService`) exchanges settlement files with a core/host system over
SFTP.

---

## 1. Channel and authentication

- Transport: **SFTP** (SSH file transfer). No FTP, no plaintext.
- **Host-key pinning.** The known host key of the remote is pinned; an unrecognised or changed
  host key aborts the session and raises an exception (defence against MITM / host substitution).
- **Auth.** Key-based (SSH key pair) preferred; credentials, if any, are never committed —
  they live in environment variables or a git-ignored override (per repo rule §10). The
  fixture does not embed any real key or secret.
- Directories (conceptual): `inbound/` (host → us), `outbound/` (us → host),
  `archive/` (processed), `error/` (quarantine).

---

## 2. Inbound settlement file layout (conceptual)

A single logical file is **Header + N Detail + Trailer**. Fields are conceptual, fixed
ordering, delimiter or fixed-width is an implementation choice.

### Header (one record)
| Field | Meaning |
|---|---|
| `RecordType` | `H` |
| `FileReference` | Unique sender file id (used for idempotency / replay detection) |
| `BusinessDate` | Settlement business date |
| `RecordCount` | Number of detail records declared |
| `SchemeRef` | Originator/scheme reference (awareness-only; not a vendor identifier) |

### Detail (one per claim/posting)
| Field | Meaning | Maps to |
|---|---|---|
| `RecordType` | `D` | — |
| `MandateReference` | Direct-debit mandate reference | `dbo.Mandate.Reference` |
| `Amount` | Settlement amount | `dbo.Posting.Amount` / `dbo.Claim.Amount` |
| `IdempotencyKey` | Per-item unique key | `dbo.Posting.IdempotencyKey` (`UNIQUE`) |
| `RejectionCode` | Present on a response/return item | `dbo.RejectionCode.Code` |

### Trailer (one record)
| Field | Meaning |
|---|---|
| `RecordType` | `T` |
| `RecordCount` | Must equal header `RecordCount` and actual detail count |
| `AmountTotal` | Control total of detail amounts |
| `Sha256` | Optional content hash for integrity (mirrors `dbo.FileArchive.Sha256`) |

**Validation:** detail count and control total must reconcile against the trailer; a mismatch
quarantines the file to `error/` and does **not** post any item.

---

## 3. Completion marker (`.done`)

A data file (`settle.txt`) is **only eligible for pickup once its sibling marker
`settle.txt.done` exists.** This avoids reading a half-written upload. The processor:

1. Sees `settle.txt.done`.
2. Reads `settle.txt`, validates header/trailer.
3. Registers it: `SftpFileProcessor.RegisterIncoming` → `dbo.SettlementFile (Status='Received')`.

A file without its `.done` marker is ignored until the next poll.

---

## 4. Idempotency, retry, and replay

- **Per-item idempotency.** Each detail carries an `IdempotencyKey`. Posting goes through
  `dbo.usp_PostTransaction`, which inserts only if that key is not already present, and
  `dbo.Posting.UQ_Posting_Idem` enforces uniqueness at the DB. **A replayed file therefore
  cannot double-post**, even if the whole file is re-delivered.
- **File-level replay detection.** On successful processing the file is archived via
  `SftpFileProcessor.ArchiveFile` → `dbo.FileArchive (FileName, ArchivedUtc, Sha256)`.
  A re-arriving file whose `Sha256` already exists in `dbo.FileArchive` is recognised as a
  replay and is reconciled, not re-posted.
- **Retry policy (advisory).** Transient SFTP/host errors are retried with backoff; the
  `.done` marker + idempotency key make retries safe (at-least-once delivery, exactly-once
  effect). Retry counts/backoff windows are deployment configuration, not in the fixture.
- **Archive-after-success.** A file is moved to `archive/` and recorded in `dbo.FileArchive`
  **only after** all items are processed (or quarantined deliberately). A crash mid-file leaves
  the file un-archived, so the next run re-picks it; idempotency keeps the re-run safe.

---

## 5. Response file processing & rejection mapping

The host returns a response/return file (returns, rejections). For each rejected item,
`ClaimResponseService.ProcessResponse`:

1. Stamps the claim — `UPDATE dbo.Claim SET RejectionCode = <code>`.
2. Looks up the human meaning — `SELECT ... FROM dbo.RejectionCode WHERE Code = <code>`.
3. Queues the exception — `INSERT INTO dbo.SuspenseQueue (ClaimId, Reason)`.

So a rejection never silently disappears: it lands in `dbo.SuspenseQueue` for operator review.
The set of valid codes lives in `dbo.RejectionCode`; changing or adding a code changes the
behaviour of `ProcessResponse` (see `expected-answers.md`, Q6).

---

## 6. Reconciliation

`SettlementReconciliationService.Reconcile` reads `dbo.SettlementFile (Status='Received')` and
the corresponding `dbo.Posting` rows to confirm that what was received was posted. Unmatched
items are reconciliation exceptions (operationally routed to suspense / manual review).

---

## 7. Audit & non-repudiation

- `dbo.FileArchive` is the durable, **append-only** record that a named file was processed at
  `ArchivedUtc` with content hash `Sha256`.
- Operator/approver identity for file release is captured as **append-only audit** (advisory;
  the fixture does not ship an approver table — stated honestly).

---

## 8. Honesty / limits

- Conceptual layout only; **no real payment-scheme format is reproduced**.
- Scheme conformance, message-standard compliance, and host certification are **out of scope**
  and explicitly **not claimed**.
- Regulatory references are **awareness-only**.
