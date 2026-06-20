# Scenario: Model Risk Review for a Financial-Market Forecasting Model

> **Fictional, synthetic scenario.** No real institution, product, or certification is referenced.
> This scenario exercises LocalAIFactory's ability to reason about **model governance and model
> risk** — it does **not** make markets predictions and does **not** give trading or investment
> advice. **Analysis is not financial advice.**

The fictional firm is **Northwind Treasury**, the in-house treasury and market-risk function of a
mid-size bank. A quant team has built **"Helios"**, a daily short-horizon forecasting model for a
basket of fixed-income and FX instruments. Before Helios may influence any limit, hedge ratio, or
report, the **Model Risk Management (MRM)** team must independently review it.

---

## Business Problem

Northwind's quant desk ships forecasting models faster than MRM can review them. Reviews live in
scattered spreadsheets and email threads; there is no single inventory, no consistent record of
which datasets and backtests a model was validated against, and no enforced sign-off trail. When a
model misbehaves in production (e.g. a regime change degrades accuracy), the team cannot quickly
answer: *which version is live, who approved it, against what evidence, and what were the stated
uncertainty bounds?*

The goal is a **model-risk register and validation workflow** that captures every model, its
datasets, its backtests, its validation findings, and an append-only sign-off trail — so that risk
decisions rest on **documented evidence and disclosed uncertainty**, never on an unaudited claim of
accuracy. The system **assesses model risk**; it never asserts that a model will be profitable.

---

## Current-State Process

- Models tracked in a shared spreadsheet that drifts out of date.
- Backtests run ad hoc on the quant's laptop; results pasted into slides, rarely reproducible.
- Validation is a free-text email from a validator to the desk; no standard checklist.
- "Sign-off" is an email reply ("looks fine to me"), with no immutable record.
- Uncertainty is rarely quantified; point forecasts are presented as if exact.
- No enforced check for look-ahead bias, leakage, or walk-forward validity.
- When asked "is Helios still valid?", nobody can reconstruct the original evidence.

## Target-State Process

- Every model is registered in a **model inventory** with owner, purpose, and tier.
- Each model version is bound to the **exact datasets** and **backtest runs** used to evaluate it.
- Validators work from a **standard validation checklist** (leakage, look-ahead, walk-forward,
  overfitting, regime-sensitivity) and record **structured findings** with severity.
- Forecasts carry **mandatory uncertainty disclosure** (interval / distribution, not a bare point).
- A **risk committee** records an explicit decision (approve / approve-with-conditions / reject)
  in an **append-only sign-off trail**.
- No model version reaches "approved" status without passing required validation evidence.
- The register answers, at any time: which version is live, on what evidence, with what stated
  limitations and uncertainty.

---

## Users and Roles

| Role | Responsibility | Can do | Cannot do |
|---|---|---|---|
| **Model Owner** (quant desk) | Builds and documents the model; submits for validation | Register model, attach datasets/backtests, request review | Approve own model; alter validation findings or sign-offs |
| **Validator** (independent MRM) | Independently challenges the model | Record validation findings, run/flag required tests, set evidence status | Approve for committee; edit the owner's submission |
| **Risk Committee** (chair + members) | Governance decision and accountability | Record approve / conditional / reject decision in sign-off trail | Modify validation findings or backtest data after the fact |
| **Auditor** (read-only) | Reconstructs the decision trail | Read full history, export evidence | Change anything |

**Segregation of duties is mandatory:** the model owner may never validate or approve their own
model; the validator may never sign off at committee level.

---

## Data Entities

- **ModelInventory** — model id, name, owner, business purpose, risk tier (high/medium/low),
  status (draft → submitted → in-validation → approved/conditional/rejected → retired), live flag.
- **ModelVersion** — version label, code/artifact hash, methodology summary, intended use, **stated
  limitations**, declared uncertainty method.
- **Dataset** — identifier, source description, time coverage, point-in-time snapshot reference,
  known caveats (survivorship, vendor revisions). Bound to versions actually used.
- **Backtest** — run id, model version, dataset, window definition (in-sample / out-of-sample /
  walk-forward folds), metrics with **confidence intervals**, embargo/purge settings.
- **ValidationResult** — checklist item, outcome (pass/fail/n-a), severity, evidence reference,
  validator, timestamp. Covers leakage, look-ahead, walk-forward, overfitting, regime sensitivity.
- **SignOff** — append-only committee decision, conditions attached, decided-by, decided-at,
  linked validation evidence, prior-decision pointer (immutable chain).
- **UncertaintyDisclosure** — per forecast/output: interval or distribution, method, assumptions,
  and an explicit "not investment advice" statement.

---

## Integrations

- **MSSQL** — system of record for the register, datasets metadata, backtests, validation, sign-offs.
- **LocalAIFactory knowledge engine** — approved governance policies, validation standards, and
  prior findings injected as curated context.
- **Optional local model (Ollama)** — drafts validation narratives and challenge questions from
  evidence; **degrades gracefully when absent** (templates still render). Never used to produce a
  forecast or a buy/sell signal.
- **Optional Qdrant** — semantic retrieval over prior validation findings; optional, `projectId=0`
  for global governance knowledge.
- No external market-data feed is required to render any page; market data lives as snapshotted
  datasets, not live calls on the request path.

---

## Security and Audit Controls

