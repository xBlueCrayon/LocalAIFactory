# LocalAIFactory ERP-Generation Self-Evaluation

**Date:** 2026-06-21 · **Product generated:** LAF Enterprise ERP (clean-room)

This is an honest accounting of **how** the ERP was produced and how much LocalAIFactory's own
capabilities contributed versus direct authored coding. No hiding manual effort.

## What LocalAIFactory capabilities were used

| Capability | Used? | How |
|---|---|---|
| Knowledge study from official docs (the LocalAIFactory pattern) | Yes | Two research passes built `benchmarks/erpnext-study/*` (sources, features, doctypes, APIs, workflows, reports, roles) and the architecture study from ERPNext/Frappe **official docs** — the same "study a system before building" loop LocalAIFactory is designed for. |
| Integration-expectation library | Indirect | The existing `erpnext-frappe-rest` expectation modelled the REST conventions reused in the design. |
| Issue-fix / production-issue knowledge | Indirect | Anti-patterns the LocalAIFactory packs warn about (e.g. `GroupBy(_=>1)`, blocking reads, decimal handling) were avoided by construction. |
| Benchmark/parity discipline | Yes | The parity-target-matrix → parity-score workflow mirrors LocalAIFactory's honest-scoring gates. |
| Local LLM | **No** | No local model inference was invoked for this build. The code was authored directly. |
| Qdrant / embeddings | No | Not used. |

## Honest split: generated-with-LocalAIFactory vs directly authored

- **Domain understanding (Phases 1–2):** produced via LocalAIFactory-style research subagents against
  official docs — this is the part most attributable to the platform's method.
- **Implementation (Phases 3–9):** the C#/.NET code, EF model, services, tests, UI and Playwright were
  **directly authored** by the orchestrator, not emitted by a local model. Subagents drafted the
  descriptive **documentation**, not the compiling code.
- **Parity/score/reviews:** authored directly with honest self-assessment.

So: the **method** (study → model → build → test → honest-score) is LocalAIFactory's; the **code
volume** was hand-authored. The local-LLM code-generation path was **not** exercised here.

## Code volume (approx)

- 5 .NET projects, ~35 entities, ~16 services, 42 DB tables.
- 74 xUnit tests + 12 Playwright tests, all passing.
- ~9 of ERPNext's modules touched; overall ERPNext-grade ≈ 36%.

## Where LocalAIFactory helped

- The disciplined **study-first** approach produced an accurate domain model (docstatus lifecycle,
  immutable GL/SLE, maker/checker) that matched the official behavior on the first build.
- The **honesty gates** culture kept the parity score conservative and the gaps explicit.

## Where it fell short (must improve in LocalAIFactory)

1. **No end-to-end local-LLM code generation** was used — the platform does not yet reliably emit a
   compiling multi-project .NET solution from a spec. This is the biggest gap to close for a true
   "AI generated the app" claim.
2. **No automated scaffolding** from the parity matrix to code stubs — the mapping was manual.
3. **No automated parity-score computation** from test results — the score was authored, not measured
   by a tool. A future LocalAIFactory feature should derive parity from passing tests per feature row.
4. The integration/knowledge packs informed design but were **not** wired into an automated generator.

## Impact on confidence

This proves LocalAIFactory's **research-and-modelling** loop is strong and that a real, tested ERP core
can be produced under its method. It does **not** yet prove autonomous local-LLM app generation. The
honest claim is: *"LocalAIFactory's study/benchmark/honesty method drove a hand-authored, tested
clean-room ERP core at ~36% ERPNext-grade,"* not *"LocalAIFactory autonomously generated an ERP."*
