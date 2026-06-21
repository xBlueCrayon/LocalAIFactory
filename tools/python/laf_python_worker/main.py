"""Single safe entrypoint. Usage: python -m laf_python_worker.main <task>  (JSON on stdin, JSON on stdout).

Only the approved task set runs; an unknown task returns a structured error. Every handler reads a JSON object
from stdin and writes a JSON object to stdout. Errors are returned as JSON ({"ok": false, "error": ...}),
never raised across the bridge.
"""
import json
import re
import sys
from . import safety


def _read_input() -> dict:
    raw = sys.stdin.read() or "{}"
    try:
        return json.loads(raw)
    except Exception:
        return {}


def code_mine(data: dict) -> dict:
    """Count symbols/keywords across provided file contents (deterministic; Python's regex/AST shine here)."""
    files = data.get("files", [])
    total_classes = total_methods = 0
    per_file = []
    for f in files:
        content = f.get("content", "")
        classes = len(re.findall(r"\bclass\s+\w+", content))
        methods = len(re.findall(r"\b(public|private|protected|internal)\s+[\w<>\[\]]+\s+\w+\s*\(", content))
        total_classes += classes
        total_methods += methods
        per_file.append({"path": f.get("path"), "classes": classes, "methods": methods})
    return {"ok": True, "task": "code-mine", "classes": total_classes, "methods": total_methods, "files": per_file}


def pattern_mine(data: dict) -> dict:
    patterns = {"login": r"(?i)password|login|authenticate", "sql": r"(?i)select\s+.*\s+from", "audit": r"(?i)audit"}
    hits = {}
    for f in data.get("files", []):
        for name, pat in patterns.items():
            if re.search(pat, f.get("content", "")):
                hits[name] = hits.get(name, 0) + 1
    return {"ok": True, "task": "pattern-mine", "patterns": hits}


def doc_extract(data: dict) -> dict:
    text = data.get("text", "")
    # Summarize: keep the first sentence of each paragraph (clean-room; never echo large raw text back).
    paras = [p.strip() for p in text.split("\n\n") if p.strip()]
    facts = [re.split(r"(?<=[.!?])\s", p)[0][:300] for p in paras][:20]
    return {"ok": True, "task": "doc-extract", "facts": facts, "source_chars": len(text)}


def web_scrape(data: dict) -> dict:
    url = data.get("url", "")
    if not safety.is_allowlisted_url(url):
        return {"ok": False, "task": "web-scrape", "error": f"url not allowlisted: {url}"}
    # Network fetch is intentionally not performed in this skeleton; a real deployment would fetch + cache by
    # hash + record citation metadata here. Returns a structured proposal stub instead of vendoring raw text.
    return {"ok": True, "task": "web-scrape", "url": url, "note": "allowlisted; fetch+cache+cite happens in the full worker", "proposal": True}


def passthrough(task: str, data: dict) -> dict:
    return {"ok": True, "task": task, "received_keys": sorted(list(data.keys()))}


HANDLERS = {
    "code-mine": code_mine,
    "pattern-mine": pattern_mine,
    "doc-extract": doc_extract,
    "web-scrape": web_scrape,
}


def main(argv) -> int:
    task = argv[1] if len(argv) > 1 else ""
    if not safety.is_approved_task(task):
        print(json.dumps({"ok": False, "error": f"task not approved: {task}"}))
        return 2
    data = _read_input()
    try:
        handler = HANDLERS.get(task)
        result = handler(data) if handler else passthrough(task, data)
        print(json.dumps(result))
        return 0
    except Exception as ex:  # never raise across the bridge
        print(json.dumps({"ok": False, "task": task, "error": str(ex)}))
        return 1


if __name__ == "__main__":
    sys.exit(main(sys.argv))
