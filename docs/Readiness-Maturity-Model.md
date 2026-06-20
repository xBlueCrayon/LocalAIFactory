# Enterprise Readiness Maturity Model

This model defines how LocalAIFactory's maturity is measured and scored. The machine-readable scores live in
[`readiness-scorecard.json`](readiness-scorecard.json) and render in-app at `/Readiness`. This document
defines the **stages**, the **scoring discipline**, and the **path to 100%**.

## Scoring discipline (anti-inflation)

Scores use a fixed scale and are deliberately conservative:

| Score | Meaning |
|---|---|
| 0 | absent |
| 25 | generic / partial |
| 50 | useful partial |
| 75 | strong / consultant-grade |
| 90 | implementation-ready |
| 100 | **implemented + tested + demonstrated + documented + reviewable** |

**100% is rare on purpose.** It requires not just code, but tests, a live demonstration, documentation, and
the ability for an external reviewer to reproduce it. Every area in the scorecard carries an explicit
`proofRequiredFor100` — the concrete evidence that would move it to 100.

## Maturity stages

1. **Technical POC** — builds, broad tests, benchmark, live HTTP/DB evidence, demonstrable UI. *(Where the
   platform is strongest today.)*
2. **Controlled Pilot** — deployable with auth/audit, backup/restore, supportability, and a runbook; run on
   real (sanitized) estate data with sign-off.
3. **Enterprise Product** — hardened core + SSO/IdP, estate model, scale testing, SLAs, external audit.
4. **Commercial Product** — licensing, packaging, support tiers, repeatable deployments.
5. **Autonomous Engineering** — sandboxed fix→build→test→review loop with human-approval gates and proven
   rollback, demonstrated on real repositories.

## The 22 scored areas

The scorecard scores 22 areas spanning the five stages plus cross-cutting capabilities (security, audit,
data governance, deployment, supportability, benchmark credibility, business/banking/document/legacy/repo/estate
capability, UX, scalability, packaging, ERP/infra advisory, vendor-style design). Each area defines criteria
at 0/25/50/75/90/100, a current score, confidence, evidence, blockers, and the path to 100.

## Current snapshot

The overall mean and per-area scores render live at `/Readiness`. As of this phase the platform is a **strong
technical POC / early pilot**: core engineering areas (security, repository understanding, ingestion, audit,
data governance, benchmark) score in the 70s; deployment/supportability/scale and all domain-*implementation*
areas (OCR, PDF, legacy, estate) are intentionally lower because they are knowledge/design, not shipped
capability; commercial/autonomous areas are low by design.

## Browser/UI testing — current state and path to Playwright

Today the UI is verified by a deterministic **HTTP smoke test** (`scripts/poc/ui-smoke-test.ps1`) that starts
the app locally and asserts core pages return 200 (no 500s) and that Base Knowledge search works. **Playwright
is not yet added** because it requires Node.js + browser binary downloads (network), which we keep out of the
deterministic local gate for now; `.gitignore` already excludes `node_modules/` and `playwright/.cache/`.

**Path to Playwright (documented, not yet executed):** add `tests/LocalAIFactory.E2E` (Node + `@playwright/test`),
install browsers in CI only, drive the same flows the HTTP smoke test covers (home, Base Knowledge + searches,
Readiness, graph/coverage), keep it minimal and deterministic (fixed local startup, no external services),
and store screenshots only as ignored artifacts. Until then, the HTTP smoke test is the gate.

## How to update

Re-score after each phase: update `readiness-scorecard.json` (scores + evidence + blockers + `proofRequiredFor100`),
keep `lastReviewedUtc` current, and never inflate — a score only rises when new, reproducible evidence exists.
