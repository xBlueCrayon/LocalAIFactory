# Public-Material Learning Governance

Defines how LocalAIFactory may learn from public videos, manuals, vendor docs, papers, and tutorials **without
copying protected content**. This governs any future ingestion of external material and complements the
knowledge-pack source registry.

## Core rules

- **Summarize only — never copy verbatim.** Output is original summaries and practical patterns. No verbatim
  text from manuals, articles, transcripts, or slides.
- **Register every source.** Each external material gets a source-registry entry: title, type, publisher,
  jurisdiction, URL/reference, retrieved/registered date, license/copyright note, allowed use,
  `summaryAllowed`, `verbatimCopyAllowed` (false unless explicitly public-domain/open-license), reliability
  level, verification status, limitation note.
- **Mark provenance and status.** Record whether the material is **official/vendor / community / research**,
  and its **verification status**. If exact metadata (DOI, author, edition) cannot be verified, mark it
  *"verification required"* — never fabricate citations.
- **Inspiration, not cloning.** Public material informs *capability and structure*, never a reproduction of a
  proprietary product, manual, or UI.
- **No certification / compatibility claims.** Learning from a vendor's public docs never implies certification,
  compatibility, or equivalence.
- **Nothing protected is committed to the repo.** No scraped corpora, downloaded videos, manuals, or PDFs are
  committed to Git. `.gitignore` excludes model/data caches; raw third-party material stays out of version control.
- **High-risk claims require verification.** Regulatory, legal, financial, medical, or safety claims must be
  verified against the authoritative source before they are relied upon; otherwise they carry an explicit
  limitation and "verification required".

## Material classification

| Class | Examples | Verbatim? | Use |
|---|---|---|---|
| Official / standard-setter | IFRS, FATF, Basel, ISO, regulator sites | No (protected) | original summary + registry + limitation |
| Vendor documentation | .NET, Python, Qdrant docs | No | original summary of documented behaviour |
| Community / open-license | OWASP, open-source docs | Only if licence permits, with attribution | summary preferred |
| Peer-reviewed / research | journal/conference literature | No | topic-family attribution; "verification required" |
| Public domain | government works (verify per item) | Possibly, if confirmed public domain | confirm first |

## Future ingestion support (designed, gated)

When external-material ingestion is implemented, it must:

- **Video transcripts** — only if legally available; store **original summaries** with **timecode references**,
  not the transcript verbatim; register the source and its licence.
- **Manuals / PDFs** — ingest with **provenance**: document hash, **page references**, extracted-text vs
  summary separation (per `PDF-Reader-and-Summarizer-Design.md`); never store protected text as knowledge.
- **Source reliability levels** — every ingested item inherits its source's reliability and limitation.
- **User approval before using proprietary material** — proprietary or restricted-licence material requires
  explicit human approval before it informs any knowledge item, and is never redistributed.

## Enforcement

The knowledge-pack installer already enforces the spine of this policy: items may only cite **registered**
sources (unregistered references are rejected), and the registry sets `verbatimCopyAllowed=false` and
"verification required" on protected/research families. Future ingestion paths must reuse the same registry
and validation.
