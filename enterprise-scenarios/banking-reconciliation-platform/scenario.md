# Scenario: Direct-Debit Settlement Reconciliation Platform

> **Synthetic exercise.** This is an original, fictional enterprise scenario authored for
> LocalAIFactory's enterprise capability simulation suite. It does **not** describe, clone, or
> imitate any real product, switch, scheme, or institution. References to a Mauritius context are
> **awareness-only** background colour and carry **no** regulatory, legal, or compliance meaning.
> Nothing here is legal, regulatory, or compliance advice, and no certification or conformance is
> claimed.

**Fictional institution:** *Banque Synthèse Ltée* ("the Bank"), a mid-sized retail bank.
**System under design:** *Reconcile* — an internal direct-debit settlement reconciliation platform.

---

## Business Problem

The Bank originates and collects retail direct debits (utility bills, loan instalments, insurance
premiums) on behalf of biller customers. Each business day it exchanges settlement files with an
external clearing counterparty and must reconcile those files against the postings made in its own
core-banking ledger.

Today reconciliation is performed in spreadsheets by an operations team. The process is manual,
slow, and error-prone:

- Breaks (mismatches between the cleared amount and the posted amount) are found late, sometimes
  the next day, after customer accounts have already been debited or credited.
- Duplicate processing of a re-sent settlement file has caused double debits that had to be
  reversed by hand.
- There is no durable, tamper-evident record of *who* approved a correction and *when*.
- Month-end reporting is assembled by copy-paste, so the numbers are hard to trust and impossible
  to audit retrospectively.

The Bank wants a controlled, idempotent, auditable platform that ingests settlement files,
matches them to core-banking postings, surfaces exceptions early, and routes corrections through a
maker/checker/approver control with an append-only audit trail.

---

## Current-State Process

1. The clearing counterparty drops a daily settlement file onto an SFTP endpoint.
2. An analyst downloads it, opens it in a spreadsheet, and eyeballs it against a core-banking
   posting export pulled separately.
3. Matches are ticked off manually; suspected breaks are emailed to a supervisor.
4. Corrections are keyed directly into the core-banking back office with no second-person check.
5. A response/acknowledgement file is hand-built and uploaded back to the counterparty, sometimes
   late, occasionally with transposition errors.
6. Reports are compiled at month-end from the surviving spreadsheets.

Pain points: no idempotency, no segregation of duties, no audit trail, cut-off times missed, and
no single source of truth for the day's reconciliation state.

---

## Target-State Process

1. Settlement files land on SFTP and are pulled by a scheduled ingestion worker.
2. Each file is registered exactly once (content hash + business key), parsed, and validated.
3. The reconciliation engine matches each settlement *claim* to a core-banking *posting* using
   deterministic keys, producing **Matched**, **Break**, or **Unmatched** outcomes.
4. Exceptions are queued for an operator (the **maker**) who proposes a resolution.
5. A second person (the **checker**) reviews; an **approver** authorises anything above a threshold.
6. Approved corrections produce idempotent posting adjustments; everything is written to an
   append-only audit log.
7. A response file is generated deterministically and queued for SFTP delivery before cut-off.
8. Reporting is live and reproducible from the stored state.

---

## Users and Roles

- **Operations Analyst (Maker):** triages exceptions, proposes resolutions, cannot self-approve.
- **Operations Supervisor (Checker):** reviews proposed resolutions, accepts or rejects.
- **Settlement Approver:** authorises corrections above a configurable monetary threshold.
- **Reconciliation Administrator:** manages matching rules, cut-off windows, counterparties.
- **Auditor (read-only):** reads the append-only audit trail and reports; can change nothing.
- **System (service account):** runs ingestion, matching, and file generation unattended.

A single human identity may **not** simultaneously hold maker and checker on the same item
(segregation of duties enforced server-side).

---

## Data Entities

- **Mandate** — the standing authority for a biller to collect from a payer account
  (mandate reference, payer account, biller, status, validity dates).
- **Claim** — one collection instruction inside a settlement file (claim id, mandate reference,
  amount, currency, value date, reason code).
- **SettlementFile** — an exchanged file (file id, direction in/out, content hash, business date,
  counterparty, sequence number, received/processed timestamps, status).
- **Posting** — a core-banking ledger movement (posting id, account, amount, direction, value date,
  source reference, idempotency key).
- **Exception (Break)** — a reconciliation discrepancy (exception id, claim ref, posting ref,
  break type, expected vs actual amount, state, assignee).
- **ResolutionAction** — a maker/checker/approver decision attached to an exception.
- **AuditRecord** — append-only event (actor, action, entity, before/after summary, timestamp,
  correlation id).

---

## Integrations

- **SFTP file exchange:** inbound settlement files pulled on a schedule; outbound response files
  delivered before cut-off. File transport is treated as untrusted: validate structure, hash, and
  sequence before acting.
- **Response files:** deterministic acknowledgement/return files generated from stored state so the
  same input always yields the same output.
