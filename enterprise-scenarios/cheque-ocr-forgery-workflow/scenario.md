# Scenario: Meridian Commercial Bank — Scanned-Cheque OCR & Forgery-Risk Triage with Mandatory Human Review

> **Synthetic scenario.** Meridian Commercial Bank is a fictional institution invented for the
> LocalAIFactory enterprise capability simulation suite. This scenario is *inspired by* the general
> category of cheque-image OCR and signature/forgery-risk triage tooling (the kind of direction
> associated with ChequeXpert/Parascript-style workflows). It is **not** a clone of, compatible with,
> equivalent to, or derived from any vendor product, manual, model, or data format. No vendor
> certification, compatibility, or equivalence is implied.
>
> **Critical discipline — read first.** Nothing in this scenario detects fraud with certainty. The
> system produces a *risk signal* over recognised fields and a signature region. It is **never
> fraud-proof, never legally conclusive, and never a sole basis for dishonouring an instrument or
> accusing a customer.** Every high-risk item is routed to a trained human reviewer who makes the
> decision. False positives and false negatives are expected, disclosed, and measured. No accuracy
> percentage is asserted anywhere in this scenario; numbers, if any, would come only from a governed
> validation set under defined conditions.

---

## Business Problem

Meridian Commercial Bank processes a daily inflow of scanned paper cheques captured at branch
counters, ATMs, and a back-office bulk scanner. Today the cheque clearing team keys core fields by
hand and eyeballs signatures against on-file specimens. The consequences are concrete:

- Manual keying of the **payee name, courtesy amount (CAR — the numeric box) and legal amount (LAR —
  the written-words line), date, and MICR line** is slow and error-prone, especially at volume and
  near cut-off.
- Signature comparison is inconsistent: outcomes depend on which clerk looks, how tired they are, and
  how good the scan is. There is no recorded, reviewable evidence for why an item was passed or held.
- Genuine fraud and simple alteration both slip through, while legitimate cheques are sometimes held,
  annoying customers — and there is no audit trail explaining either decision.
- Poor-quality scans (skew, low contrast, folds, ink bleed, stamps over fields) are handled ad hoc.

The bank wants a **local-first** workflow that (a) extracts cheque fields with confidence scores, (b)
raises a **risk signal** on amount mismatch and signature anomaly, and (c) **forces a human review
gate** for anything risky or low-confidence — with a complete, auditable evidence record behind every
decision. The bank explicitly does **not** want an autonomous "fraud detector"; it wants a triage and
evidence-capture aid that makes its reviewers faster and its decisions defensible.

---

## Current-State Process

1. Branch/ATM/back-office scanner captures a cheque image; images land in a shared folder per batch.
2. A clearing clerk opens each image, reads it, and keys payee, amount, date, and account/MICR data
   into the core banking capture screen.
3. The clerk visually compares the signature to a specimen pulled from the customer record.
4. Borderline items are escalated informally (a shout across the desk, an email) to a senior clerk.
5. Decisions to pay or hold are recorded only as a status flag; the *reasoning and evidence* are not
   captured. Disputes later cannot reconstruct what was seen or why.

## Target-State Process

1. Images are ingested into an **image pipeline** (de-skew, crop to field regions, quality scoring).
2. A **Python OCR service** recognises CAR, LAR, date, payee, and MICR fields, each with a per-field
   confidence score, and returns the cropped evidence regions.
3. A **risk-scoring** step computes signals: CAR-vs-LAR amount agreement, MICR plausibility, date
   validity (stale/post-dated), and a **signature-anomaly score** against the on-file specimen.
4. A **.NET workflow** applies bank-defined thresholds to route each item to one of: *auto-eligible
   (low risk, high confidence)* → still subject to sampling; *human review (any high-risk or
   low-confidence signal)*; *hard-stop (unreadable / pipeline failure)*.
5. **Every high-risk and every low-confidence item goes to a trained human reviewer**, who sees the
   image, the extracted fields, the cropped evidence, the signals, and the specimen — and records a
   decision **with a reason**. The human decision is authoritative; the score never auto-dishonours.
6. The decision, the evidence shown, the signals, the thresholds in force, and the reviewer identity
   are written to an **append-only audit trail**.

---

## Users and Roles (including human reviewers)

- **Clearing Clerk** — handles routine items, confirms or corrects OCR fields, escalates anything the
  workflow flags. Cannot finalise a high-risk item alone.
- **Forgery-Risk Reviewer (human-in-the-loop)** — trained reviewer who adjudicates flagged items,
  compares signature against specimen, and records a **pay / hold / return** decision with a written
  reason and the evidence considered. **The mandatory human gate for all high-risk items.**
- **Senior Reviewer / Maker-Checker** — second sign-off for the highest-risk band and for overrides of
  the workflow's recommendation; enforces maker/checker separation.
