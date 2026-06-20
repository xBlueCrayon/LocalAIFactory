# Test Questions — Meridian CRM Scenario

Questions to probe whether a model/agent genuinely understands this scenario. Each lists what a
**strong answer must contain**. The scenario is *inspired by* the relationship-management category;
strong answers must avoid claiming compatibility or equivalence with any commercial product.

## 1. Model the core entities

**Q:** Define the six core entities and their relationships.
**Strong answer:** Lists Account, Contact, Lead, Opportunity, Case, Activity with key fields;
Account 1—* Contact/Opportunity/Case; Activity polymorphically linked to one parent; notes
concurrency row versions on Lead and Opportunity.

## 2. Lead conversion

**Q:** What happens when a qualified Lead is converted?
**Strong answer:** Single transaction producing exactly one Contact and one Opportunity, both linked
to the resolved Account; idempotent on a submission token; concurrency-safe against double convert.

## 3. Weighted pipeline

**Q:** How is the weighted pipeline computed and reported?
**Strong answer:** Sum of estimated value × probability, grouped by stage/owner/region using
lightweight projections and separate counts — explicitly avoiding group-by-constant aggregation and
large-text materialization.

## 4. Record-level security

**Q:** A Sales Rep edits another rep's Opportunity by guessing its ID. What should happen?
**Strong answer:** Server-side ownership check rejects it (403/404); identifies this as an IDOR
guard; notes authorization lives in the service layer, deny-by-default, not just UI hiding.

## 5. Audit trail

**Q:** What must the audit log capture and what must it never do?
**Strong answer:** Append-only rows with actor, timestamp, entity, action, before/after for
sensitive fields on every create/update/delete/reassign; never updated or deleted by app code.

## 6. Account-360 performance

**Q:** The account-360 page must load fast. How do you ensure that?
**Strong answer:** Projection-based read models, separate `CountAsync` calls, indexed FKs, no large
text columns in the view query, no synchronous external calls on the request path.

## 7. Concurrency

**Q:** Two engagement managers edit the same Opportunity at once. What occurs?
**Strong answer:** Optimistic concurrency via row version — one save succeeds, the other receives a
concurrency exception to resolve; no silent last-write-wins data loss.

## 8. SLA and notifications

**Q:** How are case SLA alerts handled if the email relay is down?
**Strong answer:** Notifications go to a persisted queue with retry; requests never block on the
relay; alerts catch up when it recovers; consent flag respected before any outbound message.

## 9. Local-first constraints

**Q:** Which parts require an external service, and which run on MSSQL alone?
**Strong answer:** All core CRUD, security, audit, and reporting run on MSSQL only; embedding/RAG
and notifications are optional and degrade gracefully; nothing on the request path calls a service
synchronously.

## 10. Reporting cross-cut

**Q:** Leadership wants "key accounts at renewal that are also in trouble." How do you answer it?
**Strong answer:** A query joining accounts with an open renewal Opportunity and an open
severity-1/2 Case, returned as a lightweight projection; explains the indexes that keep it fast.

## 11. Rollback safety

**Q:** A release must be rolled back. What makes that safe here?
**Strong answer:** Additive, backward-compatible migrations; lead conversion is transactional;
notifications are idempotent; the append-only audit log stays valid across versions.

## 12. Scope discipline

**Q:** Is this a clone of a commercial CRM?
**Strong answer:** No — it is an original design inspired by the relationship-management category,
with no compatibility, equivalence, or certification claim, and no vendor text or trademarks reused.
