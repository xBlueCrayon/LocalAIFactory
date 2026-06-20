# CNN / OCR / Document-Intelligence Architecture

> **Scope and honesty banner.** This document describes the **pipeline interfaces** for cheque and
> document intelligence, and where a future CNN / computer-vision model would plug in. It separates
> **what is real today** (deterministic, tested prototypes) from **what is future** (trained models that
> do not exist yet). It does **not** claim production fraud detection, signature verification certainty,
> or legally conclusive forgery findings. 2D document/cheque scanning here is **not** medical imaging and
> **not** diagnosis. No real cheque images or PII are committed to this repository.

---

## 1. What is real today vs. future

| Stage | Status today | Implementation | Interface |
|---|---|---|---|
| Document hashing + provenance | **Real (tested)** | `PdfAnalyzer` (SHA-256 of bytes) | `IPdfAnalyzer` |
| PDF classification (text-based / scanned-image-only / mixed / empty) | **Real (tested, heuristic)** | `PdfAnalyzer` over PDF markers | `IPdfAnalyzer` |
| Page count, `/Title` best-effort, OCR-required flag | **Real (tested)** | `PdfAnalyzer` | `IPdfAnalyzer` |
| Extractive summarization (verbatim source sentences + page provenance) | **Real (tested)** | `ExtractiveSummarizer` | `IExtractiveSummarizer` |
| Cheque risk **triage** (Accept / Review / Reject) with evidence | **Real (tested)** | `ChequeRiskEngine` (deterministic rules) | `IChequeRiskEngine` |
| Image preprocessing (deskew/denoise/binarize) | **Future** | Python CV service | (populates DTOs) |
| OCR field reading (CAR / LAR / MICR / date / payee) | **Future** | Python OCR/CV service | populates `ChequeOcrResult` |
| Signature **detection** (present? region?) | **Future** | Python CV (CNN) service | populates `SignatureAnalysis` |
| Signature **verification** / forgery **risk score** | **Future** | Python CV service | populates `SignatureAnalysis` |
| Full PDF content-stream text extraction | **Future** | PDF parser library | (stated in `PdfAnalysis.Notes`) |

There is **no trained CV model yet.** The deterministic prototypes reason over DTOs that a future
Python OCR/CV service would populate; that boundary is stated honestly, never faked.

Source files:
- `src/LocalAIFactory.Core/Abstractions/IDocumentIntelligence.cs`
- `src/LocalAIFactory.Core/Abstractions/IChequeRisk.cs`
- `src/LocalAIFactory.Ingestion/Documents/PdfIntelligence.cs`
- `src/LocalAIFactory.Ingestion/Cheque/ChequeRiskEngine.cs`

---

## 2. Pipeline stages and interfaces

```
[bytes] -> (1) hash + provenance -> (2) PDF/image preprocessing -> (3) OCR -> (4) document classifier
        -> (5) signature-region detection -> (6) signature verification (score only)
        -> (7) cheque risk triage -> (8) human review gate
```

**Stage 1 — Hash + provenance (real).** Every document is SHA-256 hashed first. `PdfAnalysis.DocumentHash`
is the tamper-evidence/provenance anchor that every downstream record and review references.

**Stage 2 — Image preprocessing (future, Python/CV).** Deskew, denoise, binarize, MICR-band crop,
signature-band crop. Emits the cropped regions the OCR and signature stages consume. Not implemented.

**Stage 3 — OCR (future, Python/CV).** Reads cheque fields and **populates `ChequeOcrResult`**:
`CourtesyAmount` (CAR), `LegalAmount` (LAR), `Micr`, `Date`, `Payee` — each an `OcrField(Value, Confidence)`
where `Value` may be `null` ("not read") and `Confidence` is 0..1.

**Stage 4 — Document classifier.** Today `PdfAnalyzer` classifies text-based vs scanned-image-only vs
mixed/empty deterministically and sets `OcrRequired`. A future trained classifier (invoice / contract /
statement / cheque) would populate `pred_class` in `document-classification-template.csv` for evaluation.

**Stage 5 — Signature-region detection (future, CNN).** Localises the signature band and sets
`SignatureAnalysis.SignaturePresent` and `RegionDetected`. **Detection only** — "is something there / where".

**Stage 6 — Signature verification (future).** Sets `VerificationScore` (similarity to a named
`ReferenceSpecimenId`) and/or `ForgeryRiskScore` (probabilistic risk). Both are **nullable**: `null` means
"not assessed". Verification is a **separate question** from detection, with a different error profile.

