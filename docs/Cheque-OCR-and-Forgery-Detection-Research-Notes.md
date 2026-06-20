# Cheque OCR & Signature-Forgery / Document-Fraud — Research Notes & Limitations

This note governs the **Cheque OCR, Signature Forgery Detection and Document Fraud** and
**Python OCR and Computer Vision Deployment** knowledge categories, and any future ChequeXpert/Parascript-style
OCR work. It is research awareness and engineering discipline — **not** a fraud-proof claim.

## Hard limitations (non-negotiable)

- **No automatic fraud proof.** The system never asserts that a cheque or signature is fraudulent; it produces
  a **risk signal with a confidence score and evidence**.
- **No legally conclusive forgery detection.** Output is **not** legally admissible proof of forgery. Legal
  admissibility requires due process and qualified examiners.
- **Human review required for high-risk decisions.** High-risk or high-value items are escalated to a human
  reviewer; the model assists, it does not decide.
- **Detection ≠ verification.** *Signature detection* (is there a signature, where) is distinct from
  *signature verification* (does it match a reference) and from *forgery risk*. These are separate steps with
  separate error profiles.
- **False positives and false negatives are always disclosed.** No accuracy percentage is asserted; accuracy
  is dataset- and pipeline-dependent and must be measured on a representative, governed validation set.
- **Privacy / data protection.** Cheques contain personal and financial data; handling follows data-protection
  obligations (see `src-dpo-mu`). Retention, access control, and audit apply.

## Pipeline concepts (awareness)

- **Image ingestion & quality**: TIFF/scanned handling, 200/300 dpi considerations, grayscale/binarization,
  skew correction, noise removal — quality gates precede recognition.
- **Field recognition**: courtesy amount (CAR) and legal amount (LAR) recognition with a **CAR/LAR consistency
  check**; MICR line handling; date and payee extraction; handwriting-recognition limitations.
- **Signature analysis**: writer-independent vs writer-dependent verification; genuine vs **skilled** forgery
  vs **random** forgery (skilled forgery is the hard case); feature extraction; metric-learning / Siamese-network
  concepts; CNN-based document image analysis; anomaly detection — all **probabilistic**.
- **Decisioning**: confidence scoring, thresholds, human-in-the-loop workflow, and an **audit trail for fraud
  decisions** (who saw what, when, with what evidence).

## Evaluation & data discipline

- Governed **dataset labeling strategy**; representative validation set; report precision/recall and both error
  types; monitor drift. No fixed accuracy is claimed without measurement on a governed set.

## Imaging-rigor analogy (and its limits)

Medical-grade imaging QA (traceability, reproducibility, image-quality checks, calibration mindset, validation
datasets, auditability) is a useful **rigor analogy**. But **2D document scanning is a different domain from
3D/medical imaging**, and **nothing here is medical diagnosis**. Any 3D/medical-imaging concept is conceptual
and **non-diagnostic**; the platform must never claim "medical-level" capability.

## Attribution

OCR concepts → `src-research-ocr`; signature/forgery/CV concepts → `src-research-cv-signature`; privacy →
`src-dpo-mu`. Research sources are **topic families requiring verification**, not specific papers; no accuracy
figures or citations are asserted.

> Bottom line: build cheque OCR and forgery-risk tooling that is **evidence-based, confidence-scored,
> human-reviewed, privacy-aware, and auditable** — never a black-box "fraud detector".
