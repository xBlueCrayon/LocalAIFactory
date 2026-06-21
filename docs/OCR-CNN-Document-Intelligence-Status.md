# OCR / CNN Document-Intelligence — Honest Status

The honest status of document and cheque intelligence in LocalAIFactory. This is the **positioning**
companion to the architecture in
[`CNN-OCR-Document-Intelligence-Architecture.md`](CNN-OCR-Document-Intelligence-Architecture.md): what
is real today, what is future, and the exact proof to ship the future parts.

> **Status: OPTIONAL / FUTURE.** Today there are **deterministic prototypes only**. There is **NO
> trained computer-vision model**, **no OCR engine**, and **no fraud or signature certainty**. Nothing
> here is medical imaging or diagnosis. No real cheque images or PII are committed to the repository.
> This module makes **no** fraud, forgery, compliance, or regulatory claim.

---

## 1. Real today vs. future

| Stage | Status | What it is |
|---|---|---|
| Document hashing + provenance (SHA-256) | **Real (tested)** | `PdfAnalyzer` — the tamper-evidence/provenance anchor every downstream record references |
| PDF classification (text / scanned-image-only / mixed / empty) | **Real (tested, heuristic)** | `PdfAnalyzer` over PDF markers; sets `OcrRequired` |
| Page count, `/Title` best-effort, OCR-required flag | **Real (tested)** | `PdfAnalyzer` |
| Extractive summarization (verbatim source sentences + page provenance) | **Real (tested)** | `ExtractiveSummarizer` — no hallucination, extractive only |
| Cheque risk **triage** (Accept / Review / Reject) with evidence | **Real (tested)** | `ChequeRiskEngine` — deterministic rules over DTOs |
| Image preprocessing (deskew / denoise / binarize / band-crop) | **Future** | out-of-process Python CV service |
| OCR field reading (CAR / LAR / MICR / date / payee) | **Future** | Python OCR/CV — would populate `ChequeOcrResult` |
| Signature **detection** (present? region?) | **Future** | Python CV (CNN) — would populate `SignatureAnalysis` |
| Signature **verification** / forgery **risk score** | **Future** | Python CV — separate question from detection |
| Full PDF content-stream text extraction | **Future** | PDF parser library |

**There is no trained CV model.** The deterministic prototypes reason over DTOs that a future Python
OCR/CV service would populate; that boundary is stated honestly and never faked. Source files are
listed in the architecture doc §1.

---

## 2. What the deterministic prototypes guarantee

- **Provenance first.** Every document/cheque is SHA-256 hashed **before** any analysis
  (`PdfAnalysis.DocumentHash`).
- **Triage is a recommendation, never a verdict.** `ChequeRiskEngine.Assess(...)` returns
  `Accept / Review / Reject` with `RiskFlags`, `Evidence`, `OverallConfidence`, and a `LimitationNote`.
  There is **no "Fraud" or "Genuine" state** by construction.
- **Honest confidence floor.** `OverallConfidence` is the **minimum** field confidence relied upon,
  not an average — it cannot be inflated.
- **Detection ≠ verification.** `SignatureAnalysis` keeps signature *detection* (present / region) and
  *verification* (`VerificationScore`, `ForgeryRiskScore`, both nullable → "not assessed") as separate
  questions with different error profiles.
- **Degrades gracefully.** With no Python CV service / GPU, OCR and signature DTO fields are
  `null` / "not assessed"; the rule engine **raises flags and forces human review** rather than
  guessing ([`Offline-Mode-Guide.md`](Offline-Mode-Guide.md)).

---

## 3. Where a future CNN / CV model plugs in

The trained model would live in an **out-of-process Python service** (FastAPI/gRPC over localhost) and
its only job is to **populate the existing DTOs** (`ChequeOcrResult`, `SignatureAnalysis`). It does
**not** change the C# interfaces and **cannot relax any safety rule** — every output still carries
evidence, the image hash, and a limitation note, and detection stays separate from verification.

Registration is deny-by-default: a CV model is registered via `ModelConfiguration` with `IsEnabled`
**false** until it references a **governed evaluation result**. Unevaluated weights stay disabled.
Model version + dataset version are recorded together so a triage decision can be reproduced.

---

## 4. Hard limits (do not violate)

1. Never assert automatic fraud, "genuine", or legally conclusive forgery.
2. Signature **detection** and **verification** are always separate.
3. Accuracy is dataset-dependent and **currently unmeasured** — **no headline accuracy claim**.
4. High-risk items always require human review ([`Document-AI-Human-Review-Workflow.md`](Document-AI-Human-Review-Workflow.md)).
5. This is 2D document/cheque scanning — **not** medical imaging, **not** diagnosis.
6. No real cheque images or PII in source control — only hashes and synthetic template rows.
7. No fraud, compliance, or regulatory claim is made anywhere in this module.

---

## 5. Exact proof to ship

To advance this from prototype to a usable, honest capability — and only then to remove the "future"
labels — the following concrete evidence is required (mirrors
[`Known-Limitations.md`](Known-Limitations.md) §1):

- A **labelled evaluation dataset** of representative documents/cheques (synthetic or
  consent-cleared; no PII in the repo) with a documented **dataset version**.
- A **trained CV/OCR model** registered via `ModelConfiguration`, served from the out-of-process
  Python service, with a recorded **model version**.
- A **measured accuracy benchmark** on that dataset — precision/recall for OCR field reading and for
  signature detection, reported with the dataset version, **not** a single headline number.
- A **governed evaluation result** the registration references before `IsEnabled` is set true.
- Confirmation that the **human-review gate** still fires for everything not cleanly low-risk, and that
  every assessment still carries evidence, the image hash, and a limitation note.
- An explicit **production-readiness sign-off**; until all of the above exist, treat the module as
  **prototype only**.

See the evaluation plans:
[`Cheque-OCR-Signature-Risk-Evaluation-Plan.md`](Cheque-OCR-Signature-Risk-Evaluation-Plan.md) and
[`Cheque-OCR-and-Forgery-Detection-Research-Notes.md`](Cheque-OCR-and-Forgery-Detection-Research-Notes.md).
