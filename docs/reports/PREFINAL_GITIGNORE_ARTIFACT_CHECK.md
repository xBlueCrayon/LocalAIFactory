# PREFINAL — .gitignore & Local-Artifact Check (Phase 6)

**Date:** 2026-06-21

## Defect found & fixed: ignored source folders

The `[Cc]overage*/` rule (intended for test-coverage **output**) was matching real **source** folders named
`Coverage` under `src/`, silently excluding:

- `src/LocalAIFactory.Ingestion/Coverage/ImportCoverageService.cs`
- `src/LocalAIFactory.Web/Views/Coverage/Index.cshtml`

`CoverageController.cs` was tracked but its service + view were **not** — a fresh clone would have failed to
build / 404'd `/Coverage`. **Fix:** added scoped negations immediately after the rule, then tracked both files:

```gitignore
[Cc]overage*/
!src/**/[Cc]overage/
!src/**/[Cc]overage/**
```

A full scan of `src/`, `tests/`, `scripts/`, `database/`, `deploy/`, `knowledge-packs/`, `benchmarks/fixtures/`,
`enterprise-scenarios/`, `release-template/` confirmed **no other** intended source/script/pack/fixture file is
accidentally ignored.

## Required ignores present (generated artifacts)

`bin/`, `obj/`, `out/`, `publish/` + `[Pp]ublish/`, `[Dd]ebug/`, `[Rr]elease/` (with `scripts/release/`
un-ignored), `.vs/` `.vscode/` `.idea/`, `*.user` `*.suo`, `*.mdf/.ldf/.ndf/.bak/.bacpac`, `logs/` `*.log`,
`uploads/` `_incoming/` `workspaces/`, `models/` `*.gguf/.safetensors/.onnx/.pt/.pth/.ckpt`, `.ollama/`
`ollama_models/`, `qdrant_storage/` `.qdrant/`, `node_modules/` `.playwright/` `ms-playwright/`
`test-results/`, `__pycache__/` `.venv/` `.env`, `[Cc]overage*/` `*.trx` `*.coverage`, `keys/`,
`benchmarks/cache/` `benchmarks/reports/`, `scripts/poc/artifacts/` `scripts/poc/*.png`.

## Required source NOT ignored (verified)

`docs/`, `docs/reports/` (new), `scripts/**` (incl. `release/`, `knowledge/`, `diagnostics/`, `security/`,
`docs/`, `auto/`), `database/`, `deploy/`, `release-template/`, `knowledge-packs/`, `enterprise-scenarios/`,
`benchmarks/fixtures/`, `tests/`, `src/` (incl. the now-restored `Coverage` source).

## Local generated artifacts

`bin/`/`obj/`/`.vs/` exist locally and are correctly ignored. They were **left in place** (deleting them only
forces a slower rebuild and carries no tracking risk). No tracked generated artifact, backup, log, publish
output, model file, or `>5 MB` file exists.

**Verdict: .gitignore is correct after the Coverage fix; no forbidden artifact is tracked.**