**Stage 7 — Cheque risk triage (real).** `ChequeRiskEngine.Assess(ocr, signature, declaredAmount)` applies
deterministic rules and returns `ChequeRiskAssessment(Triage, HumanReviewRequired, OverallConfidence,
RiskFlags, Evidence, LimitationNote)`:
- Low-confidence/unread OCR fields raise flags (`LowConfidence = 0.60`).
- CAR vs declared-amount mismatch raises a flag.
- `ForgeryRiskScore >= 0.80` -> `Reject`; any flag -> `Review`; otherwise `Accept`.
- `OverallConfidence` is the **minimum** field confidence relied upon (honest floor, not an average).
- `Triage` is a **recommendation, never a fraud verdict**; there is no "Fraud" or "Genuine" state.
- `HumanReviewRequired` is `true` for anything not cleanly low-risk.

**Stage 8 — Human review gate.** See `Document-AI-Human-Review-Workflow.md`. High-risk items are never
auto-decided.

---

## 3. Where a future CNN / CV model plugs in

The trained model lives in an **out-of-process Python service** (FastAPI/gRPC over localhost). It does
**not** change the C# interfaces. Its only job is to **populate the existing DTOs**:

```
Python CV service  ──►  ChequeOcrResult { OcrField CourtesyAmount, LegalAmount, Micr, Date, Payee }
                   ──►  SignatureAnalysis { SignaturePresent, RegionDetected,
                                            VerificationScore?, ForgeryRiskScore?, ReferenceSpecimenId? }
```

The deterministic `IChequeRiskEngine` is unchanged: swapping a stub populator for a real CV populator does
not relax any safety rule. Every output still carries `Evidence`, the image hash (provenance), and
`LimitationNote`. Detection and verification stay separate by construction of `SignatureAnalysis`.

---

## 4. Model-registry entry for CV models

CV/OCR models are registered like any other model via `ModelConfiguration`
(`src/LocalAIFactory.Core/Entities/ModelConfiguration.cs`). A future CV registration would use:

- `Name` — human label, e.g. `cheque-ocr-v0`, `signature-detector-v0`.
- `Provider` — a local/HTTP provider pointing at the Python CV service (`BaseUrl`).
- `ModelName` — the served model id/version (e.g. weights tag).
- `IsEnabled` — gates the model off by default until evaluated (deny-by-default posture).

A registered CV model must reference its **governed evaluation result** (see the evaluation plan) before
`IsEnabled` is set true. Unevaluated weights stay disabled. Model version + dataset version are recorded
together so a triage decision can be reproduced.

---

## 5. CPU vs. GPU

- **MSSQL-only mode (no GPU, no Python service):** the deterministic prototypes
  (`PdfAnalyzer`, `ExtractiveSummarizer`, `ChequeRiskEngine`) run **CPU-only** and degrade gracefully —
  classification, provenance, extractive summary, and rule-based triage all work without any model.
- **GPU available (RTX 5070 Ti, 16 GB):** the future Python OCR/CV/signature models run on GPU for
  throughput. The GPU is **optional**, mirroring the platform's "Ollama/Qdrant optional" rule: if the
  CV service is absent, OCR/signature DTO fields are simply `null`/`not assessed`, which the risk engine
  already handles (raises flags, forces human review). The platform never blocks page rendering on the
  CV service.

---

## 6. Evidence, image-hash, and provenance

- Every document/cheque is SHA-256 hashed **before** any analysis (`PdfAnalysis.DocumentHash`).
- `ChequeRiskAssessment.Evidence` records each field's value + confidence and the raw signature
  detection/verification signals — the decision is auditable.
- `LimitationNote` is attached to every assessment, stating it is triage only, not a fraud determination.
- No real cheque image bytes are stored in the repo; only hashes and synthetic template rows.

---

## 7. Hard limits (do not violate)

1. Never assert automatic fraud, "genuine", or legally conclusive forgery.
2. Signature **detection** and **verification** are always separate.
3. Accuracy is dataset-dependent and **currently unmeasured** — no headline accuracy claim.
4. High-risk items always require human review.
5. This is 2D document/cheque scanning — **not** medical imaging, **not** diagnosis.
6. Privacy/data-protection apply: no real cheque images or PII in source control.
