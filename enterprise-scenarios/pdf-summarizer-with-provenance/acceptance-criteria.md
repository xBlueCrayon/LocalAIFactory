# Acceptance Criteria — Measurable Checklist

Each item is written to be objectively verifiable. "Pass" means the stated, measurable condition
holds. These criteria encode the hard rules: **never summarize without provenance, always keep the
document hash, require human review for legal/financial documents.**

## Document hash and integrity

- [ ] On ingest, a **SHA-256 hash** of the original PDF bytes is computed and stored before any
      processing. (Verify: stored hash equals an independently computed hash of the same bytes.)
- [ ] The stored hash is **immutable** — no role can edit it after ingest. (Verify: edit attempt is
      rejected and audited.)
- [ ] Every Summary record references the **DocumentHash** it was derived from. (Verify: no summary
      exists without a non-null hash link.)
- [ ] A re-uploaded file with **different bytes produces a different hash** and cannot reuse a prior
      summary. (Verify: altered byte yields new hash; old summary not associated.)

## Provenance (never summarize without it)

- [ ] Every **Chunk** carries a valid **page span** (start/end page) and a stable chunk id.
      (Verify: zero orphan chunks across the test corpus.)
- [ ] Every **claim/sentence in a Summary** carries at least one **Citation** to a chunk and page.
      (Verify: count of uncited claims = 0.)
- [ ] A summary containing **any uncited claim is rejected** and not persisted. (Verify: injected
      uncited claim is blocked.)
- [ ] **Extracted source text and model summary are stored as separate entities** and are never
      merged. (Verify: schema separation; UI labels them distinctly.)

## Extraction / OCR honesty

- [ ] Each page is classified as **native-text / scanned / mixed**. (Verify: matches labelled
      sample pages.)
- [ ] Scanned/mixed pages are routed to **OCR with a confidence score**. (Verify: OCR pages have a
      confidence value; low-confidence pages are flagged.)
- [ ] **Low-confidence or unreadable pages are flagged**, not silently summarized. (Verify: flag is
      visible to the reviewer.)

## Human review (legal/financial)

- [ ] Any summary of a **legal or financial** document is queued for **human review** and cannot be
      released without a recorded reviewer decision. (Verify: release blocked until sign-off.)
- [ ] The reviewer decision (approve/reject, actor, timestamp) is written to the **audit trail**.
      (Verify: decision present and append-only.)

## Audit, security, privacy

- [ ] Every upload, hash, extraction, summary, citation, and review decision is recorded in an
      **append-only audit trail** with actor, timestamp, and document hash. (Verify: no in-place
      edits; entries are additive only.)
- [ ] Access is **role-scoped and deny-by-default** (Analyst / Reviewer / Auditor / Admin).
      (Verify: cross-role access attempt denied and audited.)
- [ ] **Redacted** spans are recorded and the masked content is **not sent to the model**.
      (Verify: model input excludes redacted text.)

## Graceful degradation (local-first)

- [ ] With the **model runtime absent**, the pipeline completes ingest → hash → classify → extract
      and emits **no summary** (never a fabricated one). (Verify: summary count = 0; extraction
      present.)
- [ ] With **OCR absent**, scanned pages are flagged "needs manual OCR" rather than guessed.
      (Verify: flag present; no fabricated text.)
- [ ] Core pages still load with **only the database present**. (Verify: pages return without
      external services.)
