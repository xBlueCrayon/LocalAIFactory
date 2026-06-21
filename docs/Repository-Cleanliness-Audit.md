# Repository Cleanliness & Size Audit

Audit of the working folder and Git repository for an enterprise review. Re-measured live at commit
`1ccd494` (R2-ACC-POC-COMPLETE), 2026-06-21. Re-run the measurements with the commands shown.

## Summary verdict

**Clean.** The Git repository is small and healthy (`.git` ≈ **3.3 MB**, **6.3 MB tracked across 788
files**). No build outputs, caches, databases, model weights, release ZIPs, or secrets are tracked. The
~494 MB on disk is **entirely git-ignored** build output, temp dirs, and benchmark cache — not committed
content.

## Measurements (live, commit `1ccd494`)

| Metric | Value | Note |
|---|---|---|
| Working folder (incl. bin/obj, .tmp-*, cache, .git) | **≈ 497 MB** | dominated by git-ignored artifacts |
| **Tracked content** | **6.3 MB / 788 files** | source artifacts only — this is the real repo size |
| `.git` directory | **≈ 3.3 MB** | healthy; no large blobs, no history bloat |
| `bin/` + `obj/` on disk | ≈ 275 MB | **git-ignored** build output (src 138, tests 94, tools 45) |
| `.tmp-release` / `.tmp-publish` / `.tmp-clean-install` | ≈ 154 MB | **git-ignored** release/publish working dirs |
| `benchmarks/cache/` on disk | ≈ 51 MB | **git-ignored** pinned-repo clones |
| `.tmp-playwright` (node_modules) | ≈ 17 MB | **git-ignored** Playwright tooling |
| Largest tracked files | ≤ 191 KB | `docs/screenshots/*.png` (11 real PNGs), EF migration `*.Designer.cs` |
| Tracked files > 5 MB | **0** | none |
| `knowledge-packs/` | ≈ 0.8 MB | reasonable (4 packs / 438 items + registry, JSON) |

Commands (PowerShell):
```powershell
# working folder excl .git, .git size, tracked size + count
(Get-ChildItem -Recurse -File -Force | ? FullName -notmatch '\\\.git\\' | Measure Length -Sum).Sum
(Get-ChildItem .git -Recurse -File -Force | Measure Length -Sum).Sum
git ls-files | % { (Get-Item $_).Length } | Measure -Sum ; (git ls-files | Measure).Count
git ls-files | % { [pscustomobject]@{KB=[int]((Get-Item $_).Length/1KB);P=$_} } | sort KB -desc | select -First 12
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

`src/` (276 files, 8 projects), `tests/`, `tools/` (benchmark), `knowledge-packs/` (4 packs JSON + registry),
`docs/` (architecture, phase notes, POC/readiness/governance, 11 screenshots), `enterprise-scenarios/`
(19 scenarios), `benchmarks/` (manifest, golden snapshots incl. `ENTGIANT.json`, fixtures, README, candidates —
**not** the cache), `deploy/docs/`, `scripts/`, `prompts/`. All are **source artifacts** (code, JSON data,
markdown, small PNGs) — no runtime binaries.

## Actions taken this phase

- Re-measured live at commit `1ccd494`; confirmed **0** forbidden tracked artifacts and **0** files > 5 MB.
- Verified the scoped benchmark scratch dir (`.tmp-bench`, used by the enterprise reasoning runner) is
  git-ignored. No files were deleted; no Git history was rewritten.

## Recommendations

- Keep a periodic **benchmark-cache cleanup** habit (the 60 MB cache grows as repos are added).
- If large sample datasets or model weights are introduced later, confirm they match an existing ignore
  pattern before adding, and store them outside version control (or via Git LFS if ever required).
- The repository is suitable to share with an enterprise reviewer as-is.
