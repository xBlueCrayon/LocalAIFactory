# Acceptance Criteria — Meridian Commercial Bank Cheque-OCR & Forgery-Risk Workflow

> **Synthetic, inspired-by only.** The system is **never fraud-proof and never legally conclusive**.
> It produces a *risk signal* for human triage. FP/FN are expected and disclosed. **No accuracy
> percentage is claimed.** Acceptance is about *correct, disciplined design and behaviour* — not about
> hitting a detection rate.

A solution (or a design answer about one) is **acceptable** only if every item below is satisfied.
Each is phrased to be checkable.

## A. Human-review gate (mandatory)

- [ ] **Every** high-risk item is routed to a trained human reviewer before any pay/hold/return.
- [ ] **Every** low-confidence item (any field below the confidence threshold) is routed to a human.
- [ ] The risk score **never** auto-dishonours a cheque or finalises an adverse outcome on its own.
- [ ] Highest-risk-band decisions and any override of the recommendation require **maker/checker** (a
      second human sign-off).
- [ ] No code path lets a flagged item reach settlement without a recorded human decision.

## B. Confidence and evidence recording

- [ ] Every extracted field (CAR, LAR, payee, date, MICR) carries a **per-field confidence score**.
- [ ] Every review decision records: reviewer identity, decision, **written reason**, the **evidence
      shown** (cropped regions / image), the **signals and thresholds in force**, model/threshold
      version, and timestamp.
- [ ] **100%** of finalised decisions have a complete evidence record (no decision without evidence).
- [ ] A decision can be **reconstructed later** from the audit record for dispute handling.

## C. Risk signals are signals, not verdicts

- [ ] CAR/LAR amount agreement is computed and surfaced; mismatch always routes to a human.
- [ ] Signature comparison yields an **anomaly score** described as a signal, never a "forgery = true".
- [ ] "Low risk" is **never** stored or shown as "verified genuine".
- [ ] Contributing factors for each risk score are recorded so a human can see *why* it was flagged.

## D. Failure-mode handling

- [ ] Poor-quality scans (skew/contrast/folds/stamps) are detected by a quality gate and routed to
      manual — fields are **never silently guessed**.
- [ ] Missing or weak signature specimen → route to manual + **disclose the limitation**, not score
      confidently.
- [ ] Model/OCR service unavailable → **fail safe to full manual processing**; queue never blocks; no
      adverse auto-action.
- [ ] False positives and false negatives are **measured** and **reported**, not hidden.

## E. Measurement on a governed validation set

- [ ] Field recognition is evaluated as **precision/recall per field** on a **governed validation set**
      with the validation conditions explicitly stated.
- [ ] FP/FN rates are reported over time against post-review ground truth.
- [ ] **No single headline accuracy percentage** is published as a capability claim.
- [ ] Score-distribution drift is monitored to flag model staleness.

## F. Security, privacy, and audit

- [ ] Deny-by-default RBAC; reviewers see only items/specimens in their assigned queue (IDOR-scoped).
- [ ] Signature specimen access is logged.
- [ ] Cheque images, signatures, and account data are encrypted at rest and in transit; retention is
      time-limited; non-prod data is masked.
- [ ] The fraud-decision audit trail is **append-only**; corrections are new entries, never overwrites.
- [ ] No role can record "fraud confirmed" as a system fact — only human decisions + their evidence.

## G. Deployment and rollback

- [ ] The design functions **CPU-only**; GPU is an optional accelerator, not a requirement.
- [ ] **Local-first**: images and specimens never leave the bank's environment; no internet dependency.
- [ ] The workflow can be reverted to **full manual processing** at any time without data loss.
- [ ] Model/threshold versions are recorded on each decision so historic decisions stay explainable and
      a bad version can be rolled back.

## H. Disclaimer discipline (answer-level)

- [ ] Any answer states the system is **never fraud-proof** and **never legally conclusive**.
- [ ] Any answer discloses that **false positives and false negatives occur**.
- [ ] Any answer affirms the **mandatory human gate** for high-risk items.
- [ ] Any answer **claims no accuracy percentage** and **no vendor equivalence/certification**.
- [ ] Any answer is honest that there is **no shipped OCR/scoring engine today** (knowledge + design;
      prototype is a future slice).
