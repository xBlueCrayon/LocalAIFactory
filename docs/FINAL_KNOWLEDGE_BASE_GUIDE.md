# Final Knowledge Base Guide — LocalAIFactory

The knowledge base that ships with LocalAIFactory: the **4 packs (438 items)**, the three knowledge
**scopes** (general / project / chat-imported), how **seeding** works (startup auto-install of all
packs, idempotent, propose-never-overwrite), how to **verify**, and the honesty/limitations posture.

> The durable asset LocalAIFactory hands over is this **curated, governed memory plus the platform
> that grows it** — not any particular model. MSSQL is the runtime source of truth; the JSON packs
> are the source-controlled seed format. For the deeper model see
> [`Knowledge-Architecture-General-Project-Chat.md`](Knowledge-Architecture-General-Project-Chat.md)
> and [`Knowledge-Pack-Authoring-Guide.md`](Knowledge-Pack-Authoring-Guide.md).

---

## 1. The four shipped packs (438 items)

| Pack | Folder | Items | Covers |
|---|---|---|---|
| **Professional Base** (v1.2) | `professional-base-v1/` | **390** | Software engineering, CRUD-over-MSSQL, databases/SQL, Python/AI/RAG, security & access control, governance/project management, control-style standards, finance/accounting/banking, sysadmin/operations, product/admin patterns, reporting — plus v1.2 advisory/domain intelligence (quantitative analysis, accounting standards, the Mauritius banking context, payments/settlement/reconciliation, leasing/lending/credit, insurance controls, cheque OCR & document-fraud concepts, Python OCR/CV deployment, PDF reading/summarization, enterprise solution playbooks). |
| **Financial Institution Operations** (v1) | `financial-institution-operations-v1/` | **16** | Customer & account lifecycle, segregation of duties, approval workflows, reconciliation, operational risk and controls. |
| **KYC / AML / Transaction Approval** (v1) | `kyc-aml-transaction-approval-v1/` | **16** | Customer due-diligence (CDD/EDD), screening, transaction monitoring, and high-risk transaction-approval concepts. |
| **Market Intelligence & Forecasting** (v1) | `market-intelligence-forecasting-v1/` | **16** | Market-data governance, signal architecture, forecasting discipline, and model governance. |

**Total: 390 + 48 = 438 items.** Each pack ships as a folder with a `manifest.json` (name, version,
item count, `sourcePolicy`, `legalLimitations`, `reviewStatus`) and item JSON files. The Professional
Base pack also carries a `source-registry.json`; research-/standards-derived items are attributed via
`src:` tags.

Every item is an **original professional summary**, **awareness-level only**. Each carries a
limitation note and a confidence value.

---

## 2. The three knowledge scopes

LocalAIFactory separates knowledge by scope; retrieval respects the boundary
([`Knowledge-Separation-and-Retrieval-Rules.md`](Knowledge-Separation-and-Retrieval-Rules.md)):

| Scope | How it is set | Origin | Trust |
|---|---|---|---|
| **General** | `ProjectId = null`, Tier = Curated | The shipped packs | Curated baseline; injected first into prompt context |
| **Project** | `ProjectId` set | Imported projects, project-scoped curation | Scoped to that project; not leaked across projects |
| **Chat-imported** | Deterministic chat-import extractor | ChatGPT/Claude/markdown transcripts | **Proposals only** — never auto-approved |

The chat-import extractor turns conversations into Decision/FixPattern/Rule/Snippet/Prompt
**proposals** that sit in a review queue. Nothing it produces becomes authoritative without a human
approving it — see [`Chat-Learning-Pipeline.md`](Chat-Learning-Pipeline.md) and
[`Conversation-To-Knowledge-Extraction-Rules.md`](Conversation-To-Knowledge-Extraction-Rules.md).

---

## 3. How seeding works

### Startup auto-install of all packs

On first startup the app **migrates, seeds, and installs all knowledge packs**:

- Controlled by **`KnowledgePacks:InstallAllAtStartup`** (default **`true`**). With it on, all four
  packs install; the baseline 390-item professional pack plus the 48 domain items reach **438**.
- The install is **idempotent**: re-running does **not** duplicate items. Items are keyed by `Uid`;
  the installer converges on the current set rather than appending.
- It is **propose-never-overwrite**: any change to a curated item goes through `IPermanenceGuard`,
  which blocks direct mutation of `Curated` knowledge and routes revisions to a proposal. The
  installer never silently overwrites approved knowledge — see
  [`AI-Output-Provenance-and-Approval.md`](AI-Output-Provenance-and-Approval.md).

### Scripted seeding (CI / unattended)

```powershell
pwsh database/seed-professional-knowledge-base.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

Applies migrations, starts the app briefly to trigger the install, waits until the baseline items are
present (or times out via `-TimeoutSec`), stops the app, and verifies. Re-running does not duplicate.

### Provenance and source governance

Each installed item records pack-origin provenance (`ProvenanceEvent.OriginPackUid`) and, where
applicable, registered sources (`src:` tags). The installer **rejects unregistered sources**, so
nothing unsourced sneaks in. Provenance is append-only — the lineage of any item stays explainable.

---

## 4. How to verify

Read-only and safe against any environment; exits non-zero on any failed check (deployment gate):

```powershell
pwsh database/verify-knowledge-base.ps1 -ServerInstance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"
```

It checks, read-only:

- a `KnowledgePack` row exists (`Status = 0`);
- pack item count ≥ `-MinItems` (default **100**);
- **no duplicate `Uid`s** among pack items;
- **all pack items are curated** (`Tier = 1`);
- pack-origin **provenance** is present (`ProvenanceEvents.OriginPackUid`);
- the **source registry** is referenced (`src:` tags).

Verified this release: the knowledge base verifies with all items curated and no duplicate Uids.
After a restore into a verify database you can re-run the same check against that copy — see
[`Backup-Restore-Runbook.md`](Backup-Restore-Runbook.md) §4.

---

## 5. Honesty and limitations posture

This is the non-negotiable part. The knowledge base is **distributable precisely because** it holds
no proprietary text and makes no authoritative claims:

- **Original summaries only.** All content is original professional summary authored for
  LocalAIFactory. **No third-party proprietary, regulatory, standard, or vendor text is reproduced
  verbatim.** That source policy is recorded in every pack's `manifest.json`.
- **Awareness-level, not authoritative.** Items describe common patterns at a conceptual level to
  inform software design and validation. Each item carries a `legalLimitations` note.
- **No compliance / regulatory / financial / fraud claim.** The KYC/AML and operations packs are
  **not** legal, regulatory, or compliance advice; a screening hit or monitoring alert is a signal to
  review, not a legal determination. The market pack is **not** financial or investment advice and
  provides no buy/sell recommendation (see [`Market-Module-Disclaimers.md`](Market-Module-Disclaimers.md)).
  Jurisdiction- and institution-specific rules must always be confirmed against current rulebooks,
  regulator guidance, and the institution's own approved procedures before use.
- **AI never overrides curated knowledge.** Optional local AI can *propose* revisions; it has no
  write path to approved knowledge. Human approval is the sole promotion gate
  ([`AI-Governance.md`](AI-Governance.md)).

For what the knowledge base does **not** do (e.g. source-attestation is a human act; no automated
provenance audit yet), see [`Known-Limitations.md`](Known-Limitations.md) §7 and
[`Compliance-Disclaimers.md`](Compliance-Disclaimers.md).

---

## 6. Authoring and extending

To add or revise packs, follow [`Knowledge-Pack-Authoring-Guide.md`](Knowledge-Pack-Authoring-Guide.md):
original summaries only, every item with a limitation note and confidence, sources registered, review
status set, and `InstallAllAtStartup` (or the seed script) re-run idempotently. Curated items can
only be changed through a proposal routed by `IPermanenceGuard` — never by hand-editing the database.