- **Core banking:** read postings for matching; write idempotent adjustment postings on approved
  corrections. The core-banking interface is assumed to honour an idempotency key so retries do not
  double-post.

All integration boundaries are designed to degrade safely: if SFTP or core banking is unavailable,
ingestion and posting pause and retry; the reconciliation UI and stored state remain readable.

---

## Security and Audit Controls

- **Maker / Checker / Approver:** every correction passes through propose → review → (conditional)
  authorise. State transitions are validated server-side, never trusted from the client.
- **Segregation of duties:** the maker of an item cannot be its checker; the approver is distinct
  again for above-threshold amounts.
- **Append-only audit:** audit records are insert-only; no update or delete path exists in code.
  Each record carries actor, action, target entity, a before/after summary, and a correlation id.
- **Authorisation by role and project/counterparty scope**, enforced on the server for every action.
- **No secrets in source:** SFTP credentials and connection strings come from environment or
  protected configuration, never committed.

---

## Reporting Requirements

- **Daily reconciliation summary:** counts and amounts by outcome (Matched / Break / Unmatched).
- **Open exceptions ageing:** breaks by age bucket and by assignee.
- **Maker/checker activity:** who proposed, reviewed, approved, with timestamps.
- **File register:** every settlement file received/sent, with hash and sequence, proving none
  were skipped or processed twice.
- **Reproducibility:** any historical report can be regenerated from stored state.

---

## Failure Modes

- **Duplicate file:** same content hash or business key re-sent; must be detected and ignored
  without re-posting.
- **Cut-off miss:** outbound response not ready before the cut-off window; must alert, not silently
  skip.
- **Partial / truncated file:** declared record count or control total does not match contents;
  reject the whole file, do not process half of it.
- **Reconciliation break:** amount or status mismatch between claim and posting; routed to
  exception workflow.
- **Out-of-sequence file:** sequence number gap or regression; held for review.
- **Replayed posting:** the same correction submitted twice; idempotency key prevents a second
  ledger movement.

---

## Acceptance Criteria

(See `acceptance-criteria.md` for the measurable checklist.) At a high level the platform is
accepted when: files are ingested exactly once; matching produces deterministic outcomes;
corrections cannot bypass maker/checker/approver; the audit trail is append-only and complete; and
core pages render from stored state without depending on SFTP or core banking being up.

---

## Expected Architecture

- **ASP.NET Core MVC** front end for triage, review, approval, and reporting.
- **MSSQL + EF Core** as the primary store and single source of truth; the platform must remain
  usable for reading and triage even when external services are down.
- **Ingestion worker** (hosted background service) that pulls files, registers them by content hash
  + business key, parses, and validates control totals before any matching.
- **Reconciliation engine:** deterministic key-based matching producing Matched / Break / Unmatched;
  pure and re-runnable against stored claims and postings.
- **Idempotent posting:** every adjustment carries an idempotency key; a replay is a no-op.
- **Append-only audit:** insert-only audit table with no update/delete code path.
- **Response-file generator:** deterministic output from stored state, queued for SFTP delivery.

Design constraints mirror the host platform: avoid blocking external calls on the request path,
prefer simple reliable EF queries, and never materialise large file-body columns in list views.

---

## Expected Tests

- File registration is idempotent: re-ingesting the same file does not create a second record or a
  second set of claims.
- Matching is deterministic: identical inputs yield identical outcomes across runs.
- Control-total validation rejects truncated/partial files atomically.
- Maker cannot check own item; below/above-threshold approval routing is enforced.
- Idempotency key prevents double-posting on replayed corrections.
- Audit records are written for every state transition and are never mutated.
- Out-of-sequence and duplicate files are detected and held/ignored as specified.

---

## Expected Deployment Concerns

- SFTP credentials and core-banking connection details supplied via environment / protected config.
- Cut-off windows and matching rules configurable without redeploy.
- Background ingestion must be safe to run on a single node first; document any assumptions before
  scaling out (avoid double-pull of the same file).
- Time zone and value-date handling pinned explicitly to avoid off-by-one-day breaks.

---

## Rollback Considerations

- Schema changes are additive and backward compatible where possible.
- A failed deployment can revert to the prior version while stored state (files, claims, postings,
  audit) remains intact and re-processable.
- Because posting is idempotent and matching is re-runnable, a rollback does not corrupt the ledger;
  re-running ingestion/matching after rollback reproduces the same outcomes.
- The append-only audit means rollbacks are themselves auditable events.

---

## CEO/CTO Summary

Reconcile replaces a fragile spreadsheet process with a controlled, auditable settlement
reconciliation platform. It ingests each settlement file exactly once, matches it deterministically
to the core-banking ledger, and catches breaks early — before customers feel them. Every correction
is double-checked and, above a threshold, separately authorised, with a tamper-evident audit trail.
Built on ASP.NET Core MVC and MSSQL with idempotent posting, it keeps working for triage and
reporting even when external systems are down, and it makes month-end reporting reproducible instead
of hand-assembled. The result is fewer double debits, faster break resolution, and a defensible,
self-evidencing record of who did what and when.
