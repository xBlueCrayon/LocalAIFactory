# Load and Reliability Test Report

What has actually been run for reliability and bounded load this sprint, the method, the results,
and — stated plainly — **what is not yet tested** and the exact proof that would close each gap.

> Honesty note: these are **smoke** tests. They establish that fast operations are stable and
> repeatable on one host. They are **not** a concurrency, soak, or capacity test. Do not cite them
> as evidence of production load behaviour.

---

## 1. Reliability smoke

### Method

`scripts/tests/reliability-smoke.ps1` repeats a fast, in-memory subset of the unit tests N times and
reports per-iteration wall-clock and pass/fail. It uses no external services and performs no
destructive action, so it is safe on any developer machine. The subset targets the chat-learning,
licensing, and local-fix-loop tests.

```powershell
pwsh scripts/tests/reliability-smoke.ps1 -Iterations 4
```

### Result (this sprint)

| Metric | Value |
|---|---|
| Iterations | 4 |
| Failures | 0 |
| Average | 1.00 s |
| Minimum | 0.90 s |
| Maximum | 1.27 s |

**Interpretation:** stable, no flakiness, no timing drift across iterations at this scale. The
0.90–1.27 s spread is ordinary warm/cold variance on a shared machine.

---

## 2. Load smoke (bounded)

### Method

`scripts/tests/load-smoke.ps1` runs the in-memory benchmark smoke suite repeatedly and reports
per-run timing and PASS/FAIL. It is a **safe, bounded** repeat — no network, no destructive action,
no production load. It documents the host's repeatable throughput on a known workload.

```powershell
pwsh scripts/tests/load-smoke.ps1 -Iterations 3
```

### Result (this sprint)

| Metric | Value |
|---|---|
| Benchmark smoke suite | PASS |
| Repeated runs | PASS, consistent timing |

The benchmark smoke suite passed on each repetition. This confirms the suite is deterministic and
repeatable on this host; it is **not** a measure of concurrent or sustained load.

---

## 3. Supporting evidence

From `docs/Resource-and-Performance-Evidence.md` (same host, same sprint):

- Full unit-test suite: 235 tests, ~1 s, 0 failures.
- Release build: 0 errors.
- Benchmark smoke suite: PASS.

---

## 4. What is NOT tested yet (and the proof to close it)

These gaps are real. Each lists the **exact, observable proof** that would let us claim it closed —
nothing is hand-waved away.

| Gap | Why it matters | Proof to close |
|---|---|---|
| **Concurrent multi-user load** | All measurements above are single-threaded/single-user. Behaviour under many simultaneous authenticated users (request contention, DB connection pool, dashboard parallel reads) is unmeasured. | A load harness driving N concurrent authenticated sessions against the core pages, with p50/p95 latency and error rate reported, and `RequestTimingMiddleware` showing no hung requests. |
| **Large-repository import under load** | Memory and timing while importing a very large repository — especially with other activity in flight — are uncharacterised. | An import of a large, representative repository captured with `system-snapshot.ps1` before/during/after (RAM/CPU), import duration, and post-import core-page smoke timings unchanged. |
| **Sustained soak / endurance** | Short smokes do not reveal slow leaks or drift over hours. | A multi-hour soak running the smoke workload on a loop with stable memory and timing, no hung requests in the logs. |
| **SQL Express / production-host behaviour** | Validation ran on LocalDB; SQL Express and a real IIS host were not exercised. | The same smoke + benchmark run on SQL Express and on a representative IIS host, with core-page smoke checks passing there (`docs/Known-Limitations.md` §5). |
| **Concurrent autonomous fix-loop runs** | The fix loop was tested for safety, not for concurrency. | Multiple concurrent fix-loop runs against isolated workspaces with no cross-contamination and correct rollback per run. |

---

## 5. How to extend this report

When any gap above is closed, append the method and the captured numbers here (or link a results
file), keeping the same discipline: state the host, the dataset, the exact command, and the raw
output. Do not promote a smoke result to a load/soak claim — they answer different questions.

Until then, the honest summary is: **fast operations are stable and repeatable on one host;
concurrency, large-import, soak, and production-host load remain unproven.**
