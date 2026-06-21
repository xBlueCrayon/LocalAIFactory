# LAF Knowledge-Growth Scraper — Report

**Stamp:** 2026-06-21
**Component:** `LocalAIFactory.KnowledgeGrowth`
**Benchmark:** `benchmarks/results/laf-knowledge-growth-scraper.json`

## Purpose

Let the platform learn from official documentation **safely**: allowlisted sources only, never
vendoring raw third-party text, always citing, always requiring human approval.

## Allowlist (`ScrapeAllowlist`)

- **https-only**, **default-deny**.
- Allowed hosts: `learn.microsoft.com`, `docs.python.org`, `docs.ollama.com`,
  `modelcontextprotocol.io`.
- Also allowed: official GitHub docs (`*.github.io`, `github.com`).
- Anything else (including plain `http`) is rejected.

## Ingest (`KnowledgeGrowthService.Ingest`)

1. **Allowlist check** on the document URL.
2. **Cache + dedup by content hash** (SHA-256 of the HTML); a duplicate returns the existing
   proposal.
3. **Clean-room summarise**: tags stripped, leading sentences kept, facts capped at **300 chars ×
   20**. The raw HTML/body text is **never stored**.
4. **Required `CitationMetadata`**: url + title + fetchDate + sourceHash.
5. Emits a **`GrowthProposal` with `Approved=false`** — a human must approve.

## Test result

| Metric | Value |
| --- | --- |
| Tests | 13 |
| Passed | 13 |
| Offline | yes (fully) |

## Honest limitations / not met

- The service is **offline by design**: it ingests **caller-supplied** fetched documents and does
  **not** perform the network fetch itself. The actual fetch is the allowlisted Python web-scrape
  worker's job — and that worker is a **stdlib skeleton** this sprint (it allowlist-checks but does
  not fetch).
- Summarisation is a **deterministic first-sentence extractor**, not a model summary; quality is
  bounded by that heuristic.
- No knowledge is added automatically: every proposal **requires human approval**, so this path
  does not, on its own, grow the knowledge base without a reviewer.
