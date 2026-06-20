# Evaluation Datasets (templates)

Structure for the **governed evaluation datasets** that any CNN/OCR/document-AI capability must be measured
against before it is trusted. These are **templates only** — no real cheque images, signatures, or personal
data are committed (privacy + data protection). A real evaluation dataset is assembled separately, governed,
and never source-controlled here.

## Files

- `cheque-ocr-evaluation-template.csv` — one row per cheque sample: ground-truth fields (CAR/LAR/MICR/date/payee),
  signature ground truth (present/genuine/forged class), image quality, and the human-review decision. Used to
  compute precision/recall and false-positive/false-negative rates per field and for forgery risk.
- `document-classification-template.csv` — one row per document: ground-truth class, page count, text-vs-scanned,
  and the model's predicted class — used to compute classification accuracy/confusion.

## Rules

- **No real PII/images in the repo.** Templates carry headers + synthetic example rows only.
- **Ground truth is human-labelled** under a governed labelling process (`Cheque-OCR-Signature-Risk-Evaluation-Plan.md`).
- **Metrics are reported honestly** — precision, recall, FP/FN per class; no single "accuracy" headline.
- Forgery is a **risk score with a human-review gate**, never a fraud verdict; evaluation measures triage
  quality, not "fraud detection accuracy".
