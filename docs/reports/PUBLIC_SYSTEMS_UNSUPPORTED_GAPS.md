# Public Systems — Unsupported / Gap Report

**Date:** 2026-06-21

Honest accounting of what LocalAIFactory **cannot** do across the 113-system manifest.

## Language extraction gaps

The deterministic extractor supports **C#, T-SQL, Python** only. Of the 113 systems:

- **34** are `full` extraction mode (C#/T-SQL/Python primary) — real symbols extractable.
- **74** are `validation-only` (TypeScript, JavaScript, Java, Go, PHP, Ruby, etc.) — cloned and
  file-classified, but **not** structurally extracted. Code-evidence questions on these are honest gaps.
- **5** are `DocsOnlyReference` (no useful open-source repo for our purposes) — docs metadata only.

So the **majority (≈70%)** of the manifest is **not** fully code-extractable today. This is the headline gap
and is reported, never hidden.

## Docs/API gaps

- Official docs/API URLs are **registered** for all 113 systems but **content was deep-read for only a
  sampled 5** (see the doc/API cross-check). The remaining systems' doc understanding is **metadata-level**.

## Scale gaps (from the 51-repo run)

- xlarge monorepos (dotnet/runtime, elasticsearch, kubernetes, node, …) exceed the per-repo clone/analyze
  budget on this workstation — `CloneFailed`/`TimedOut`. A server-class host + a longer budget + incremental
  extraction would close this.

## What would close these gaps

1. **Language coverage:** deterministic extractors for TypeScript/JavaScript, Java, Go, PHP (the bulk of the
   74 validation-only systems).
2. **Docs depth:** a polite, rate-limited official-docs fetch + content extraction across more systems.
3. **Scale:** a server-class host + incremental/streaming extraction for xlarge monorepos.

These are capability gaps, **not** crashes — the platform degrades honestly and reports the gap.