- **Operations Supervisor** — manages batches, reassigns work, monitors SLA/queue depth; cannot alter
  audit records.
- **Risk & Compliance Officer** — reads audit trail and reporting; reviews FP/FN trends; cannot change
  decisions retroactively.
- **System Administrator** — manages thresholds, model versions, and access; all changes audited.

No role can mark an item "fraud confirmed" as a system fact — the system records *human decisions and
the evidence behind them*, never a legal conclusion.

---

## Data Entities (illustrative)

- **ChequeImage** — scanned image reference, capture channel, batch, quality score, skew/contrast
  metrics. Image bytes treated as sensitive; access controlled and logged.
- **ExtractedField** — field type (CAR, LAR, payee, date), recognised value, **per-field confidence**,
  cropped evidence region reference.
- **CourtesyAmount (CAR)** / **LegalAmount (LAR)** — numeric box value and written-words value, plus a
  computed **agreement signal** (do they reconcile?).
- **MicrLine** — recognised MICR fields (routing/account/serial as applicable), plausibility flags.
- **SignatureSpecimen** — on-file reference signature(s) for the account (sensitive biometric-adjacent
  data; strict access control).
- **SignatureComparison** — cropped drawn signature, **anomaly score**, and the specimen(s) compared
  against. An anomaly score is a *signal*, not a verdict.
- **RiskScore** — composite risk band derived from signals + thresholds in force, with the contributing
  factors recorded for explainability.
- **ReviewDecision** — reviewer identity, decision (pay/hold/return), free-text reason, evidence shown,
  timestamp, model/threshold version. Authoritative and append-only.

---

## Integrations

- **Image capture sources** — branch scanners, ATM capture, back-office bulk scanner (batch folders or
  a capture API). Inspired-by only; no vendor format guaranteed.
- **Core banking / customer record** — to pull the on-file signature specimen and account status, and
  to post the final human decision. Read of specimen is logged.
- **Case management / clearing system** — to hand off pay/hold/return outcomes for settlement.
- **Identity provider** — Windows/AD authentication for reviewers, mapped to deny-by-default RBAC.
- **Local model runtime (optional)** — OCR/scoring models run locally; the system must degrade
  gracefully (route to manual) when the model service is unavailable. No internet dependency.

---

## Security and Audit Controls

- **Append-only audit trail for every fraud-risk decision** — who reviewed, what evidence was shown,
  which signals and thresholds applied, the decision, the reason, model/threshold version, timestamp.
  Records are immutable; corrections are new entries, never overwrites.
- **Deny-by-default RBAC + IDOR scoping** — a reviewer can only open items and specimens for accounts
  in their assigned queue; specimen access is logged.
- **Privacy / data protection** — cheque images, signatures, and account data are sensitive personal
  data. Apply data minimisation, encryption at rest and in transit, strict retention limits, masking
  in non-production, and access logging. Signature specimens are treated as especially sensitive.
- **Maker/checker** — high-risk-band decisions and any override of the workflow recommendation require
  a second human sign-off.
- **No autonomous adverse action** — the system never auto-dishonours a cheque or labels a customer
  fraudulent; it can only *recommend a human review* and *record* the human outcome.

---

## Reporting Requirements

- Queue depth, SLA, and throughput by channel and batch.
- **False-positive / false-negative trend** against the governed validation set and against
  post-review ground truth, reported as counts and rates over time — with explicit statement of the
  validation conditions. **No single headline accuracy percentage is published as a capability claim.**
- Reviewer workload and override rate (how often humans disagree with the recommendation).
- Evidence-completeness report: proportion of decisions with full recorded evidence (target 100%).
- Drift watch: confidence-score and anomaly-score distribution shift over time, to flag model staleness.

---

## Failure Modes

- **False positives** — genuine cheque flagged high-risk; cost is customer friction and reviewer load.
  Mitigation: human gate, threshold tuning, FP rate monitored and reported.
- **False negatives** — altered/forged cheque scored low-risk; cost is loss. Mitigation: sampling of
  auto-eligible items, conservative thresholds, never treating "low risk" as "verified genuine."
- **Poor scans** — skew, low contrast, folds, stamps over fields, ink bleed → low OCR confidence.
  Mitigation: quality gate routes poor scans to manual; never guess a field silently.
- **CAR/LAR mismatch ambiguity** — numeric and written amounts disagree; always a human decision.
- **Specimen gaps** — no/poor on-file specimen → signature anomaly score is unreliable; route to manual
  and disclose the limitation rather than scoring confidently.
- **Model unavailable** — OCR/scoring service down → fail safe to full manual processing, never block.
- **Adversarial alteration** — sophisticated forgery may defeat any signal; this is disclosed, not
  hidden, and is precisely why the human gate exists.

