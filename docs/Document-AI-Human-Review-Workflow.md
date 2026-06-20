# Document-AI Human-Review Workflow

> **Honesty banner.** Cheque and document AI on this platform is **decision-support, not decision-making**.
> No model output is a fraud verdict, a forgery proof, or a legally conclusive finding. High-risk items are
> **never auto-decided** — they are routed to a human, who sees the evidence, the image hash, and the
> provenance, and whose decision is recorded. This workflow is mandatory and enforced by the **types**, not
> just by policy: `ChequeRiskAssessment.HumanReviewRequired` and the absence of any "Fraud"/"Genuine" state.

---

## 1. Principles

1. **Human-in-the-loop is mandatory** for any item not cleanly low-risk.
2. **The model triages; the human decides.** `ChequeTriage` is `Accept / Review / Reject` — a recommendation.
3. **Detection ≠ verification.** Reviewers are shown both, separately, and told which is which.
4. **Everything is auditable** — evidence, image hash, provenance, and the human decision are stored.
5. **Never auto-decide high-risk.** A `Reject` triage or any risk flag forces a human.
6. **Graceful degradation** — if the CV service is absent, OCR/signature fields are `null`/`not assessed`,
   which raises flags and forces review (fail-safe, not fail-open).

---

## 2. Routing by confidence and risk

`ChequeRiskEngine.Assess(...)` produces `ChequeRiskAssessment(Triage, HumanReviewRequired,
OverallConfidence, RiskFlags, Evidence, LimitationNote)`. Routing:

| Condition | Triage | Routed to |
|---|---|---|
| No flags, all field confidence ≥ 0.60, no forgery concern | `Accept` | proceed (still not a "genuine" determination) |
| Any risk flag (low-confidence OCR, unread field, CAR/declared mismatch, high value, elevated forgery risk) | `Review` | **human reviewer** |
| `ForgeryRiskScore ≥ 0.80` | `Reject` | **human reviewer** (manual-rejection workflow) |

`HumanReviewRequired` is `true` whenever there is any flag or the triage is not `Accept`.
`OverallConfidence` is the **minimum** field confidence relied upon — a deliberately conservative floor.

Even an `Accept` is **not** a legal "genuine" finding; it means "no rule fired", and remains auditable.

---

## 3. What the reviewer sees

The review screen presents, for each item:
- **Image hash (provenance):** the SHA-256 (`PdfAnalysis.DocumentHash`) anchoring the exact bytes reviewed.
- **OCR evidence:** each `OcrField` value + confidence (or "(none)" when unread), from
  `ChequeRiskAssessment.Evidence`.
- **Signature signals — separated:** `SignaturePresent` / `RegionDetected` (detection) shown apart from
  `VerificationScore` / `ForgeryRiskScore` / `ReferenceSpecimenId` (verification), each labelled.
- **Risk flags:** the human-readable `RiskFlags` list (what fired and why).
- **Triage recommendation:** Accept / Review / Reject — labelled clearly as a recommendation.
- **Limitation note:** `LimitationNote` verbatim — triage only, not a fraud determination, probabilistic,
  not legally conclusive.

The reviewer is never shown a "fraud" or "genuine" label, because the platform does not produce one.

---

## 4. Reviewer decision and audit trail

- The reviewer records a decision (e.g. approve / reject / escalate) plus a rationale.
- The decision is stored **append-only** alongside the image hash, model version, dataset/threshold
  version, the full `Evidence` and `RiskFlags`, and a timestamp + reviewer identity.
- The audit record reproduces *why* the item was routed and *who* decided — so any outcome is traceable to
  the exact model and inputs that produced the recommendation.
- Audit records are never edited or deleted; corrections are new appended entries.

---

## 5. Separation of detection and verification (reviewer-facing)

Reviewers must not conflate "a signature is present" with "the signature is genuine":
- **Detection** answers *is something there / where* — `SignaturePresent`, `RegionDetected`.
- **Verification** answers *does it match a reference* — `VerificationScore` vs `ReferenceSpecimenId`,
  and the probabilistic `ForgeryRiskScore`.

The UI keeps these in separate, labelled panels. A high detection confidence with no verification reference
(`ReferenceSpecimenId == null`, `VerificationScore == null`) is explicitly shown as "verification: not
assessed" — never silently treated as genuine.

---

## 6. Never auto-decide high-risk

- `Reject` triage and any forgery/risk flag **always** require a human; there is no code path that finalises
  a high-risk item without a recorded human decision.
- Disabling the human gate is not a configuration option.
- If inputs are missing (CV service down, fields unread), the engine flags and routes to Review rather than
  guessing — fail-safe.

---

## 7. Legal-admissibility caution

- Model outputs are **investigative decision-support**, not evidence of fraud or forgery.
- A `Reject` triage or elevated `ForgeryRiskScore` is **not** a forgery finding and **must not** be
  represented as legally conclusive in any downstream process or communication.
- Any legal/fraud determination is made by authorised humans through the bank's own governed processes,
  on the documented evidence — not by this platform.
- The image hash supports provenance/tamper-evidence of *what was analysed*; it does **not** establish the
  authenticity of the underlying instrument.
- This is 2D document/cheque analysis — **not** medical imaging and **not** diagnosis. Accuracy is
  dataset-dependent and, until a CV model is evaluated, **unmeasured**.
