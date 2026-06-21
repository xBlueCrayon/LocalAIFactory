# LAF Code Building Blocks — Report

**Stamp:** 2026-06-21
**Component:** `LocalAIFactory.CodeBlocks`
**Benchmark:** `benchmarks/results/laf-code-building-blocks-summary.json`

## Purpose

Treat engineering patterns as reusable, well-understood **bricks**. A requirement is satisfied by
**composing** known blocks (with their dependencies) rather than generating from scratch — and the
engine is honest about any capability it has no brick for.

## The block model (`CodeBuildingBlock`)

Each block carries: `BlockId`, `Name`, `Purpose`, `ProblemSolved`, `RequiredInputs`,
`GeneratedFiles`, `CodePatternSummary`, `Dependencies`, `SecurityRules`, `ValidationRules`,
`TestPattern`, `PlaywrightPattern`, `FailureModes`, links to knowledge / generator templates /
experiences, `Keywords`, and `Confidence`. Derived flags: `RequiresMigration`, `HasPlaywright`.

## The 16 seeded blocks (`CodeBlockCatalog` / `DefaultBlockLibrary`)

| Block | Depends on |
| --- | --- |
| password-hashing | — |
| audit-event | — |
| anti-forgery | — |
| secure-login | password-hashing, audit-event, anti-forgery |
| login-lockout | secure-login, audit-event |
| maker-checker | audit-event |
| ef-migration | — |
| crud-module | audit-event |
| report-endpoint | — |
| stock-movement | — |
| accounting-posting | — |
| document-lifecycle | maker-checker, audit-event |
| manufacturing-order | stock-movement, audit-event |
| import-export | — |
| playwright-proof | — |
| production-smoke | — |

Each is grounded in the real ERP Gold + reasoning work (knowledge ids and generator-template paths
are linked on the block).

## Composition (`BlockComposer`)

- Matches blocks by keyword score against the requirement.
- Pulls in **transitive dependencies** (bounded recursion).
- Aggregates files, tests, Playwright proofs, security rules, migration impact, knowledge ids, and
  generator templates.
- **Honestly flags uncovered capabilities as missing bricks** (odoo, woocommerce, cheque-ocr, sftp,
  ticketing, mcib, direct-debit, web-scraper) and applies a confidence **honesty penalty**.

## Extraction (`BlockExtractor`)

Detects which blocks are present in a given file set.

## Test result

| Metric | Value |
| --- | --- |
| Seeded blocks | 16 |
| Tests | 24 |
| Passed | 24 |
| Model required | NO |

## Honest limitations / not met

- The catalogue is **partial by design**: 16 blocks. Several real-world capabilities have **no
  brick** (odoo connector, woocommerce mapper, cheque OCR, sftp, ticketing, mcib export,
  direct-debit mandate, web-scraper). The composer reports these as missing rather than faking them.
- Matching is **keyword-based**, not semantic; a requirement phrased without the expected keywords
  may under-match.
- `GeneratedFiles` and `TestPattern` describe the **pattern**; the composer plans, it does not by
  itself emit or run the code (that is the generator's and the patch runner's job).