- **Model governance lifecycle** enforced in state transitions; illegal transitions are rejected.
- **Segregation of duties** enforced server-side (owner ≠ validator ≠ approver).
- **Append-only sign-off trail**: decisions and validation findings are never edited in place;
  corrections are new records that reference the prior one.
- **Deny-by-default** access; auditors are strictly read-only.
- Every state change is recorded with actor, timestamp, and the evidence it relied on.
- No model can be marked **live** without an approved sign-off bound to passing validation evidence.

---

## Reporting Requirements (Uncertainty Disclosure)

- Every forecast output **must** present uncertainty (prediction interval or distribution) and the
  method used; a bare point estimate is non-compliant.
- Every model report carries **stated limitations** and the regimes it was *not* validated for.
- Reports must show backtest metrics **with confidence intervals**, in-sample vs out-of-sample
  side by side, and walk-forward results.
- Every artifact carries the standing disclaimer: **"This is model-risk analysis, not financial
  advice. Past backtest performance does not guarantee future results."**
- The register reports **governance status** (validated / conditional / overdue for revalidation),
  never a profitability claim.

---

## Failure Modes (and how the system guards them)

- **Overfitting** — model fits noise; flagged by out-of-sample degradation and parameter-count vs
  data-size checks; required ValidationResult item.
- **Data leakage** — target or future-derived features bleed into training; required leakage check
  with embargo/purge verification on backtest windows.
- **Look-ahead bias** — using information not available at decision time; point-in-time dataset
  snapshots required; validators verify no future timestamps in features.
- **Regime change** — relationships break when the market regime shifts; revalidation triggers and
  explicit "validated regimes" disclosure; live monitoring of drift.
- **Survivorship / selection bias** — dataset caveats recorded; validator must confirm universe
  construction.
- **Silent staleness** — models past their revalidation date are flagged, not trusted by default.

---

## Acceptance Criteria

1. A model cannot reach **approved/live** without bound datasets, at least one out-of-sample and one
   walk-forward backtest, and a complete validation checklist.
2. **Segregation of duties** is enforced: owner cannot validate or approve own model.
3. The **sign-off trail is append-only**; no edit or delete of past decisions/findings.
4. Every forecast/report includes **uncertainty disclosure** and the **not-advice disclaimer**.
5. Leakage, look-ahead, overfitting, walk-forward, and regime-sensitivity each have an explicit
   pass/fail validation record before approval.
6. The register can reconstruct, for any version: who approved, when, on what evidence, with what
   stated limitations.
7. The system **never** outputs a buy/sell/hold recommendation or a profitability guarantee.

---

## Expected Architecture (model-risk register + validation workflow over MSSQL)

- **MSSQL** as primary store: `ModelInventory`, `ModelVersion`, `Dataset`, `Backtest`,
  `ValidationResult`, `SignOff`, `UncertaintyDisclosure` tables, plus an append-only audit table.
- A **workflow state machine** (draft → submitted → in-validation → decision → live/retired) with
  server-side guards on transitions and roles.
- **Lightweight list queries** (project to records; never materialize large narrative columns in
  lists) consistent with LocalAIFactory's hang-avoidance rules.
- Knowledge engine supplies **approved governance policy** as first-injected context.
- Optional local model assists narrative drafting only; all decisions are human-recorded.
- No external service call on the request path; market data is snapshotted, health is cached.

---

## Expected Tests

- **Backtesting**: in-sample vs out-of-sample split enforced; metrics reported with intervals.
- **Walk-forward / rolling-origin** validation across multiple folds; degradation tracked per fold.
- **Leakage checks**: feature/target temporal separation; embargo and purge applied and verified.
- **Look-ahead checks**: point-in-time snapshot used; no feature references future data.
- **Overfitting checks**: complexity vs sample-size, out-of-sample drop thresholds.
- **Regime-sensitivity**: performance segmented by regime; "validated regimes" recorded.
- **Governance tests**: SoD enforcement, append-only sign-off, no-live-without-evidence.
- **Disclosure tests**: every output carries uncertainty + not-advice disclaimer.

---

## Expected Deployment Concerns

- Runs MSSQL-only; optional Ollama/Qdrant gated and degrade gracefully.
- No live market feed dependency on render; datasets are imported snapshots.
- Migrations additive and backward-compatible; no destructive schema changes without approval.
- Revalidation scheduler/flagging must not block page loads (background, cached state).

## Rollback Considerations

- Because the sign-off trail is **append-only**, "rollback" means recording a new decision that
  supersedes a prior one (e.g. revoke approval), never deleting history.
- Reverting a **live** model to a prior approved version is a new state transition with its own
  sign-off and rationale; the previous live binding remains in the audit trail.
- Dataset/backtest records are immutable once a decision relied on them; a re-run is a **new**
  backtest, not an edit of the old one.

---

## CEO/CTO Summary

Northwind cannot prove, today, why any forecasting model is trusted. This scenario gives MRM a
single **model-risk register** where every model is bound to the exact data and backtests it was
judged on, every validation finding is recorded, and every approval sits in an **append-only
sign-off trail** with enforced segregation of duties. The output is **governance assurance and
disclosed uncertainty**, not a market call: the system tells leadership *whether a model is
properly validated and what its limitations are* — it never tells anyone to trade, and it never
claims a model will make money. **Analysis is not financial advice.**
