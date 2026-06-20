# Test Questions — Capability Probes

Each question probes whether an analysis/implementation correctly understood the scenario.
A strong answer must contain the listed elements. Weak answers omit boundaries, audit, or
idempotency, or claim compatibility with a real vendor product.

## 1. Module boundaries

**Q:** How should the leasing module record a posted invoice in the general ledger?
**Strong answer must contain:** it must NOT write ledger rows itself; it creates a draft
invoice and calls a published Accounting service contract (`IAccountingPosting`); posting
is the Accounting module's responsibility; no cross-schema foreign keys.

## 2. Proration

**Q:** A monthly lease starts on the 17th. How is the first invoice computed?
**Strong answer must contain:** proration by remaining days in the period; a deterministic,
unit-tested day-count rule; explicit handling of month length and leap years.

## 3. Idempotency

**Q:** The scheduling worker reruns after a crash. How do you prevent double billing?
**Strong answer must contain:** an idempotency key per billing period; regeneration is a
no-op for already-generated periods; the worker is safe to restart mid-run.

## 4. Authorization

**Q:** A Leasing Agent tries to approve an early-termination fee write-off. What happens?
**Strong answer must contain:** denied; finance-sensitive actions require Finance Approver;
deny-by-default; enforcement is server-side, not UI-only.

## 5. Audit

**Q:** What is recorded when an asset moves from "On Lease" to "Returned"?
**Strong answer must contain:** one append-only `AssetStateTransition` and/or `AuditEvent`
with from-state, to-state, timestamp, actor, and reason; entries are never edited in place.

## 6. Accounting unavailable

**Q:** The Accounting module is down when an invoice is due to post. What is the behavior?
**Strong answer must contain:** the invoice stays in a draft/pending state; posting is
retried; nothing is silently dropped; no partial ledger write.

## 7. Asset lifecycle integrity

**Q:** A lease is cancelled while its asset is "On Lease". How is the asset state corrected?
**Strong answer must contain:** a compensating transition back to an available/return state
with a recorded reason; illegal transitions are rejected by the state machine.

## 8. Data ownership

**Q:** Where does customer master data live, and what does the leasing module store?
**Strong answer must contain:** customer master lives in CRM/Customer module, referenced by
stable ID; the leasing module stores only lease-specific data, not duplicated master data.

## 9. Concurrency

**Q:** Two agents edit the same lease agreement simultaneously. What prevents corruption?
**Strong answer must contain:** optimistic concurrency (row version / concurrency token);
the second save is rejected and must reload; no last-writer-wins data loss.

## 10. Runtime constraint

**Q:** Can the module render its pages with only SQL Server present?
**Strong answer must contain:** yes; MSSQL is the primary store; no blocking external call
on the request path; external integrations degrade gracefully or run in a background worker.

## 11. Deployment & rollback

**Q:** How is the module installed and later disabled safely?
**Strong answer must contain:** additive, backward-compatible migrations run at install;
feature-flag per environment; disabling is non-destructive (data retained, endpoints gated);
destructive changes require explicit approval.

## 12. Honesty / scope

**Q:** Is this module compatible with or certified for a specific commercial ERP?
**Strong answer must contain:** no; it is inspired-by modular ERP design only; it makes no
compatibility, equivalence, or certification claims and reproduces no vendor text.
