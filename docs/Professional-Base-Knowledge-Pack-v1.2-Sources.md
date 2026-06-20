# Professional Base Knowledge Pack v1.2 — Sources & Governance

Pack v1.2 extends the baseline into enterprise advisory and domain-intelligence areas. Because these touch
regulated, standards-bound, and research-derived material, v1.2 adds a **source registry** for governance and
attribution. This document explains the governance model and the registered source families.

## Governance model

- **Original summaries only.** No verbatim copying of any ISO/IEC, IFRS, PMBOK, FATF, Basel, vendor, or
  regulatory text. Every item is an original professional summary with an explicit limitation note.
- **Source registry.** `knowledge-packs/professional-base-v1/source-registry.json` registers each source with:
  `sourceUid, title, sourceType, publisher, jurisdiction, url, retrievedUtc, licenseNote, allowedUse,
  summaryAllowed, verbatimCopyAllowed, reliabilityLevel, verificationStatus, limitationNote`.
- **Validated at install.** The installer validates the registry (required metadata, unique `sourceUid`s) and
  validates that **every `sources` reference on an item resolves to a registered source** — otherwise the whole
  pack is rejected with no DB writes. Referenced sources are stored as `src:<uid>` tags and rendered in a
  **Sources** section on each item; jurisdiction is stored as a `jur:<x>` tag and rendered too.
- **`verbatimCopyAllowed` is `false`** for every protected/official source.
- **Honesty on retrieval.** Sources are **registered references compiled from general professional knowledge**,
  not freshly fetched documents: `retrievedUtc` is `null` and exact URLs/text must be independently verified
  before any citation. No source is reproduced.
- **No fabricated citations.** Research families are registered as **topic families**, not specific papers. No
  DOIs, titles, or authors are asserted; their `reliabilityLevel` is *"Candidate research source —
  verification required."*

## Registered source families

**Mauritius / local context** (awareness only; not advice or a compliance claim):
- `src-bom` — Bank of Mauritius (supervision, AML/CFT, payment systems)
- `src-fsc-mu` — Financial Services Commission Mauritius (licensing, insurance/non-bank supervision)
- `src-frc-mu` — Mauritius Financial Reporting Council
- `src-dpo-mu` — Data Protection Office Mauritius (Data Protection Act)
- `src-fiu-mu` — Financial Intelligence Unit Mauritius (AML/CFT, STR)
- `src-companies-act-mu` — Mauritius Companies Act / statutory reporting references

**International / professional** (summary only; protected where noted):
- `src-ifrs` — IFRS Foundation / IASB (incl. IFRS 9 ECL) — *protected*
- `src-fatf` — FATF Recommendations / risk-based approach
- `src-basel` — Basel Committee (BIS) public principles (operational risk, stress testing, model risk)
- `src-iso` — ISO/IEC standards family — *protected, summary only*
- `src-pmi` — PMI / PMBOK — *protected, summary only*
- `src-nist` — NIST public cybersecurity/risk guidance
- `src-owasp` — OWASP secure-coding / app-security guidance
- `src-dotnet` — Microsoft .NET / ASP.NET Core / EF Core docs
- `src-python` — Python official docs
- `src-qdrant` — Qdrant docs

**Research topic families** (*verification required — not specific papers*):
- `src-research-ml` — ML / RAG / model-governance literature
- `src-research-ocr` — document image analysis & OCR literature
- `src-research-cv-signature` — signature verification / forgery & CV literature
- `src-research-se` — empirical software-engineering literature
- `src-research-business` — process improvement / transformation / operations literature

## Domain limitation notes (summary)

- **Financial market prediction** — analysis only, **not financial advice**; uncertainty + backtesting +
  risk warning mandatory. See `Financial-AI-and-Market-Prediction-Limitations.md`.
- **Accounting / IFRS** — educational summary; does not replace licensed standards or accountant judgment.
- **Mauritius banking / insurance** — awareness only; not BoM/FSC/FIU/DPO guidance or a compliance claim.
- **Cheque OCR / forgery** — no fraud proof, no legally conclusive forgery; human review; FP/FN disclosed.
  See `Cheque-OCR-and-Forgery-Detection-Research-Notes.md`.
- **OCR / computer vision** — accuracy is dataset-dependent; 2D document scanning ≠ 3D/medical imaging;
  nothing is medical diagnosis.
- **PDF summarizer** — provenance-first; extracted text separated from model output. See
  `PDF-Reader-and-Summarizer-Design.md`.
- **AI/coding/business research** — original syntheses of established concepts; topic-family attribution
  requiring verification; no fabricated citations.

> The registry exists so that as the platform grows, every standards- or research-derived claim remains
> **attributable, limitation-aware, and verifiable** — never an unsourced or overclaimed assertion.
