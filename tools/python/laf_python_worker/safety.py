"""Safety policy for the Python workers: approved tasks, allowlisted scrape domains, safe path checks."""
import os
from urllib.parse import urlparse

APPROVED_TASKS = {
    "code-mine", "pattern-mine", "doc-extract", "web-scrape", "embed-text",
    "rerank", "build-dataset", "graph-enrich", "extract-knowledge",
}

# Web scraping is ALLOWLIST-ONLY. Anything not on this list is rejected.
ALLOWLIST_DOMAINS = {
    "learn.microsoft.com",
    "docs.python.org",
    "docs.ollama.com",
    "modelcontextprotocol.io",
}


def is_approved_task(task: str) -> bool:
    return task in APPROVED_TASKS


def is_allowlisted_url(url: str) -> bool:
    try:
        host = (urlparse(url).hostname or "").lower()
    except Exception:
        return False
    return host in ALLOWLIST_DOMAINS or host.endswith(".github.com")


def is_safe_path(root: str, candidate: str) -> bool:
    """True only if candidate resolves inside root (no traversal/absolute escape)."""
    try:
        root_full = os.path.realpath(root)
        full = os.path.realpath(candidate if os.path.isabs(candidate) else os.path.join(root, candidate))
        return os.path.commonpath([root_full, full]) == root_full
    except Exception:
        return False
