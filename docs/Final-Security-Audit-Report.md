# Final Security Audit Report — LocalAIFactory

The results of the committed **static security self-audit**, the **repo-hygiene** facts it confirms,
its **coverage and limits**, and a clear statement that **no external penetration test has been
performed** and that one is **required for commercial GA**.

The audit tool is `scripts/security/security-audit.ps1`. It is **read-only and static** — no network,
no system changes. It is explicitly **not** a penetration test.

---

## 1. Live result

```powershell
pwsh scripts/security/security-audit.ps1
# -> HIGH findings: 0   INFO findings: 2
# -> SECURITY-AUDIT: PASS (no HIGH findings)
```

| Severity | Count | Meaning |
|---|---:|---|
| **HIGH** | **0** | No forbidden/large tracked artifacts, no hardcoded secrets — gating findings, none present |
| **INFO** | **2** | Two operator scripts contain **gated** destructive patterns flagged for human review |

The script exits non-zero only when run with `-FailOnFindings` **and** a HIGH finding exists, so it
can gate a release. With **0 HIGH**, it passes.

---

## 2. The 2 INFO findings explained

The audit flags any committed `.ps1` containing a destructive pattern
(`Remove-Item -Recurse -Force <drive>`, `rm -rf /`, `git reset --hard`, `git clean -fdx`,
`DROP DATABASE`, `format <drive>`) for **review** — it does not assume them unsafe.

Both INFO hits are in **operator scripts where the destructive action is intentional and gated**:

- The action runs only on a **throwaway, git-ignored** path or a clearly-named **non-production**
  target (e.g. re-creating a clean extraction folder, or operating on a verify database), never on the
  production database or a live drive.
- The restore path **refuses** the production database name; the index-reset is **dry-run by default**
  and protects curated/audit/provenance tables.

They are surfaced as INFO **so a human confirms the gating**, which is the intended behavior — not
defects. (The audit tool excludes itself from the destructive-pattern scan to avoid self-flagging its
own pattern list.)

---

## 3. Static-scan coverage

Over **tracked files only** (`git ls-files`):

- **Forbidden / large tracked artifacts:** flags any tracked `bin/`, `obj/`, `*.mdf`, `*.ldf`,
  `*.bak`, `*.gguf`, `*.onnx`, `*.safetensors`, `*.pfx`, `*.pem`, `*.key`, anything under `keys/`, or
  any tracked file **> 5 MB**.
- **Potential secrets / hardcoded credentials:** conservative regex scan over `.cs/.json/.ps1/.config/
  .cshtml/.xml/.yml/.yaml` (excluding `*.example.*`, `docs/`, `THIRD-PARTY`,
  `appsettings.Development.json`) for password assignments, API-key/secret/token literals, AWS access
  keys, and private-key blocks. Obvious placeholders and `Integrated Security` / `Trusted_Connection`
  are allowlisted.
- **Dangerous shell commands in committed scripts:** the destructive-pattern scan described in §2.

What it does **not** cover: runtime behavior, authentication/authorization logic correctness,
injection testing, dependency CVE scanning, or any dynamic/offensive testing. Those require the
external assessment in §5.

---

## 4. Repo-hygiene facts (confirmed)

| Check | Result |
|---|---|
| Tracked `bin/obj/db/model/keys` artifacts | **0** |
| Tracked file > 5 MB | **0** |
| Hardcoded secrets in tracked source/config | **none matched** |
| HIGH findings | **0** |

Consistent with the platform's secrets policy: API keys are encrypted at rest via Data Protection
(`keys/` is git-ignored), and connection strings with credentials live in environment variables or a
git-ignored override — never in committed config (CLAUDE.md §10). The release package enforces the
same exclusions (`docs/Published-Package-Contents.md` §4).

The audit is also captured in the support bundle (`security-audit.txt`) for point-in-time evidence
(`docs/Support-Bundle-Contents.md`).

---

## 5. No external penetration test — REQUIRED for commercial GA

**No external/third-party penetration test or offensive security assessment has been performed.** The
static self-audit in this document is a hygiene gate, **not** a pen-test and **not** a security
certification.

A completed external penetration test — covering authentication/authorization (Windows/Negotiate
today; **no SSO/OIDC/SAML**), injection, IDOR, session handling, and dependency exposure — plus
remediation of its findings, is a **required gate for commercial GA**. Until that report exists and
its findings are closed:

- Treat the platform as **local-first, single-tenant, internally deployed** only.
- **Commercial GA remains blocked** on this item (among others in `docs/Known-Limitations.md`).

Related open security items (see `docs/Known-Limitations.md`): audit is append-only by convention,
**not** tamper-evident (no hash chaining); `AccessLevel.Write` is reserved, not behavioral; no
enterprise SSO/IdP.

---

## 6. Reproduce

```powershell
# Report mode (always exits 0 unless -FailOnFindings + a HIGH finding):
pwsh scripts/security/security-audit.ps1

# Release-gating mode (non-zero on any HIGH finding):
pwsh scripts/security/security-audit.ps1 -FailOnFindings
```

## 7. Honesty statement

The repository is **clean by the static measures above** (0 HIGH, hygiene confirmed) and the 2 INFO
findings are intentional, gated operator patterns. This is a **static self-audit only**. It makes no
security-certification or compliance claim, and it does not substitute for the external penetration
test required before commercial GA.

## See also

- `docs/Security-Model.md`, `docs/Threat-Model.md`
- `docs/Security-Pentest-Readiness.md`, `docs/Security-Test-Checklist.md`
- `docs/Secrets-Handling.md`, `docs/Known-Limitations.md`
