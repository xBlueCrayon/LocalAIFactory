# Test Questions — Reconcile

> **Synthetic exercise.** Original fictional scenario. Mauritius framing is awareness-only. Not legal,
> regulatory, or compliance advice; no certification claimed.

Twelve questions to probe whether a solution genuinely understands this scenario. Each lists what a
**strong** answer must contain.

---

**1. How do you guarantee a re-sent settlement file is not processed twice?**
Strong answer: register each file by content hash **and** business key + business date; reject on
duplicate before parsing; show the file register proves exactly-once; note idempotency is at the
file level *and* the posting level.

**2. What makes the reconciliation engine deterministic and re-runnable?**
Strong answer: pure key-based matching over stored claims and postings, no hidden state or wall-clock
dependence; identical inputs yield identical Matched/Break/Unmatched outcomes; safe to re-run after
rollback.

**3. A file declares 1,000 records but contains 998. What happens?**
Strong answer: control-count (and control-total) validation fails; the **whole** file is rejected
atomically; zero claims persisted; the event is audited and alerted — never process the partial file.

**4. How is a double-posting prevented when a correction is submitted twice?**
Strong answer: each adjustment carries an idempotency key honoured by the core-banking interface; the
second submission is a no-op; tested explicitly.

**5. Walk through the maker/checker/approver flow for a £40,000 correction above a £10,000
threshold.**
Strong answer: maker proposes; checker (different identity) reviews; because it exceeds threshold a
distinct approver authorises; every transition is server-validated and audited; maker cannot self-
approve or self-check.

**6. How do you enforce segregation of duties so it cannot be bypassed from the UI?**
Strong answer: SoD enforced **server-side** on each transition, comparing actor identity against the
item's prior actors; client is never trusted; forged transitions rejected.

**7. Why is the audit trail append-only and how is that enforced in code?**
Strong answer: insert-only table; no update/delete code path exists; records carry actor, action,
entity, before/after summary, timestamp, correlation id; rollbacks are themselves audited.

**8. The outbound response file will miss its cut-off. What should the system do?**
Strong answer: detect the cut-off window, alert rather than silently skip; do not emit a malformed or
late-but-unflagged file; deterministic generation means the file can be produced reproducibly once
unblocked.

**9. Map the data entities to an MSSQL + EF Core model.**
Strong answer: Mandate, Claim, SettlementFile, Posting, Exception, ResolutionAction, AuditRecord with
sensible keys/relationships; additive migration shape; lightweight list-query records that avoid
materialising large file-body text columns.

**10. How does the platform keep working when SFTP or core banking is down?**
Strong answer: MSSQL is the single source of truth; ingestion/posting pause and retry; triage and
reporting still render from stored state; no blocking external call on the request path.

**11. How do you handle an out-of-sequence settlement file?**
Strong answer: detect sequence gap or regression against the file register; hold for review instead
of processing; audit the hold; resume in order once resolved.

**12. What is your rollback story, and why does it not corrupt the ledger?**
Strong answer: additive/backward-compatible schema; stored state preserved; because posting is
idempotent and matching re-runnable, re-processing after rollback reproduces identical outcomes; the
rollback is an audited event.

---

**Awareness-only note.** Any mention of the Mauritius context in answers is background colour, not a
compliance statement. None of the above is legal, regulatory, or compliance advice.
