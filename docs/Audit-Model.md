# Audit Model

LocalAIFactory keeps two distinct lineage trails, both append-only:

1. **Operational audit** (`AuditEvent`) — who did what, when, to which project, and whether access
   was denied. This is the security/governance trail.
2. **Knowledge provenance** (`ProvenanceEvent`) — why/how a knowledge item came to be or changed.
   This is the explainability / knowledge-lineage trail.

They serve different questions and are never conflated. This document covers both, plus retention
guidance and the honest tamper-evidence gap.

---

## 1. Operational audit — `AuditEvent`

Defined in `src/LocalAIFactory.Core/Entities/Security.cs`; written by
`src/LocalAIFactory.Data/Security/AuditTrailService.cs` (`WriteAsync`). The record is **never
updated or deleted** in normal operation.

### Schema

| Field | Meaning |
|---|---|
| `Id` | surrogate key |
| `Uid` (Guid v7) | stable, time-ordered identifier (portable across instances) |
| `UserAccountId` (nullable) | the actor's app account, if resolved |
| `WindowsIdentity` (nullable) | `DOMAIN\user` captured at the time of the action |
| `EventType` | `AuditEventType` (see list below) |
| `Action` | short human-readable description (truncated to 200 chars) |
| `TargetType` (nullable) | kind of object acted on (truncated) |
| `TargetId` (nullable) | id of that object (truncated to 400 chars) |
| `ProjectId` (nullable) | which project the action touched |
| `Detail` (nullable) | extra context (e.g. the denied action string) |
| `IpAddress` (nullable) | caller IP at the time |
| `CreatedUtc` | UTC timestamp |

The write path captures the actor and IP from `ICurrentUserService` and is wrapped so that an audit
failure **never breaks the request** (`SecuredController.SafeAudit`). The trade-off: auditing is
best-effort on the write path, so an audit-store outage degrades to "request succeeds, event may be
missing" rather than "request fails".

### Audited event types

From `AuditEventType` (`Core/Enums/Enums.cs`), append-only (stored values are stable):

- `AuthSuccess` (0), `AuthDenied` (1)
- `ImportStarted` (2), `ImportCompleted` (3)
- `ProjectViewed` (4)
- `SymbolQueried` (5), `DependencyViewed` (6), `ImpactQueried` (7), `CoverageViewed` (8)
- `AccessGranted` (9), `AccessRevoked` (10), `RoleChanged` (11), `UserDisabled` (12)
- `ConsolidationStarted` (13), `ConsolidationCompleted` (14)
- `KnowledgePackInstalled` (15)

### Denials are audited

Every authorization failure is written as `AuthDenied` **before** the 403 is returned, with the
attempted action and (for project-scoped denials) the `ProjectId` in `Detail`. This means the trail
answers not only "what happened" but "what was attempted and refused" — the IDOR regression and
admin-gate denials all land here.

### What the trail can answer

- Who imported which repository, and when.
- Who was granted/revoked access to a project, by implication of `AccessGranted`/`AccessRevoked`.
- Which Knowledge Packs were installed, by whom.
- Every refused privileged action (`AuthDenied`), with actor, IP, and target.

---

## 2. Knowledge provenance — `ProvenanceEvent`

Defined in `src/LocalAIFactory.Core/Entities/ProvenanceEvent.cs`. Also **append-only — never
mutated or deleted**. This is lineage, not operational security audit.

Key fields:

| Field | Meaning |
|---|---|
| `Uid` / `KnowledgeItemUid` | the event and the item it explains |
| `SourceArtifactId` (nullable) | originating imported file, when known |
| `Method` | `ProvenanceMethod` — Deterministic / Semantic / Human / Import / Promotion / Consolidation / Autonomous |
| `ExtractorOrModelId` | which extractor or model produced it |
| `Actor`, `Reason` | who/why |
| `OriginInstanceId` (nullable) | the deployment that produced the event |
| `OriginPackUid` (nullable) | links an item that arrived from a Knowledge Pack |
| `CreatedUtc` | UTC timestamp |

Provenance underpins explainability and Knowledge-Pack origin tracking: an installed pack's items
carry `OriginPackUid`, and the installer rejects references to **unregistered** sources (see
`Compliance-Disclaimers.md` §source-registry governance). Operational audit records *that* a pack
was installed (`KnowledgePackInstalled`); provenance records *what knowledge* it contributed and
where each item came from.

---

## 3. Retention guidance

There is **no enforced retention policy in code** — this is operational guidance only:

- Treat both `AuditEvent` and `ProvenanceEvent` as **records of record**: retain for at least the
  pilot's full lifetime; do not prune during a pilot.
- If/when a retention window is adopted, archive (export then delete) rather than in-place delete,
  to preserve the append-only invariant in the live store.
- Back up the audit and provenance tables with the rest of the database; include them in the
  backup/restore drill (`Release-Checklist.md`).
- Any deletion or trimming of audit data is a privileged operational action and should itself be
  recorded out-of-band.

---

## 4. Honest gap: tamper-evidence

The audit trail is append-only **by application convention and database permissions**, not by
cryptography. It is **not hash-chained** and not cryptographically sealed. Consequences:

- A sufficiently privileged DB operator could alter or remove rows undetectably.
- There is no independent way to prove the log has not been edited.

**Proof required to close:** introduce a per-row hash chain (each event hashes the prior event's
hash + its own content) plus a periodic anchor, and a verifier that detects any break. Until that
exists, do not describe the audit as tamper-evident.
