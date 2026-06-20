# Repository Cleanliness & Size Audit

Audit of the working folder and Git repository for an enterprise review. Measured at commit `138a59b`
(R2-ACC-B3), 2026-06-21. Re-run the measurements with the commands shown.

## Summary verdict

**Clean.** The Git repository is small and healthy (`.git` ≈ 2.9 MB). No build outputs, caches, databases,
model weights, or secrets are tracked. On-disk bulk is entirely **git-ignored** build output and benchmark
cache, not committed content.

## Measurements

| Metric | Value | Note |
|---|---|---|
| Working folder (incl. bin/obj, cache, .git) | ≈ 345 MB | dominated by git-ignored artifacts |
| `.git` directory | ≈ 2.9 MB | healthy; no large blobs |
| `bin/` + `obj/` on disk | ≈ 275 MB | **git-ignored** build output |
| `benchmarks/cache/` on disk | ≈ 60 MB | **git-ignored** pinned-repo clones |
| Tracked files | 347 | source artifacts only |
| `knowledge-packs/` | ≈ 756 KB | reasonable (390 items + registry, JSON) |
| Largest tracked files | ≈ 100 KB each | EF migration `*.Designer.cs` / `ModelSnapshot.cs` |

Commands:
```bash
du -sh .            # working folder
du -sh .git         # git size
git ls-files | wc -l                                   # tracked file count
git ls-files | xargs -I{} du -k "{}" | sort -rn | head # largest tracked files
```

## Tracking hygiene (all clean)

| Should NOT be tracked | Tracked? | Guard |
|---|---|---|
| `bin/` `obj/` | No (0) | `.gitignore: bin/ obj/` |
| Benchmark cache | No (0) | not added; clones live in `benchmarks/cache/` |
| Database files (`*.mdf/ldf/bak/db/sqlite`) | No (0) | `.gitignore: *.mdf` (+ added patterns) |
| Model weights (`*.gguf/onnx/safetensors/pt`) | No (0) | `.gitignore: *.gguf *.onnx model-cache/` |
| Data Protection keys | No (0) | `.gitignore: keys/` |
| `node_modules/`, Playwright cache | No (0) | `.gitignore: node_modules/ playwright/.cache/` |
| Generated screenshots/logs/temp | No | `.gitignore` POC artifact patterns (added this phase) |

## Content composition (tracked)

`src/` (251 files, 8 projects), `tests/`, `tools/` (benchmark), `knowledge-packs/` (pack JSON + registry),
`docs/` (architecture, phase notes, POC/readiness/governance), `enterprise-scenarios/` (14 × 4 markdown),
`benchmarks/` (manifest, golden snapshots, README, candidates — **not** the cache), `deploy/docs/`, `scripts/`,
`prompts/`. All are **source artifacts** (code, JSON data, markdown) — no runtime binaries.

## Actions taken this phase

- Extended `.gitignore` with POC artifact patterns (`*.log`, temp/screenshot output under `scripts/poc/`) so
  demo/smoke artifacts are never accidentally committed. No files were deleted; no Git history was rewritten.

## Recommendations

- Keep a periodic **benchmark-cache cleanup** habit (the 60 MB cache grows as repos are added).
- If large sample datasets or model weights are introduced later, confirm they match an existing ignore
  pattern before adding, and store them outside version control (or via Git LFS if ever required).
- The repository is suitable to share with an enterprise reviewer as-is.
