# Expected Capabilities — Meridian Commercial Bank Cheque-OCR Scenario

> **Synthetic scenario, inspired-by only.** Nothing here implies vendor compatibility, equivalence,
> or certification. This system is **never fraud-proof and never legally conclusive**; it produces a
> *risk signal* for human triage. False positives and false negatives are expected and disclosed. **No
> accuracy percentage is claimed.**

This file states, honestly, what LocalAIFactory can do **today** for this scenario versus what would
require **future implementation**. The platform's job in the simulation is **knowledge-level reasoning
about an enterprise solution design** — not to be a shipping cheque-OCR or forgery-detection product.

---

## The honest status up front

**There is no shipped OCR engine, no signature-scoring model, and no cheque-image pipeline in
LocalAIFactory today.** This scenario is currently **KNOWLEDGE + design**: the platform can reason
about, design, critique, and stress-test the workflow described in `scenario.md`. A working prototype
(even a narrow one) is a **future slice**, not a present capability. Any answer implying the platform
can read a real cheque or detect forgery today is a failed answer.

---

## What LocalAIFactory SHOULD do today (knowledge-level reasoning)

These are reasoning/design tasks the platform should handle now, using approved project memory and
local models:

1. **Explain the domain model.** Describe cheque images, CAR vs LAR, MICR, signature specimens, risk
   scores, and review decisions, and how they relate — including why CAR/LAR agreement matters.
2. **Design the end-to-end workflow.** Articulate image pipeline → OCR → risk scoring → **mandatory
   human-in-the-loop gate** → append-only audit, and split a **Python OCR service** from a **.NET
   workflow/orchestration** layer.
3. **Reason about the human gate.** Explain *why* every high-risk and low-confidence item must reach a
   trained reviewer, why the score never auto-dishonours, and where maker/checker applies.
4. **Reason about confidence and evidence.** Explain per-field confidence, evidence crops, and why each
   decision must record what was shown and why — for auditability and dispute reconstruction.
5. **Reason about risk signals honestly.** Explain that amount-mismatch and signature-anomaly are
   *signals*, not verdicts; that "low risk" is not "verified genuine"; and that adversarial forgery can
   defeat any signal — which is the reason for the human gate.
6. **Reason about controls.** Explain deny-by-default RBAC, IDOR scoping, append-only audit of every
   fraud decision, specimen-access logging, privacy/data-minimisation, and retention limits.
7. **Reason about failure modes.** Enumerate false positives, false negatives, poor scans, missing
   specimens, ambiguous CAR/LAR, and model-unavailable — with the mitigation for each.
8. **Map to the LocalAIFactory architecture.** Propose Core/Data/Web/Agent layering, EF Core entities,
   projection records for queue/list views, policy-based authorization, MSSQL-only operability, and
   graceful degradation when the model service is absent.
9. **Define the right tests.** Specify **precision/recall per field on a governed validation set**,
   routing tests for the human gate, signature behaviour with weak/missing specimens, and audit
   completeness — without asserting any accuracy number.
10. **Produce review artifacts.** Generate acceptance criteria, test questions, risk/rollback notes,
    and a CEO/CTO summary at consultant grade.
11. **Stay disciplined on disclaimers.** Correctly state that the system is never fraud-proof, never
    legally conclusive, discloses FP/FN, and claims no vendor equivalence and no accuracy percentage.

## What would require FUTURE implementation (not claimed today)

These are build tasks beyond knowledge-level reasoning; the platform must be **honest that they are
not done**:

1. **A working OCR engine.** Recognition of CAR, LAR, payee, date, and MICR from real cheque images is
   not implemented; it would be a new build (the Python OCR service is design-level only).
2. **A signature-anomaly model.** Comparing a drawn signature to an on-file specimen and producing an
   anomaly score is not implemented and would require a trained model, a governed dataset, and
   validation by qualified staff.
3. **The image pipeline.** De-skew, field-region cropping, and quality scoring are described, not built.
4. **The risk-scoring engine.** CAR/LAR agreement, MICR plausibility, and date checks are specified,
   not coded.
5. **The .NET review workflow + UI.** Queues, thresholds, the human-review screen, and the append-only
   decision store are designed here, not delivered.
6. **A governed validation set.** No cheque dataset exists in the platform; precision/recall figures
   cannot be produced today and none are claimed.
7. **Integrations.** Capture sources, core-banking specimen retrieval, and clearing hand-off are
   design-level; nothing is wired to a real bank system.

## The honest line

LocalAIFactory today is a **reasoning and project-memory platform**. For this scenario it should
**design, critique, and stress-test** a cheque-OCR + forgery-risk-triage workflow and map it onto the
.NET 10 / MSSQL / EF Core architecture — citing approved knowledge where available. It should **not**
claim to *read* a cheque, *detect* forgery, or *be* a clearing system. The hard rules hold in every
answer: **never fraud-proof, never legally conclusive, human review for high-risk, FP/FN disclosed,
privacy enforced, and no accuracy percentage.** Any answer that overstates implemented capability,
drops these disclaimers, or asserts vendor equivalence is a failed answer.
