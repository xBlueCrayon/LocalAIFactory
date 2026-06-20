# Resource and Performance Evidence

This document records **actual measurements** taken on the build/validation host this sprint and
the exact diagnostics scripts that reproduce them. Nothing here is projected or estimated; where a
value was read from a tool, the tool is named so the reading can be re-taken. This is a single-host
snapshot, not a statistical benchmark — re-run the scripts on your own host to get your own numbers.

> Scope honesty: these numbers describe one developer-class workstation at one point in time. They
> are **not** a load test, **not** a multi-user benchmark, and **not** a production-capacity
> statement. See `docs/Load-and-Reliability-Test-Report.md` for what is and is not covered.

---

## 1. Host snapshot

Captured live with `scripts/diagnostics/system-snapshot.ps1` (read-only; makes no changes).

### 1.1 CPU / RAM

| Resource | Value |
|---|---|
| CPU | AMD Ryzen 7 9800X3D |
| Cores / threads | 8 cores / 16 logical processors |
| RAM total | 31.1 GB |
| RAM free (at snapshot) | 15.8 GB |

### 1.2 Disk

| Drive | Free | Total |
|---|---|---|
| C: | 285 GB | 476 GB |
| D: | 1404 GB | 1863 GB |

### 1.3 GPU / VRAM (optional)

Captured with `scripts/diagnostics/gpu-health-check.ps1`. The GPU is **optional** — the platform
runs fully in MSSQL-only mode without it.

| Property | Value |
|---|---|
| GPU | NVIDIA RTX 5070 Ti |
| VRAM | 16 GB |
| Driver | 596.36 |
| Idle temperature | ~44 C |
| Utilisation (idle/light) | 9–14% |

### 1.4 Local inference (optional)

Captured with `scripts/diagnostics/ollama-health-check.ps1`.

| Property | Value |
|---|---|
| Ollama | Online |
| Models present | `qwen2.5-coder:14b`, `deepseek-r1:14b` |

### 1.5 Database

Captured with `scripts/diagnostics/sql-health-check.ps1`.

| Property | Value |
|---|---|
| Engine | Microsoft SQL Server via LocalDB (`(localdb)\MSSQLLocalDB`) |
| Mode | MSSQL-authoritative; Ollama/Qdrant optional |

---

## 2. Build, test, benchmark and reliability timings

These were measured on the host above. Build and test timings depend heavily on machine, disk and
warm/cold caches — treat them as a baseline for *this* host, reproducible release-over-release with
`scripts/diagnostics/performance-profile.ps1`.

| Gate | Result | How measured |
|---|---|---|
| Release build | 0 errors | `dotnet build LocalAIFactory.sln -c Release` |
| Unit-test suite (235 tests) | ~1 s, 0 failures | `dotnet test … --no-build` |
| Benchmark smoke suite | PASS | `dotnet run -- --inmemory --suite smoke` |
| Reliability smoke (4 iterations) | avg 1.00 s (min 0.90 s, max 1.27 s), 0 failures | `scripts/tests/reliability-smoke.ps1` |

### 2.1 Reliability smoke detail

The reliability smoke runs a fast, in-memory subset of the unit tests repeatedly and reports
per-iteration timing and pass/fail. Over 4 iterations: average **1.00 s**, minimum **0.90 s**,
maximum **1.27 s**, **0** failures. The spread (0.90–1.27 s) is normal warm/cold variance on a
shared developer machine, not a stability concern at this scale.

### 2.2 Prior-sprint packaging evidence (for context)

Recorded in an earlier sprint, included here for completeness; re-verify before relying on it.

| Artifact | Value |
|---|---|
| `dotnet publish` output | 142 files / 45 MB |
| Database backup | 69.5 MB |
| `RESTORE VERIFYONLY` | OK |

---

## 3. How to reproduce

All diagnostics scripts are **read-only** and make no system changes. Run from the repository root
in PowerShell.

```powershell
# One-shot host snapshot (CPU / RAM / disk / GPU / top processes)
pwsh scripts/diagnostics/system-snapshot.ps1
pwsh scripts/diagnostics/system-snapshot.ps1 -Json   # machine-readable for a support bundle

# Component health (each degrades gracefully when the component is absent)
pwsh scripts/diagnostics/gpu-health-check.ps1
pwsh scripts/diagnostics/ollama-health-check.ps1
pwsh scripts/diagnostics/sql-health-check.ps1

# Timing baseline: build / full test / benchmark smoke wall-clock
pwsh scripts/diagnostics/performance-profile.ps1

# Reliability + bounded load smokes
pwsh scripts/tests/reliability-smoke.ps1 -Iterations 4
pwsh scripts/tests/load-smoke.ps1 -Iterations 3
```

Capture the console output (and the `-Json` snapshot) into a support bundle when raising a
performance question — see `docs/Support-Runbook.md`.

---

## 4. What these numbers do and do not prove

**Do prove:** the solution builds cleanly, the full unit-test suite runs in about a second, the
benchmark smoke suite passes, and a repeated run is stable on this host. The GPU/Ollama stack is
present and reachable, but the core platform does not depend on it.

**Do not prove:** behaviour under concurrent multi-user load, large-repository import under memory
pressure, sustained throughput, or production-host performance. Those require the proofs listed in
`docs/Load-and-Reliability-Test-Report.md`. No production deployment performance has been measured
(see `docs/Known-Limitations.md` §5).
