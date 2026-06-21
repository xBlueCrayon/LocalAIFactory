# LAF Python Workers

A small, **safe** Python worker subsystem invoked by the C# `SafePythonWorkerRunner`
(`src/LocalAIFactory.PythonBridge`). Python does the things Python is good at — AST/regex mining,
embeddings, reranking, scraping, dataset building — while **C# stays the orchestration + safety authority**.

## Contract

- Single entrypoint: `python -m laf_python_worker.main <task>`.
- **Approved tasks only:** `code-mine, pattern-mine, doc-extract, web-scrape, embed-text, rerank,
  build-dataset, graph-enrich, extract-knowledge`. Anything else returns a structured error.
- **JSON in / JSON out** (stdin → stdout). Errors are returned as `{"ok": false, "error": ...}`, never raised
  across the bridge.
- **Allowlist-only** web scraping (`safety.ALLOWLIST_DOMAINS`): learn.microsoft.com, docs.python.org,
  docs.ollama.com, modelcontextprotocol.io (+ official GitHub docs on explicit request). Fetches are cached
  by hash and carry citation metadata; large raw third-party text is **never** vendored — only summarised facts.
- No arbitrary script execution, no writes outside the approved folder, no network outside the allowlist.

## Honest status

The committed modules are a **stdlib-only skeleton** that runs and returns structured JSON. The C# bridge and
its tests pass **without Python installed** (it reports unavailable and the platform degrades). The full
embedding/rerank/scrape workers require the pinned `requirements.txt` in a local venv and are out of scope to
execute in CI.
