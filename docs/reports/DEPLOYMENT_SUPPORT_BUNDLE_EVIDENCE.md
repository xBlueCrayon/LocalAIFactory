# Deployment Support Bundle Evidence (Phase 6)

**Date:** 2026-06-21

A read-only support bundle was exported during the deployment proof with
`scripts/support/export-support-bundle.ps1`.

| Item | Value |
|---|---|
| Output | `./.tmp-release/LocalAIFactory-support-bundle.zip` (≈ **3 KB**) — git-ignored, **not committed** |
| Captured sections | `ollama-health`, `sql-health`, `process-monitor`, `knowledge-verify`, `security-audit` |
| Secrets | **None** — the exporter is read-only diagnostics only |
| Exit | 0 (success) |

The support bundle is the artifact an operator attaches to a support request: it captures local AI
(Ollama) reachability, SQL health, running processes, knowledge-pack verification, and the security-audit
result, with no secrets or large files. It is a **deliverable script output**, deliberately git-ignored
(diagnostics, not source).

## Reproduce

```powershell
pwsh scripts\support\export-support-bundle.ps1
# -> .tmp-release/LocalAIFactory-support-bundle.zip
```
