# PDF Reader, Classifier & Summarizer — Design & Provenance Rules

This note governs the **PDF Reader, Classifier and Summarizer** knowledge category and any future PDF
document-intelligence feature in LocalAIFactory. The governing principle is **provenance-first**: a summary
is never trusted without traceability to the source.

## Provenance-first rules (non-negotiable)

- **Never summarize without provenance.** Every summary links back to the source document and, where possible,
  to **page-level** locations.
- **Distinguish extracted text from model summary.** Extracted (verbatim, from the PDF) and generated (model
  summary) content are always labelled distinctly so a reader knows which is which.
- **Keep the original document hash.** Store a hash of the ingested document so the exact source is verifiable
  and tamper-evident.
- **Human review for legal/financial documents.** High-stakes documents require human verification before any
  summary is acted upon.
- **Hallucination risk is disclosed.** Model summaries can fabricate or mis-attribute; citations are preserved
  and extraction confidence is surfaced.

## Pipeline shape

1. **Ingest & hash** — store the document, compute and record its hash; capture metadata.
2. **Classify** — text-based PDF vs scanned (image) PDF vs mixed; route accordingly.
3. **Extract** — text extraction for text PDFs; OCR for scanned pages (shares the OCR/CV discipline — accuracy
   is dataset-dependent, confidence is recorded); table extraction and image extraction as needed.
4. **Chunk with provenance** — chunk for retrieval while retaining **page-level references** on each chunk.
5. **Summarize** — generate summaries that cite the supporting chunks/pages; keep extracted vs generated
   separation; surface extraction + summary confidence.
6. **Govern** — redaction and privacy handling for sensitive content; an **audit trail** of who summarized
   what, when, from which document hash; multi-document comparison and report generation build on the same
   provenance spine.

## Integration with LocalAIFactory

- Reuses the platform's MSSQL-authoritative model and the **no-silent, evidence-first** posture used elsewhere
  (coverage/gap honesty, knowledge provenance).
- Vector retrieval (Qdrant) and local LLM summarization are **optional accelerators**; keyword retrieval and
  deterministic extraction work without them.
- Summaries entering the knowledge base would follow the same approval/permanence rules as other knowledge
  (curated, versioned, never silently overwritten).

## Attribution

Summarization/hallucination concepts → `src-research-ml`; OCR concepts → `src-research-ocr`. Research sources
are topic families requiring verification, not specific papers.

> Bottom line: a PDF summary is only as trustworthy as its provenance. Extracted text, page references, the
> document hash, confidence, and human review for high-stakes documents are mandatory — not optional polish.
