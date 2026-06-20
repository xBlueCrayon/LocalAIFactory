# Cheque OCR / Signature-Risk Evaluation Plan

> **Honesty banner.** This plan describes how to evaluate cheque OCR and signature-risk capabilities
> **honestly**. There is **no trained CV model yet**, so today there are **no measured accuracy numbers** —
> the deterministic prototypes have *behavioural* tests (`tests/.../CapabilityPrototypeTests.cs`) but not
> field-accuracy metrics. This plan defines the governed process to produce honest metrics **once a CV
> model exists**. It deliberately refuses a single "accuracy" headline. It never treats forgery risk as a
> fraud verdict. Signature **detection** and **verification** are measured **separately**.

---

## 1. What we are (and are not) evaluating

We evaluate, separately:
- **OCR field reading** — CAR, LAR, MICR, date, payee (`ChequeOcrResult.OcrField` values + confidence).
- **Document classification** — text-based / scanned / mixed (`PdfAnalyzer`) and future class labels.
- **Signature detection** — present? region localised? (`SignatureAnalysis.SignaturePresent`, `RegionDetected`).
- **Signature verification / forgery risk** — `VerificationScore`, `ForgeryRiskScore` against a reference.
- **Triage quality** — does `ChequeRiskEngine` route items to Accept/Review/Reject appropriately?
- **Human-review gate** — is every risky item actually sent to a human?

We are **not** evaluating "fraud detection accuracy" or "forgery proof" — those are not outputs the
platform produces. There is no Accept-as-genuine legal determination to score.

---

## 2. Governed labelled dataset

Evaluation uses the governed templates under `datasets/` — **templates only, no real data committed**
(see `datasets/README.md`). A real evaluation set is assembled, governed, and stored **outside** source
control.

- `datasets/cheque-ocr-evaluation-template.csv` — one row per cheque sample:
  ground truth (`gt_courtesy_amount`, `gt_legal_amount`, `gt_micr`, `gt_date`, `gt_payee`,
  `gt_signature_present`, `gt_signature_class` ∈ {genuine, skilled_forgery, ...}), `image_quality`,
  predictions (`pred_*`, `pred_forgery_risk`), `triage`, `human_review_required`, `reviewer_decision`.
- `datasets/document-classification-template.csv` — one row per document: `gt_class`, `page_count`,
  `gt_text_or_scanned`, `document_sha256`, `pred_class`, `pred_confidence`, `extractable_text`, `ocr_required`.

**Labelling rules:**
- Ground truth is **human-labelled** under a governed process; labellers are not the model authors.
- Samples are tracked by `sample_id` / `document_id` and referenced by hash, never by stored image bytes.
- Forgery class labels (`genuine` / `skilled_forgery` / etc.) require documented provenance for each sample.

---

## 3. Metrics — reported per field/class, never as one headline

There is **no single "accuracy" number.** Report, per field and per class:

**Per OCR field (CAR, LAR, MICR, date, payee):**
- Exact-match read rate, character error rate (CER) where applicable.
- **Precision / recall**, false-positive and false-negative counts.
- Read rate vs. confidence calibration (is `OcrField.Confidence` actually predictive?).
- Breakdown by `image_quality` (good / fair / poor) and DPI.

**Document classification:**
- Per-class precision/recall and a full confusion matrix (text / scanned / mixed; invoice / contract / …).

**Signature detection (separate report):**
- Precision/recall and FP/FN for "signature present" and "region detected".

**Signature verification / forgery risk (separate report):**
- Precision/recall and FP/FN of forgery-risk flagging at each threshold.
- ROC / detection-error-tradeoff over `ForgeryRiskScore`.
- Reported **independently** from detection — they are different questions with different error costs.

**Triage quality:**
- Confusion of `triage` (Accept/Review/Reject) vs. the human `reviewer_decision`.
- Specifically the **false-Accept rate** (risky item triaged Accept) — the most safety-critical error.

---

## 4. Thresholds (current deterministic defaults)

The triage engine's thresholds are explicit and auditable (`ChequeRiskEngine`):

| Constant | Value | Effect |
|---|---|---|
| `LowConfidence` | 0.60 | OCR field below this raises a low-confidence flag |
| `ForgeryConcern` | 0.50 | forgery-risk at/above raises an elevated-risk flag |
| `ForgeryHigh` | 0.80 | forgery-risk at/above forces `Reject` |
| `VerificationFloor` | 0.50 | verification below this (with a reference) raises a flag |
| `HighValue` | 10000 | high-value item flag (forces review) |

Evaluation must **sweep these thresholds** and report the precision/recall trade-off at each, rather than
fixing a single operating point and claiming it is "accurate". Thresholds are tuned to minimise
false-Accept on risky items, accepting more Review load as the cost.

---

## 5. Detection vs. verification — measured separately

Because `SignatureAnalysis` separates `SignaturePresent`/`RegionDetected` (detection) from
`VerificationScore`/`ForgeryRiskScore` (verification), the evaluation produces **two independent reports**.
A model may detect well but verify poorly; collapsing them into one number would hide that. We never report
a combined "signature accuracy".

---

## 6. Human-review gate — measured

The gate is itself evaluated:
- **Gate recall:** of all items a human ultimately judged risky, what fraction had
  `HumanReviewRequired = true`? Target: **100%** — a missed gate is a critical defect.
- **Review load:** fraction of items routed to Review (the operational cost of caution).
- **Reviewer agreement:** triage vs. `reviewer_decision` agreement, and reviewer-to-reviewer agreement
  on a shared subset.

---

## 7. Privacy and governance

- No real cheque images, signatures, or PII in source control — templates carry headers + synthetic rows.
- Real evaluation data is governed, access-controlled, and referenced by hash.
- Results record **model version + dataset version** together so a metric is reproducible and a triage
  decision can be traced to the exact model that produced it.

---

## 8. Hard limitations (state plainly in any report)

1. **No measured accuracy exists today** — no trained CV model is deployed; numbers here are a plan.
2. Accuracy is **dataset-dependent**; a metric is valid only for the governed set it was measured on.
3. Forgery output is a **risk score with a human-review gate**, never a fraud verdict or forgery proof.
4. Results are **probabilistic** with real false-positive/false-negative rates; report them, never hide them.
5. Detection and verification are different capabilities with different error profiles — never merged.
6. This is 2D document analysis — **not** medical imaging and **not** diagnosis.
