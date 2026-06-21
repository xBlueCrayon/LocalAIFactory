# Knowledge Packs — Format and Authoring

**Authoritative current status:** [`docs/reports/CURRENT_STATUS.md`](../reports/CURRENT_STATUS.md)

Default knowledge ships as **packs** under `knowledge-packs/<pack-id>/`. A pack is source-only JSON.
There are **20 packs / 852 items** today (see [`DEFAULT_KNOWLEDGE_CATALOG.md`](DEFAULT_KNOWLEDGE_CATALOG.md)).

## Pack layout

```
knowledge-packs/<pack-id>/
  manifest.json          # pack metadata + file list + itemCount
  <category>.json        # one or more category files containing the items
  source-registry.json   # attribution: source families for research/standards-derived items
```

### manifest.json

| Field | Meaning |
|---|---|
| `packUid` | globally unique pack identifier (GUID) |
| `name` | human-readable pack name |
| `version` | semantic version of the pack |
| `description` | what the pack covers |
| `license` | usage terms (original summaries; no third-party text vendored) |
| `createdUtc` / `lastReviewedUtc` | authoring and last-review dates |
| `itemCount` | number of items across all `files` (must equal the actual item total) |
| `sourceRegistry` | filename of the source registry (`source-registry.json`) |
| `files` | list of category JSON files in the pack |
| `legalLimitations` / `sourcePolicy` | (where present) pack-level limitation and source policy |
| `reviewStatus` | approval state (e.g. `Approved`) |

### Category files (`<category>.json`)

A category file is `{ "category": "...", "items": [ ... ] }`. Each **item** has:

| Field | Meaning |
|---|---|
| `uid` | **globally unique** item identifier (GUID). Must be distinct across **all** packs. |
| `category` | sub-category within the pack |
| `title` | short title |
| `knowledgeType` | `rule` \| `concept` \| `pattern` \| … |
| `scope` | domain scope (e.g. `Accounting`) |
| `description` | the knowledge itself (original summary) |
| `applicability` | when/where it applies |
| `example` | a worked example |
| `limitation` | **required** — what the item does **not** assert; implementation/test implications |
| `confidence` | `high` \| `medium` \| `low` |
| `sourceType` | e.g. `original-summary` |
| `version` | item version |
| `lastReviewedUtc` | last review date |
| `reviewStatus` | `approved` \| … (approval lifecycle) |
| `tags` | array of lowercase tags |

### source-registry.json

Registers the **source families** (not specific papers/standards) that informed research- or
standards-derived items, so attribution is governed and items derived from external families are
flagged "verification required." No proprietary or official text is reproduced verbatim anywhere in
a pack.

## Authoring rules

1. **UID uniqueness.** Every item `uid` (and every `packUid`) must be a fresh GUID, unique across
   the entire `knowledge-packs/` set. **No collisions** is a hard gate.
2. **Limitation note required.** Every item must carry an honest `limitation` stating what it does
   not claim (no compliance/certification claim, no financial advice, no guaranteed OCR accuracy,
   advisory-vs-blocking distinctions, etc.).
3. **Original summaries only.** Do not copy ISO/IFRS/PMBOK/FATF/Basel/vendor (ERPNext, SAP, …) text
   verbatim. Summarize in your own words and register the source family.
4. **Tags + confidence.** Provide lowercase `tags` and a `confidence` level on every item.
5. **`itemCount` must match.** The manifest `itemCount` must equal the actual number of items across
   the pack's `files`.
6. **Approval lifecycle.** Set `reviewStatus` honestly; only approved items are treated as injected
   memory.

## How to add a pack

1. Create `knowledge-packs/<new-pack-id>/`.
2. Author `manifest.json` (new `packUid`, `files`, `itemCount`, `sourceRegistry`).
3. Author one or more `<category>.json` files; give every item a fresh `uid`, a `limitation`,
   `confidence`, and `tags`.
4. Author `source-registry.json` for any research/standards-derived attribution.
5. Run `verify-all-knowledge-packs` (and `security-audit`) and fix any schema, UID-collision,
   missing-limitation, or count-mismatch findings.
6. Update [`DEFAULT_KNOWLEDGE_CATALOG.md`](DEFAULT_KNOWLEDGE_CATALOG.md) so the catalog and totals
   stay accurate.

## How validation works

`verify-all-knowledge-packs` walks every pack and asserts:

- valid manifest and category schemas;
- `itemCount` matches the actual item total;
- **all UIDs distinct across all packs — no collisions** (currently 852 items / 852 distinct UIDs);
- every item carries a `limitation` and the required fields;
- source attribution present where required.

Current result: **PASS — 20 packs / 852 items / 852 distinct UIDs / no collisions**; `security-audit`
also **PASS**. See [`docs/reports/KNOWLEDGE_ENGINE_READY_REPORT.md`](../reports/KNOWLEDGE_ENGINE_READY_REPORT.md).
