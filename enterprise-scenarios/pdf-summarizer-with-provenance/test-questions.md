# Test Questions and Strong Answers

These questions probe whether the design genuinely upholds the hard rules. Answers are written to
be defensible to a reviewer, an auditor, and a skeptical CTO.

### 1. How do we know it's not hallucinating?

Because the system **cannot persist a summary unless every claim is cited**. Each summary sentence
links to one or more chunks, and each chunk carries a page span back to the original document.
Extracted source text is stored **separately** from model output, so a reviewer can compare what
the model said against the exact page it points to. Anything uncited is blocked and flagged. And
for legal/financial documents, a **human reviewer** signs off before release. Hallucination is not
"hopefully avoided" — uncited output is structurally rejected.

### 2. What exactly was analysed — how do we prove it later?

The **SHA-256 hash** of the original bytes is captured at ingest, stored immutably, and bound to
every extraction, summary, and citation. To prove what was analysed, recompute the hash of the
file and compare. A different file = a different hash = it was not this analysis.

### 3. What happens with a scanned PDF that has no text layer?

Each page is classified as native-text / scanned / mixed. Scanned and mixed pages are routed to
**OCR with a confidence score**. Low-confidence or unreadable pages are **flagged for the
reviewer**, never silently summarized as if they were clean text.

### 4. What if OCR garbles a number in a financial statement?

The page carries an OCR **confidence value**, and low-confidence figures are flagged. Because the
summary cites the specific page/chunk, the reviewer can open the source and verify the number
directly. The figure is never presented as authoritative without that traceable source.

### 5. Why keep extracted text and the summary separate?

So they can never be confused. Extracted text is the firm's **source of truth**; the summary is
**model output**. Keeping them as distinct entities means a citation always resolves to real
source text, and a reviewer can audit the model's claim against the original without the two being
blended in storage or display.

### 6. Can a summary be released for a contract without anyone checking it?

No. Legal and financial documents route to a **mandatory human review** queue. Release is blocked
until a reviewer records an approve/reject decision, and that decision is written to the
append-only audit trail with actor and timestamp.

### 7. What if the document is re-uploaded after an edit — could we cite a stale summary?

No. The new bytes produce a **new hash**. Summaries are bound to the hash they were derived from,
so an edited document cannot inherit the previous summary. Version drift is closed by hash binding.

### 8. What runs locally, and what happens if the model isn't available?

Everything runs locally. If the **model runtime is absent**, the pipeline still ingests, hashes,
classifies, and extracts — but produces **no summary**. It never fabricates one to fill the gap.
Likewise, if OCR is absent, scanned pages are flagged rather than guessed.

### 9. How is privacy and privileged content handled?

A **redaction pass** can mask PII/privileged spans before summarization. The masked content is
**not sent to the model**, and the redaction itself is recorded. Documents and summaries stay
local with role-scoped, deny-by-default access.

### 10. Can an administrator quietly alter the audit record?

No. The audit trail is **append-only**. No role — including Admin — can rewrite or delete entries;
corrections are added as new audited events. The Compliance Auditor has read-only access to verify
this.

### 11. How would we test that the provenance guarantee actually holds?

Inject a summary claim with no citation and confirm it is **rejected**; scan the corpus for orphan
chunks (must be zero); verify every summary has a non-null document-hash link; and confirm a
legal/financial summary cannot be released without a recorded reviewer decision. See
`acceptance-criteria.md` for the full measurable checklist.

### 12. Is this a finished product or a certified solution?

Neither. This is an **original synthetic scenario** in a capability simulation suite. It states an
honest design and the tests that would prove it. No certification, regulatory approval, or vendor
parity is claimed; implementation of PDF parsing, OCR, classification, and the citation-enforcement
guard is a future slice (see `expected-capabilities.md`).
