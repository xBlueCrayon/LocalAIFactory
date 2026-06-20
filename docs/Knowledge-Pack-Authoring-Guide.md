# Knowledge Pack Authoring Guide

> **Status:** Implemented reference (the install path is live for the Professional Base Knowledge Pack
> v1/v1.2). This guide tells an author how to build a pack that installs cleanly and idempotently into
> MSSQL.
> **Authority:** subordinate to `MASTER_VISION.md`. See also
> `docs/Professional-Base-Knowledge-Pack-v1.md` and `docs/Knowledge-Separation-and-Retrieval-Rules.md`.

## 1. What a pack is (and is not)

A Knowledge Pack is a **portable JSON seed** for knowledge. Installing it creates ordinary
`KnowledgeItem` rows in MSSQL; **MSSQL is then authoritative** and the pack files are not consulted at
query time. A pack is therefore an *import/export representation*, never a runtime store
(MASTER_VISION §5).

A pack is **not**:

- a parallel knowledge store (each item becomes a normal `KnowledgeItem`);
- a way to bypass governance (pack items are `Tier = Curated` and protected by `IPermanenceGuard`);
- authoritative once edited by a human (a human edit makes the item human-anchored; later pack changes
  are *proposed*, not written through).

## 2. Folder structure

```
knowledge-packs/<pack-id>/
  manifest.json
  <category-1>.json
  <category-2>.json
  ...
  source-registry.json        (optional, v1.2+)
```

The folder is copied next to the published binaries (csproj `<Content>` → output) so a deployed app can
install it. `KnowledgePackLocator` resolves the directory across `dotnet run` (walks up to the repo root)
and published layouts (`AppContext.BaseDirectory`), with a `KnowledgePacks:<...>Path` config override.

## 3. `manifest.json`

Required fields:

```json
{
  "packUid": "<stable GUID for the pack>",
  "name": "My Domain Pack",
  "version": "1.0.0",
  "description": "What this pack covers.",
  "license": "Original summaries; no proprietary verbatim text.",
  "createdUtc": "2026-06-21",
  "lastReviewedUtc": "2026-06-21",
  "itemCount": 42,
  "files": ["category-1.json", "category-2.json"],
  "legalLimitations": "General domain concepts; not legal/compliance/financial advice.",
  "sourcePolicy": "Original summaries only; no verbatim standard text.",
  "reviewStatus": "Approved"
}
```

- `packUid` must be a **stable GUID** — re-issuing the same pack keeps the same `packUid` so upgrades are
  recognised.
- `itemCount` and `files[]` must match the category files actually shipped.
- `version` drives the upgrade story; bump it when content changes.

## 4. Category files

Each is `{ "category": "...", "items": [ … ] }`. Every item:

```json
{
  "uid": "<stable GUID, unique within the pack>",
  "category": "Security / Access Control",
  "title": "Deny-by-default",
  "knowledgeType": "Standard",
  "scope": "Standards",
  "description": "…",
  "applicability": "…",
  "example": "…",
  "limitation": "…",
  "confidence": 0.9,
  "sourceType": "ArchitectureNote",
  "version": "1.0.0",
  "lastReviewedUtc": "2026-06-21",
  "reviewStatus": "Approved",
  "tags": ["rbac", "least-privilege"],
  "sources": ["<registry id>"],          // optional, v1.2+
  "jurisdiction": "Mauritius"            // optional, v1.2+
}
```

The stored `Content` is rendered markdown: the `description` followed by **Applicability**, **Example**,
**Limitation** (and, in v1.2, **Sources**) sections.

## 5. Required fields and validation

The installer **validates the whole pack in memory before touching any database row**. A malformed pack
can never partially corrupt the store. Validation checks:

- **`uid`** present, a **well-formed GUID**, and **unique within the pack**. This is the upsert key —
  duplicate or malformed `uid`s fail the whole install.
- **Required fields** present: `category`, `title`, `description`, `knowledgeType`, `scope`, `sourceType`.
- **`confidence` ∈ [0, 1]**.
- **Enum values** parse to known members (see §6).
- **`sources`** (if present) each resolve to an entry in `source-registry.json` (see §7).

If anything fails, the installer returns the **full error list** and writes nothing.

## 6. The enums (must match exactly)

Author values are parsed to these Core enums. Use the exact member names:

- **`knowledgeType`** → `KnowledgeType`: `Unspecified`, `CodeSymbol`, `DataDictionary`, `BusinessRule`,
  `Requirement`, `Standard`, `Regulation`, `ArchitectureDecision`, `FixPattern`, `ConversationInsight`,
  `GlossaryTerm`, `Other`.
