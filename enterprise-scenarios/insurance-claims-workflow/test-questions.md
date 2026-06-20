# Test Questions — Capability Probe

> Fictional scenario (Zephyr Mutual Assurance). Awareness-only on FSC/Mauritius.
> Not legal or insurance advice; no compliance or certification claim.

These questions probe whether a model (or contributor) understands the scenario and can
reason about a correct solution. Each lists what a **strong answer must contain**.

## 1. How do you guarantee separation of duties on settlement?

Strong answer: a server-side guard evaluated before commit that compares the raising actor,
approving actor, and releasing actor and rejects when any two coincide; enforced in a service,
not the UI; covered by a unit test for the self-approval case.

## 2. Why are reserves append-only rather than updated in place?

Strong answer: to preserve a defensible, ordered change history; corrections are made by a new
compensating row with a reason code, never by deletion; this also supports reserve-movement
reporting and audit reconstruction.

## 3. The policy feed is down. What happens to claim registration?

Strong answer: registration falls back to manual policy entry, the claim is flagged unverified,
no page blocks on the external call, and the feed sits behind an interface with a manual fallback.

## 4. Where and how is the audit trail written?

Strong answer: an append-only AuditEvent row written in the **same transaction** as the state
change it records, capturing actor, role, action, before/after, timestamp, and reason; never
editable or deletable through the app.

## 5. How are authority thresholds enforced and changed?

Strong answer: thresholds map claim value bands to required authority; evaluated server-side
before approval commits; stored as configuration so they change without a migration or redeploy.

## 6. How do you prevent two handlers overwriting the same reserve?

Strong answer: optimistic concurrency (row version / concurrency token); a stale write is
rejected and surfaced to the user rather than silently overwriting.

## 7. What does the platform do about KYC/AML?

Strong answer: it captures claimant identity by reference and flags beneficiary changes for
review as operational **awareness** only; it explicitly is **not** a sanctions-screening, AML
decisioning, or KYC-verification product, and makes no regulatory claim.

## 8. A payment file is rejected by finance. How is that recovered?

Strong answer: the instruction enters a recoverable failed state and can be re-run without
producing a double payment; the platform never moves funds itself.

## 9. What are the legal vs illegal claim state transitions, and how are illegal ones handled?

Strong answer: an explicit state machine defines allowed transitions (e.g. cannot settle before
assessment and reserve); illegal transitions raise a typed exception and are logged, not silently
ignored; covered by state-machine tests.

## 10. How does this run in MSSQL-only mode?

Strong answer: MSSQL is the system of record; all integrations are optional behind interfaces
with manual/no-op fallbacks; no internet, GPU, or external AI is required to render core pages;
health is read from cache, never probed synchronously on the request path.

## 11. How is FSC/Mauritius context handled in the design?

Strong answer: as awareness-only context that an insurer operates under a regulator; the design
asserts **no** conformance or certification and surfaces no compliance claim in UI or documents.

## 12. What rollback story does a risky change have?

Strong answer: additive schema only (prior build still reads data); append-only reserves and
audit mean bad entries are compensated, not deleted; new transition rules sit behind a feature
flag so they can be disabled without a migration.
