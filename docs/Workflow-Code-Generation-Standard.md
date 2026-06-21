# Workflow Code-Generation Standard

This is the standard that AI- or human-generated workflow code in LocalAIFactory must follow when
implementing any family from the Enterprise Workflow Pattern Library. It is original practical
guidance, not a regulatory control attestation. The reference fixtures live in
`benchmarks/fixtures/enterprise-workflows/` and are synthetic prototypes, not a deployed engine.

The governing rule: **every state change is authorized, validated, transactional, and audited.**

---

## 1. Entities

- Model the business entity separately from the workflow. The workflow attaches to it via
  `WorkflowInstance(BusinessEntityType, BusinessEntityId)` — do not bolt workflow columns onto the
  business table.
- Entities expose a single current-state column plus a `RowVersion`/concurrency token. History lives
  in `WorkflowTransition` and `WorkflowAuditEvent`, never overwritten in place.
- Keep large text/blob columns out of list projections (consistent with the repository's query rules).

## 2. Enums and states

- Represent states and decisions as **strong enums**, not free-text magic strings scattered across the
  code. Map enum <-> stored value in one place.
- Define `initial`, `allowed`, and `terminal` states explicitly. A terminal state has no outgoing
  transitions.

## 3. State machine

- Encode allowed transitions in one authoritative place (a transition table or guarded map). The
  service asks "is `from -> to` allowed for this actor?" before acting — no ad-hoc `if` ladders in
  controllers.
- Reject any transition that is not in the allowed set; never let an unknown transition fall through
  silently.

## 4. Service layer

- All business rules and transitions live in services, never in controllers. The service validates,
  checks authorization, opens a transaction, records the transition + audit event, and commits.
- Use the `usp_RecordTransition`-style pattern (transition row + state update + audit event in one
  transaction). See `enterprise-workflow-services.cs`.

## 5. Controllers

- Controllers translate HTTP to a service call and back. **No direct DB writes from a controller.**
  No business decision, no state mutation, no `ExecuteSqlRaw` of an `UPDATE`/`INSERT` in an action.

## 6. UI

- The UI shows current state, allowed actions for the current user's role, audit history, and any open
  exceptions. Disable actions the user is not authorized for (defence in depth, not the only check).
- Never render a workflow page by calling external services synchronously (read from a cached snapshot).

## 7. Validation

- Validate mandatory fields, value limits, distinct-actor rules, and idempotency **server-side**.
  Client validation is a convenience, never the control.
- A rejection must always capture a non-empty reason.

## 8. Authorization and ACL

- Check both **role** (can this role perform this transition?) and **scope/ACL** (is this actor
  allowed on this specific instance / cost centre / amount band?).
- Resolve approvers from an authority matrix or policy table — never hardcode approver identities.
- Enforce segregation of duties (maker != checker != approver) in the service, not just the UI.

## 9. Audit

- Every meaningful action writes one immutable `WorkflowAuditEvent` (actor, timestamp, from-state,
  to-state, reason, correlation id). Audit rows are append-only — never updated or deleted.

## 10. Notification

- Notifications are queued rows processed out-of-band; a notification failure must not roll back or
  block the state change. Record queued/sent state.

## 11. Exception handling

- Unhappy paths route to a typed exception/queue row with severity and ownership — never a swallowed
  catch. Surface and assign; do not silently dismiss.

## 12. Migration

- Schema changes are **additive and backward-compatible** by default. No destructive change without
  explicit approval. Regenerate the EF model snapshot through tooling; never hand-edit it inconsistently.

## 13. Seed

- Seed workflow definitions, roles, SLA rules, and policies as data — not as hardcoded constants in
  code. Thresholds and limits come from `WorkflowPolicy`, so they can change without a redeploy.

## 14. Tests

- Every control has a test: distinct-actor enforcement, threshold routing, mandatory rejection reason,
  audit-event-per-transition, idempotency, terminal-state guards, SLA breach/escalation. See
  `enterprise-workflow-tests.cs` for the behaviour sketch.

## 15. Security

- Parameterize all SQL. Validate and bound uploads (type, size, checksum). Encrypt secrets at rest.
  Apply least privilege. Treat the audit trail and evidence export as tamper-evident (hash retained).

## 16. Rollback

- Any production-affecting action (release, restore, schema change) carries a rollback plan and a
  reversal path; a failed action transitions to a recorded `RolledBack`/`Failed` state, never an
  ambiguous limbo.

---

## Generated code MUST AVOID

1. Hardcoded user IDs or approver identities (resolve from a matrix/policy).
2. Hidden or implicit approvals (no auto-approve to "save a step").
3. Unaudited state changes (every move writes an audit event).
4. Direct DB updates from a controller (go through a service).
5. Missing authorization checks (role + scope, server-side).
6. Missing ACL/scope checks (instance/amount/cost-centre level).
7. Weak enums / magic strings for states and decisions.
8. Silent failure (swallowed exceptions, ignored unknown codes).
9. Missing transactions around multi-row state changes.
10. Missing concurrency control (no `RowVersion`/optimistic token).
11. Missing rejection reason on a reject/return.
12. Missing audit event on a meaningful action.
13. Unbounded file uploads (no type/size/integrity check).
14. Missing evidence export / tamper-evidence (no approval, no hash).
15. Missing tests for the controls above.

---

## Correct example (segregation of duties + audited transition)

```csharp
// Service: distinct-actor check, then an audited transition in one transaction.
public void Check(int instanceId, int checkerActorId, decimal amount)
{
    // The stored proc THROWs if checker == maker (50001) and writes a WorkflowAuditEvent.
    _db.Database.ExecuteSqlRaw(
        "EXEC dbo.usp_CheckerVerify @InstanceId = {0}, @CheckerActorId = {1}, @Amount = {2}",
        instanceId, checkerActorId, amount);
}
```

```sql
-- usp_CheckerVerify (excerpt): self-check is rejected at the control boundary; nothing commits.
IF @MakerActorId = @CheckerActorId
    THROW 50001, 'Segregation of duties violation: checker must differ from maker.', 1;
-- ... else usp_RecordTransition writes the transition AND the immutable audit event atomically.
```

## Anti-pattern (do NOT do this)

```csharp
// BAD: controller mutates state directly, no service, no authz, no distinct-actor check,
// no transaction, no audit event, and a hardcoded approver id.
[HttpPost]
public IActionResult Approve(int id)
{
    _db.Database.ExecuteSqlRaw(
        "UPDATE dbo.WorkflowInstance SET CurrentState = 'Approved' WHERE Id = {0}", id);
    // approver hardcoded, self-approval possible, nothing audited -> fails the standard on 6+ counts.
    return Ok();
}
```
