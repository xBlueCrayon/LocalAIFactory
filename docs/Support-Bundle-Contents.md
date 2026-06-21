# Support-Bundle Contents — LocalAIFactory

What the support bundle collects, what it **deliberately excludes**, and how to produce it. The bundle
is a small, **read-only** package of diagnostics an operator can send to support without leaking
secrets, database contents, or source code.

The producer is `scripts/support/export-support-bundle.ps1`. On the build host it yields a **~3 KB
zip** (`LocalAIFactory-support-bundle.zip`).

---

## 1. Produce the bundle

```powershell
pwsh scripts/support/export-support-bundle.ps1
# -> SUPPORT-BUNDLE: .../.tmp-release/LocalAIFactory-support-bundle.zip (~3 KB)
```

It runs each diagnostics script, captures its **text output** into a file, adds an `environment.json`
of non-secret facts, and zips the folder. Output goes to a git-ignored `./.tmp-release/support-bundle`
folder. If a diagnostic script is absent it is skipped; if one errors, the error message is captured
instead of failing the bundle.

---

## 2. What the bundle contains

| File in bundle | Source script | Contents |
|---|---|---|
| `system-snapshot.txt` | `scripts/diagnostics/system-snapshot.ps1` | OS / hardware / runtime snapshot |
| `gpu-health.txt` | `scripts/diagnostics/gpu-health-check.ps1` | GPU presence / driver health |
| `ollama-health.txt` | `scripts/diagnostics/ollama-health-check.ps1` | Ollama reachability + model list (optional service) |
| `sql-health.txt` | `scripts/diagnostics/sql-health-check.ps1` | SQL Server reachability / version |
| `process-monitor.txt` | `scripts/diagnostics/process-monitor.ps1` | Relevant process snapshot |
| `knowledge-verify.txt` | `scripts/knowledge/verify-all-knowledge-packs.ps1` | KB validation result (4 packs / 438 items) |
| `security-audit.txt` | `scripts/security/security-audit.ps1` | Static self-audit result (HIGH/INFO counts) |
| `environment.json` | (inline) | `timestampUtc`, machine name, OS caption, `dotnet --version`, git short commit, git branch |

`environment.json` contains only **non-secret** environment facts — notably **no** connection
strings, **no** credentials, and **no** API keys.

---

## 3. What the bundle EXCLUDES (by design)

The bundle deliberately contains:

- **No secrets** — no connection strings, credentials, API keys, or the `keys/` Data Protection
  folder.
- **No database contents** — only health/reachability snapshots, never rows, knowledge content, or
  audit data.
- **No source code** — only the captured **output** of read-only diagnostics, not the scripts' inputs
  or the application source.
- **No backups or model files.**

Every captured script is **read-only**: none submit a form, write to the database, or change system
state. The result is a snapshot small enough (~3 KB) to attach to a support ticket safely.

---

## 4. When to send a bundle

- The app fails to start or a core page hangs (attach the bundle plus the relevant
  `RequestTimingMiddleware` log lines — see `docs/07-Troubleshooting.md`).
- An optional service (Ollama / Qdrant) appears unhealthy on `/Support` and you want a point-in-time
  capture.
- A deployment/acceptance check fails and support needs the environment facts.
- As a routine artifact attached to any escalation.

For the operator-facing health view and the broader diagnostics workflow, see
`docs/Supportability-Dashboard-Guide.md`.

---

## 5. Honesty notes

- The **~3 KB** figure is from a real run on the build host; size varies slightly with the captured
  output but the bundle is intentionally tiny and content-light.
- The bundle reflects only the host it was generated on. It is a **diagnostic aid**, not an audit
  record; the tamper-evidence limitations of the audit trail still apply
  (`docs/Audit-Model.md`, `docs/Known-Limitations.md` §4).

## See also

- `docs/Supportability-Dashboard-Guide.md`
- `docs/Support-Runbook.md`, `docs/07-Troubleshooting.md`
- `docs/Final-Security-Audit-Report.md`
