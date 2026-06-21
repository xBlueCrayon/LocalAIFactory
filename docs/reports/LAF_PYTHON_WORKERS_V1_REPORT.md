# LAF Python Workers V1 — Report

**Stamp:** 2026-06-21
**Component:** `LocalAIFactory.PythonBridge/SafePythonWorkerRunner` + `tools/python/laf_python_worker`
**Benchmark:** `benchmarks/results/laf-python-workers-v1-summary.json`

## Purpose

Provide a **safe-by-construction** bridge to optional Python workers for work where Python's
ecosystem is strong (mining, embeddings, datasets), without letting Python become a hard dependency
or a security hole.

## The bridge (`SafePythonWorkerRunner`)

- **Allowlist only.** Exactly **9 approved entrypoints** may run: `code-mine`, `pattern-mine`,
  `doc-extract`, `web-scrape`, `embed-text`, `rerank`, `build-dataset`, `graph-enrich`,
  `extract-knowledge`. An unknown entrypoint is rejected before any process starts.
- **No arbitrary scripts.** The worker is invoked as `python -m laf_python_worker.main {entrypoint}`
  from a fixed working directory. There is no path to run a caller-supplied script.
- **JSON in / JSON out** over stdin/stdout.
- **Hard timeout + kill.** A worker that overruns the timeout is killed; the run returns a timeout
  error rather than hanging.
- **Graceful when Python is absent.** `IsAvailable` probes `python --version`; if Python is not
  installed, every run returns `Available=false` and **never throws**.

## The Python skeleton (`tools/python/laf_python_worker`)

Stdlib-only (`main.py` dispatcher, `safety.py` allowlist, `requirements.txt`, README,
`__init__.py`). The dispatcher refuses any non-approved task and never raises across the bridge.
Implemented handlers: `code-mine`, `pattern-mine`, `doc-extract`, `web-scrape` (allowlist-checked,
fetch deferred to the full worker). It runs when Python is present.

## Test result

| Metric | Value |
| --- | --- |
| Bridge tests | 9 |
| Passed | 9 |
| Python installed during tests | NO |

The bridge is proven to behave correctly (allowlist enforcement, graceful unavailability) **without
Python installed**.

## Honest limitations / not met

- The Python side is a **stdlib-only skeleton**, not a complete ML/scrape worker. The
  `embed-text`, `rerank`, `build-dataset`, `graph-enrich`, and `extract-knowledge` entrypoints are
  approved at the bridge but their full implementations require a local Python **venv** and are not
  delivered this sprint.
- `web-scrape` is allowlist-checked but the skeleton **does not perform a network fetch**; the real
  fetch+cache+cite is left to the full worker.
- Tests cover the bridge with Python absent; **end-to-end runs with Python present are not part of
  this benchmark**.
