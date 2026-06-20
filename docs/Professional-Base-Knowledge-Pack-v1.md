# Professional Base Knowledge Pack v1 (R2-ACC-B1)

## Purpose

LocalAIFactory should not only understand *imported* repositories — it should also ship with
**professional baseline knowledge** that helps it reason about common enterprise systems: C#/ASP.NET
Core MVC, EF Core/MSSQL, Python/AI/RAG, CRUD-over-MSSQL patterns, security & access control, governance
(PMBOK-style), control-style standards (ISO-style), finance/accounting/banking, operations/sysadmin,
product/admin patterns, and reporting.

This baseline is **local, deployable, searchable, versioned, installed into MSSQL, visible in the UI,
legally safe, distinguishable from imported project knowledge, and protected from silent overwrite**. It
is shipped as a portable **Knowledge Pack** so future packs (and pack export/import) reuse the same path.

## Design

Baseline knowledge is **not** a parallel store. Each pack item becomes an ordinary `KnowledgeItem`,
so it flows through the existing search, approval, permanence, versioning, provenance, and (future)
Knowledge Pack export with **zero new mechanisms**. The only additions are an install anchor and an
origin marker:

| Concern | Mechanism (reused unless noted) |
|---|---|
| Portable identity | `KnowledgeItem.Uid` (set from the pack item's stable `uid`) |
| Baseline vs imported | **`KnowledgeItem.KnowledgePackId`** (new nullable FK) — non-null ⇒ baseline |
| Global (not project) | `ProjectId = null` |
| Protected from automated overwrite | `Tier = PermanenceTier.Curated` + `IPermanenceGuard` |
| Lineage / pack origin | `ProvenanceEvent` with `Method = Import` and `OriginPackUid` (already existed) |
| Version history | `KnowledgeVersion` (hash-guarded) |
| Category / type / scope | `cat:<Category>` tag + `KnowledgeType` + `KnowledgeScope` |
| Confidence | `Confidence` |
| Last reviewed | **`KnowledgeItem.LastReviewedUtc`** (new nullable column) |
| Install record | **`KnowledgePack`** table (new) |
| Searchable | existing `KnowledgeChunk` index path (best-effort) + keyword search over Title/Content |

### Database changes (additive only — migration `AddKnowledgePack`)

- New table **`KnowledgePack`** `(Id, Uid, Name, Version, Description, License, InstalledUtc, ItemCount,
  SourceManifestHash, Status)`.
- New columns on **`KnowledgeItem`**: `KnowledgePackId` (nullable FK → `KnowledgePack`, `DeleteBehavior.Restrict`)
  and `LastReviewedUtc` (nullable `DateTime`).
- No existing column is removed, renamed, or retyped. All existing rows are unaffected (`KnowledgePackId = NULL`).
- **Rollback story:** the migration's `Down()` drops only the new table/columns/index/FK; existing
  knowledge is untouched. Schema-frozen rule respected — this phase's migration is purely additive.

## Folder structure

```
knowledge-packs/professional-base-v1/
  manifest.json
  software-engineering.json
  crud-web-mssql.json
  database-sql.json
  python-ai-data.json
  security-access-control.json
  governance-project-management.json
  standards-controls.json
  finance-accounting-banking.json
  sysadmin-operations.json
  product-patterns.json
  reporting-templates.json
```

The folder is copied next to the published binaries (csproj `<Content>` → output) so a deployed app can
install it. `KnowledgePackLocator` resolves the directory across dev (`dotnet run`, walks up to the repo
root) and published layouts (`AppContext.BaseDirectory`), with a `KnowledgePacks:BaseV1Path` config override.

## Item schema

`manifest.json` carries `packUid, name, version, description, license, createdUtc, lastReviewedUtc,
itemCount, files[], legalLimitations, sourcePolicy, reviewStatus`.

Each category file is `{ "category": "...", "items": [ … ] }`; each item:

```json
{
  "uid": "<stable GUID, unique within the pack>",
  "category": "Security / Access Control",
  "title": "Deny-by-default",
  "knowledgeType": "Standard",          // parsed to KnowledgeType
  "scope": "Standards",                  // parsed to KnowledgeScope
  "description": "…",
  "applicability": "…",
  "example": "…",
  "limitation": "…",
  "confidence": 0.9,                     // 0..1
  "sourceType": "ArchitectureNote",      // parsed to SourceType
  "version": "1.0.0",
  "lastReviewedUtc": "2026-06-20",
  "reviewStatus": "Approved",            // parsed to KnowledgeStatus
  "tags": ["rbac", "least-privilege"]
}
```

Stored `Content` is rendered markdown: the description followed by **Applicability**, **Example**, and
**Limitation** sections.

## Install behaviour

The installer (`IKnowledgePackInstaller` / `KnowledgePackInstaller`, Rag layer, MSSQL-only) is:

1. **Validate the whole pack in memory first.** Required fields, unique + well-formed GUID `uid`s,
   `confidence ∈ [0,1]`, and known enum values for `knowledgeType`/`scope`/`sourceType`. If anything is
   wrong it returns the full error list and **touches no database row** — a malformed pack can never
   partially corrupt the store.
2. **Fast idempotency path.** A `SourceManifestHash` (SHA-256 over manifest + all files) lets a re-install
   of unchanged content return `AlreadyCurrent` instantly.
3. **Transactional writes** (when the provider is relational). Upsert the `KnowledgePack` anchor, then per
   item, keyed on `Uid`:
   - **New** → create the `KnowledgeItem` (`Tier=Curated`, `KnowledgePackId` stamped), write v1
     `KnowledgeVersion`, write an Import `ProvenanceEvent` with `OriginPackUid`, sync tags, chunk + index.
   - **Unchanged** (content hash matches) → no-op, keep pack link + review date fresh.
   - **Changed, unedited** → update in place + new `KnowledgeVersion` + provenance.
   - **Changed, user-edited** → **do not overwrite** — raise a `ProposedRevision` via `IPermanenceGuard`.

### Update behaviour & "no silent overwrite"

A baseline item's content changes only via (a) the installer or (b) a human edit, and a human edit/approve
**always writes a `ProvenanceMethod.Human` event**. That event is the reliable signal: if any exists for
the item, an incoming pack change is routed to a proposed revision (review-required) instead of
overwriting the user's work. Unedited baseline items upgrade cleanly with a new version snapshot.

## Security

- Viewing Base Knowledge follows the existing Knowledge posture (open to authenticated users).
- **Installing/updating a pack is Admin-only**, enforced server-side via `SecuredController.RequireAdminAsync`
  (hiding the button is never sufficient), and **audited** (`AuditEventType.KnowledgePackInstalled`).
- Startup auto-install runs as `system (startup)`, is idempotent, and never blocks startup on failure.
- No dev/test-auth or security regression is introduced.

## Baseline vs imported (and other origins)

- **Baseline** — `KnowledgePackId != null` (shown on the Base Knowledge screen with a *Baseline* badge).
- **Imported project knowledge** — `KnowledgePackId == null && ProjectId != null`.
- **Curated / generated / proposed** — `Tier`, provenance, and `ProposedRevision` distinguish these on the
  existing Knowledge surfaces.

## How to add a future pack

1. Create `knowledge-packs/<pack-id>/` with a `manifest.json` (new `packUid`) and category files.
2. Ensure every item has a stable, unique GUID `uid` and the required fields.
3. Install via the Base Knowledge **Install / Update** action (Admin), or wire an additional startup
   install. Re-installs are idempotent; user-edited items are preserved as proposed revisions.

## Legal / source limitations

- **Original summaries only.** No verbatim ISO/IEC, PMBOK, or other proprietary/paid standard text. No
  claim of official certification or compliance.
- Governance and control-style items are **practical summaries**, not substitutes for the licensed
  standards or for formal legal/compliance advice (stated in each item's `limitation`).
- Finance/accounting/banking items are **general domain concepts**, not jurisdiction-specific advice.
- The `manifest.json` records the `license`, `legalLimitations`, and `sourcePolicy`.

## v1.2 extension (R2-ACC-B2)

Pack v1.2 keeps this same architecture (no schema change) and adds: a **source registry**
(`source-registry.json`, validated at install — see `Professional-Base-Knowledge-Pack-v1.2-Sources.md`),
optional per-item `sources` (validated against the registry, rendered as a **Sources** section and `src:` tags)
and `jurisdiction` (rendered + `jur:` tag), and 11 new enterprise-advisory categories: Financial Market
Prediction & Quantitative Analysis, Accounting Standards & Financial Reporting, Mauritius Banking & Financial
Services Context, Payments/Transfers/Settlement/Reconciliation, Leasing/Lending/Credit/Arrears, Insurance &
Financial Services Controls, Cheque OCR/Signature Forgery/Document Fraud, Python OCR & Computer Vision
Deployment, PDF Reader/Classifier/Summarizer, AI/Coding/Business Research Notes, and Enterprise
Solution-Solving Playbooks. Domain limitations are documented in `Financial-AI-and-Market-Prediction-Limitations.md`,
`Cheque-OCR-and-Forgery-Detection-Research-Notes.md`, and `PDF-Reader-and-Summarizer-Design.md`; the advisory
posture is in `Enterprise-Consultant-Operating-Model.md`.

## Limitations (v1) & next improvements

- Keyword + best-effort vector search only; baseline items are not yet weighted in the RAG ranker.
- Category is modelled via a `cat:` tag (no dedicated column) — sufficient for the filter, revisit if
  category becomes a first-class facet.
- Tag re-sync on in-place updates is additive (it does not prune removed tags) — acceptable for curated
  baseline content; revisit if packs start removing tags between versions.
- Pack **export** (the reverse of install) is not implemented in v1; the schema (`Uid`, `OriginPackUid`)
  is already export-ready.
- A future pack-management screen could show per-pack diffs and pending proposed revisions from upgrades.
