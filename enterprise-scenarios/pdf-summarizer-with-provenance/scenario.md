# Scenario: PDF Reader / Classifier / Summarizer with Provenance

**Fictional firm:** *Halberd & Roe LLP* — a mid-size legal and financial advisory firm that
reviews contracts, loan agreements, and audited financial statements on behalf of corporate
clients. This scenario is an **original synthetic exercise** for LocalAIFactory's enterprise
capability simulation suite. It does not clone, certify, or represent any real vendor product.

---

## Business Problem

Halberd & Roe's analysts spend hours reading long PDFs (50–400 pages): commercial contracts,
credit agreements, and audited financials. They need fast, trustworthy summaries that point back
to the exact source page. The firm's professional liability rules forbid relying on any summary
that cannot be traced to the original document. A wrong or hallucinated summary of an indemnity
clause or a covenant ratio could cause a material advisory error. The firm therefore needs a
tool that **never produces a summary without provenance**, preserves an immutable record of the
exact document analysed, and routes legal and financial conclusions through human review.

## Current-State Process

- Analysts open each PDF manually and read end to end.
- Key clauses and figures are copied into a Word memo by hand; page numbers are noted
  inconsistently or omitted.
- Scanned PDFs are retyped or skimmed because there is no reliable OCR step.
- No verification that the file reviewed matches the file later cited (version drift).
- Review sign-off is informal; there is no enforced audit trail.

## Target-State Process

1. Document is uploaded and **hashed (SHA-256) at ingest**; the hash is stored and shown.
2. Document is **classified** (contract / financial statement / other) to pick an extraction
   profile.
3. Text is **extracted**; scanned or mixed pages are routed through **OCR** with a quality flag.
4. Text is **chunked with page-level provenance** (every chunk keeps its source page span).
5. A summary is generated **with inline citations** to page numbers and chunk IDs.
6. Extracted source text is stored **separately** from model output so the two are never conflated.
7. Legal and financial summaries are placed in a **human review** queue before release.

## Users and Roles

- **Analyst** — uploads documents, requests summaries, reviews citations.
- **Reviewer (Legal/Financial)** — mandatory human sign-off for legal/financial documents.
- **Compliance Auditor** — read-only access to the immutable audit trail and document hashes.
- **Administrator** — manages users, extraction profiles, and retention policy. No edit access to
  audit records.

## Data Entities (documents, hashes, pages, extractions, summaries, citations)

- **Document** — id, filename, classification, upload timestamp, uploader, retention class.
- **DocumentHash** — SHA-256 of the original bytes, captured at ingest, immutable.
- **Page** — document id, page number, source type (native-text / scanned / mixed), OCR flag.
- **Extraction** — page id, extracted text, extraction method (native / OCR), confidence,
  bounding info where available. This is *source text*, never model output.
- **Chunk** — extraction-derived span with start/end page provenance and a stable chunk id.
- **Summary** — model-generated text, linked to the document hash and the model/version used.
- **Citation** — links a sentence/claim in a Summary to one or more Chunks and page numbers.

## Integrations

- **Document store** (firm DMS) — source of incoming PDFs; integration is read-only on ingest.
- **OCR engine** — pluggable; treated as optional and degrades to "needs manual OCR" if absent.
- **Local model runtime** (Ollama-style, optional) — summary generation; absent runtime yields
  extraction + classification only, never a fabricated summary.
- **Identity provider** — for role-based access (Analyst / Reviewer / Auditor / Admin).

## Security and Audit Controls (audit trail, redaction, privacy)

- **Immutable, append-only audit trail**: every upload, hash, extraction, summary, citation, and
  review decision is logged with actor, timestamp, and document hash.
- **Redaction**: a redaction pass can mask PII/privileged spans before a summary is produced;
  redactions are recorded but the masked content is never sent to the model.
- **Privacy**: documents and summaries stay local; no external transmission. Access is
  role-scoped and deny-by-default.
- **Hash binding**: every Summary references the DocumentHash it was derived from; a re-uploaded
  file with a different hash cannot reuse an old summary.

## Reporting Requirements

- Per-document **provenance report**: every summarized claim with its page citation and chunk id.
- **Coverage report**: pages extracted natively vs by OCR vs flagged unreadable.
- **Review report**: which legal/financial summaries were reviewed, by whom, and the outcome.
- **Audit export**: append-only log filtered by document, actor, or date range.

## Failure Modes (hallucination, scanned/mixed PDFs, OCR errors)

- **Hallucination** — model asserts a clause/figure not in the source. Mitigation: every summary
  claim must carry a citation; uncited claims are blocked and flagged.
- **Scanned / mixed PDFs** — pages without a native text layer. Mitigation: detect per page, route
  to OCR, flag low-confidence pages instead of guessing.
- **OCR errors** — garbled numbers or characters. Mitigation: confidence scoring, low-confidence
  flag surfaced to the reviewer, never silently summarized as fact.
- **Version drift** — wrong file cited. Mitigation: document hash binding end to end.
- **Runtime absence** — no model available. Mitigation: stop at extraction; never fabricate.

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. Headline rules: never summarize
without provenance; always keep the document hash; require human review for legal/financial
documents.

## Expected Architecture

```
ingest + hash (SHA-256)
   -> classify (contract / financial / other)
      -> extract  (native text)  |  OCR (scanned/mixed, with confidence)
         -> chunk WITH page provenance (chunk id + page span)
            -> summarize WITH citations (page refs + chunk ids)
```

- **Separation of concerns:** extracted source text and model-generated summary are stored as
  distinct entities and never merged in storage or display.
- **Provenance is structural, not cosmetic:** a summary cannot be persisted unless each claim
  links to at least one chunk/page.
- **Optional services degrade gracefully:** OCR and the model runtime are optional; their absence
  reduces output scope but never produces an unprovenanced or fabricated result.

## Expected Tests

- Hash is computed at ingest and is stable across reads.
- Classification selects the correct extraction profile for sample contracts vs financials.
- Native-text and scanned sample pages route to the correct extraction method.
- Every chunk carries a valid page span; no orphan chunks.
- A summary with any uncited claim is rejected.
- Re-upload with altered bytes yields a new hash and cannot reuse a prior summary.
- Legal/financial summaries cannot be released without a recorded reviewer decision.
- With the model runtime absent, the pipeline stops at extraction and emits no summary.

## Expected Deployment Concerns

- Local-first: must run with only the database present; OCR and model runtime optional.
- Storage growth from extracted text and OCR images; retention policy required.
- OCR throughput on large scanned documents; queue and background processing.
- Model/version pinning so summaries are reproducible against a recorded model id.

## Rollback Considerations

- Additive, backward-compatible schema changes only; no destructive migrations without approval.
- Summaries and citations are versioned by document hash and model id, so a bad summarization run
  can be withdrawn without touching source extractions or the audit trail.
- The append-only audit log is never rewritten on rollback; a rollback is itself an audited event.
- Disabling the model runtime cleanly reverts the system to extraction-only mode.

## CEO/CTO Summary

Halberd & Roe needs to read long legal and financial PDFs faster **without** trading away trust.
The design hashes every document at ingest, separates the firm's source text from anything the
model writes, and refuses to emit a summary unless each claim cites a specific page. Legal and
financial conclusions still pass through a human reviewer, and every step is recorded in an
append-only audit trail bound to the document hash. The result is speed with traceability: faster
review, fewer transcription errors, and a defensible record of exactly what was analysed and who
signed off — without claiming the machine replaces professional judgment.
