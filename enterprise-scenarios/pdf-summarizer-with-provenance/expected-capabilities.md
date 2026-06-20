# Expected Capabilities — Honest Statement

This document states **honestly** what LocalAIFactory brings to this scenario **today** versus
what is a **future implementation slice**. Nothing here claims a shipped feature, certification,
or vendor parity. It is a reasoned design position, not a delivery claim.

## What exists today (knowledge + design)

- **Provenance-first discipline** as an established principle in this codebase: approved knowledge
  and source material are tracked, and outputs are expected to trace back to inputs.
- **Local-first architecture** that runs with only the database present; vector store and model
  runtime are optional and degrade gracefully. This directly supports the "no model = no
  fabricated summary" rule.
- **Ingestion pipeline patterns** (ZIP/file import, per-file robustness, content/binary detection,
  honest encoding decode) that the PDF ingest + hash + extraction stages can build on.
- **Append-only audit and RBAC patterns** (Windows auth, deny-by-default, project access control)
  that the document audit trail and reviewer roles can reuse.
- **Chunking and retrieval concepts** already present for code/knowledge that generalize to
  page-provenance chunking.
- A clear **data model design** for documents, hashes, pages, extractions, chunks, summaries, and
  citations (see `scenario.md`).

## What is a future implementation slice (not built here)

- **PDF parsing and per-page text-layer detection** — choosing and integrating a PDF library.
- **OCR integration** with confidence scoring for scanned/mixed pages.
- **Document classification** model/profile selection (contract vs financial vs other).
- **Citation-enforced summarization** — the runtime guard that blocks any summary whose claims are
  not all cited.
- **Redaction pass** for PII/privileged spans before summarization.
- **Reviewer workflow UI** and the legal/financial sign-off queue.
- **Reporting/export** (provenance, coverage, review, audit reports).

## Honesty guardrails

- This is a **simulation scenario**: it describes a credible target design and the tests that would
  prove it, not a finished product.
- No claim of accuracy, certification, or regulatory approval is made.
- The hard rules — **never summarize without provenance, always keep the document hash, require
  human review for legal/financial** — are stated as design requirements, and the acceptance
  criteria and tests are written so that a future implementation can be measured against them
  rather than assumed.