- **`scope`** → `KnowledgeScope`: `Unspecified`, `Global`, `Project`, `Standards`, `Regulatory`, `Team`.
- **`sourceType`** → `SourceType`: e.g. `UserExplanation`, `SourceCode`, `SqlScript`, `Documentation`,
  `Readme`, `ArchitectureNote`, `BusinessRule`, `DatabaseObject`, `DebuggingFix`, `Configuration`
  (chat-derived types `ChatGptExport`/`ClaudeExport`/`ConversationTranscript` exist but are normally
  produced by chat import, not authored into a pack).
- **`reviewStatus`** → `KnowledgeStatus`: `Draft`, `NeedsReview`, `Approved`, `Deprecated`.

Pack items are typically authored as `Approved` and installed at `Tier = Curated`.

## 7. Optional source registry (v1.2+)

`source-registry.json` is a governance list of the sources a pack's items may cite, each with an id,
title, type, and (optionally) a reliability/freshness note. Item `sources[]` must reference these ids.
This keeps citations **honest and auditable** — an item cannot cite a source that is not declared, and
the platform never fabricates citations. See `docs/Professional-Base-Knowledge-Pack-v1.2-Sources.md`.

## 8. Install behaviour (idempotent, propose-not-overwrite)

`IKnowledgePackInstaller` (Rag layer, MSSQL-only):

1. **Validate in memory first** (§5) — no partial corruption.
2. **Fast idempotency** — a `SourceManifestHash` (SHA-256 over manifest + all files) lets an unchanged
   re-install return `AlreadyCurrent` instantly.
3. **Transactional writes** (relational provider). Upsert the `KnowledgePack` anchor, then per item keyed
   on `Uid`:
   - **New** → create `KnowledgeItem` (`Tier = Curated`, `KnowledgePackId` stamped), write a v1
     `KnowledgeVersion`, write an `Import` `ProvenanceEvent` with `OriginPackUid`, sync tags, chunk +
     index.
   - **Unchanged** (content hash matches) → no-op; refresh pack link + review date.
   - **Changed, unedited** → update in place + new `KnowledgeVersion` + provenance.
   - **Changed, user-edited** → **do not overwrite** — raise a `ProposedRevision` via `IPermanenceGuard`.

The "user-edited" signal is reliable: a human edit/approve always writes a `ProvenanceMethod.Human`
event. If one exists for an item, an incoming pack change is routed to a proposed revision instead of
overwriting the human's baseline.

## 9. Security & audit

- Viewing Base Knowledge follows the existing posture (authenticated users).
- **Installing/updating a pack is Admin-only**, enforced server-side via
  `SecuredController.RequireAdminAsync` (hiding the button is never sufficient), and **audited**
  (`AuditEventType.KnowledgePackInstalled`).
- Startup auto-install runs as `system (startup)`, is idempotent, and never blocks startup on failure.

## 10. Authoring checklist

- [ ] `manifest.json` has a stable `packUid`, accurate `itemCount`/`files[]`, and licence/limitations.
- [ ] Every item has a **stable, unique, well-formed GUID `uid`**.
- [ ] All required fields present; `confidence ∈ [0,1]`; enums use exact member names.
- [ ] No verbatim proprietary/paid standard text — **original summaries only**; each domain item states
      its `limitation`.
- [ ] If citing sources, every `sources[]` id exists in `source-registry.json`.
- [ ] Re-install is idempotent (unchanged ⇒ `AlreadyCurrent`); user-edited items are preserved as
      proposed revisions.
- [ ] Install via the Base Knowledge **Install / Update** action (Admin) or a wired startup install.

## 11. Legal / source limitations

- **Original summaries only.** No verbatim ISO/IEC, PMBOK, or other proprietary/paid text. No claim of
  certification or compliance.
- Governance/control items are **practical summaries**, not substitutes for the licensed standards or for
  legal/compliance advice.
- Finance/banking items are **general domain concepts**, not jurisdiction-specific advice.
- The `manifest.json` records `license`, `legalLimitations`, and `sourcePolicy`; these limitations are
  the contract for what a pack may and may not assert.

## 12. Export (roadmap)

Pack **export** (the reverse of install) is not implemented yet. The schema (`KnowledgeItem.Uid`,
`ProvenanceEvent.OriginPackUid`) is already export-ready, so round-tripping curated knowledge back into a
pack is a planned, additive capability — not a migration risk.