---

## Acceptance Criteria

See `acceptance-criteria.md` for the measurable checklist. In summary, the solution is acceptable only
if: every high-risk and low-confidence item passes through a **mandatory human review gate**; every
decision records **confidence and evidence**; the audit trail is **append-only and complete**; FP/FN
are **measured and disclosed** on a governed validation set; privacy controls are enforced; and **no
accuracy percentage and no legal/fraud conclusion is asserted by the system**.

---

## Expected Architecture

```
Capture (branch / ATM / bulk scanner)
        │  scanned images (batch)
        ▼
Image Pipeline  ── de-skew, field-region crop, quality scoring ──► quality gate
        │                                                   (poor scan → manual)
        ▼
Python OCR Service  ── CAR, LAR, payee, date, MICR + per-field confidence + evidence crops
        │
        ▼
Risk Scoring  ── CAR/LAR agreement, MICR plausibility, date validity, signature-anomaly score
        │                         (signals only — never a verdict)
        ▼
.NET Workflow  ── apply bank thresholds → route:
        ├─ auto-eligible (low risk + high confidence) → still sampled
        ├─ HUMAN REVIEW  (any high-risk OR low-confidence)  ◄── mandatory gate
        └─ hard-stop (unreadable / pipeline failure) → manual
        │
        ▼
Human-in-the-loop review UI  ── image + fields + evidence crops + signals + specimen
        │  reviewer records pay/hold/return + reason
        ▼
Append-only Audit Trail  +  hand-off to clearing/case management
```

- **Python OCR/scoring service** (the recognition + signal engine) is separated from the **.NET
  workflow/orchestration** (queues, thresholds, RBAC, audit, review UI), mapping cleanly onto the
  LocalAIFactory Core/Data/Web/Agent layering and MSSQL-only operability.
- Signals are persisted with their contributing factors so a human can see *why* an item was flagged.

---

## Expected Tests

- **Field recognition** on a **governed validation set**: CAR, LAR, date, payee, MICR — reported as
  precision/recall **per field**, with the validation conditions stated. Not a single headline number.
- **CAR/LAR agreement** logic: matched, mismatched, and ambiguous cases all behave correctly.
- **Risk routing**: every high-risk and low-confidence item is routed to the human gate; none bypass.
- **Signature anomaly**: behaviour with strong specimen, weak specimen, and missing specimen — must
  degrade to "route to manual + disclose" rather than over-confident scoring.
- **FP/FN measurement**: precision/recall on the validation set computed and reported honestly.
- **Quality gate**: poor scans routed to manual, never silently guessed.
- **Audit completeness**: 100% of decisions carry full evidence and reviewer identity.
- **Fail-safe**: model service down → full manual processing, no blocked queue, no adverse auto-action.

---

## Expected Deployment Concerns

- **CPU vs GPU** — OCR and signature scoring may run CPU-only (slower, higher latency) or GPU-assisted.
  The bank's local-first, possibly GPU-less estate means the design must function on CPU and treat GPU
  as an optional accelerator, not a requirement.
- **Local-first** — no internet dependency; images and specimens never leave the bank's environment.
- **Throughput vs latency** — batch back-office volumes vs near-real-time branch capture; queue sizing
  and reviewer staffing must match.
- **Model/threshold versioning** — versions recorded on every decision so historic decisions remain
  explainable after a model update.
- **Sensitive data handling** — encrypted storage, retention limits, masked non-prod data.

## Rollback Considerations

- The workflow must be reversible to **full manual processing** at any time without data loss —
  manual is always the safe fallback, since the system only ever *assists* a human.
- Threshold and model changes are versioned; a regression can be rolled back to the prior version, and
  prior decisions remain explainable because the version was recorded.
- Because the system never takes autonomous adverse action, a rollback cannot have wrongly dishonoured
  cheques on its own — the human gate bounds the blast radius.

---

## CEO/CTO Summary

Meridian wants to clear scanned cheques faster and make every pay/hold/return decision **defensible**,
not to replace human judgement. The proposed design extracts cheque fields with confidence scores and
raises a **risk signal** on amount mismatch and signature anomaly — then **forces every risky or
unclear item through a trained human reviewer** who decides, with the full evidence recorded in an
append-only audit trail. It runs **local-first**, works **CPU-only**, and **fails safe to manual**.

The honest boundary: this is a **triage and evidence-capture aid, not a fraud detector**. It is **never
fraud-proof and never legally conclusive**; false positives and false negatives are expected, measured
on a governed validation set, and disclosed; **no accuracy percentage is claimed**; and the system
**never** auto-dishonours a cheque or labels a customer fraudulent. Within those bounds it makes
reviewers faster and the bank's cheque decisions auditable and consistent.
