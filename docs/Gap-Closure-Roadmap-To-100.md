# Gap-Closure Roadmap to 100%

What it would take to move each materially-incomplete readiness area to its next band — honest, evidence-based,
no shortcuts. "100" everywhere means implemented + tested + demonstrated + documented + reviewable. Authoritative
per-area criteria and `proofRequiredFor100` live in `readiness-scorecard.json`.

## After R2-ACC-CAPABILITY-MAX (overall ≈ 49%)

The capability sprint converted knowledge/design into **shipped, tested capability** for the bridge, Python,
benchmark tiering, deployment scripts, document/OCR prototypes, and the autonomous skeleton. The remaining gaps
are deliberately not claimed.

## Highest-leverage next steps (in order)

1. **Cross-repository estate model (17, 13 → higher).** Link symbols/edges across repos by shared-DB identity so
   impact crosses system boundaries (BDM/MCIB/ETAMS/ChequeXpert). Proof: an estate-wide impact query validated
   against the real (sanitized) estate.
2. **Real OCR/PDF engine (14 → 50+).** Add a PDF parser library for true text extraction with page provenance,
   and a Python CV service to populate the cheque OCR/signature DTOs. Proof: measured precision/recall on a
   governed validation set; human-review gate exercised end-to-end.
3. **Autonomous execution loop (5 → 60+).** Let the dry-run planner actually run its allowlisted steps in an
   isolated worktree, with proven rollback and a human-approval gate before any commit. Proof: a real fix
   applied, built, tested, and reverted safely, with an audit trail.
4. **Proven deployment (9 → 75+).** Execute the compose/scripts to a staged environment with a backup/restore
   drill and health gates. Proof: a documented, repeatable rollout.
5. **Benchmark breadth (11 → 90).** Promote approved public repos across Smoke/Standard/Extended (incl. a real
   Python repo now that the extractor exists) and reproduce a run externally.
6. **Security hardening (6 → 90).** Enterprise IdP/SSO + an independent penetration test.
7. **Supportability dashboard (10 → 75).** Health/diagnostics UI + diagnostics bundle + alerting/SLOs.
8. **Scale/perf (19 → 75).** Load testing and capacity guidance on a large estate.

## What stays low on purpose

Commercial/packaging/licensing (4, 20) and Enterprise/Commercial product readiness (3, 4) remain low until there
is a licensing model, packaging, support tiers, and at least one real pilot/commercial deployment. These are
business milestones, not engineering ones, and are not inflated.

## Discipline

A score only rises when new, reproducible evidence exists (a test, a benchmark, a DB/HTTP result, a validated
script, or a demonstrated workflow). The scorecard is re-reviewed at the end of each phase; `lastReviewedUtc`
and `changeLog` in the JSON record when and why.
