# LAF GPU / Ollama Orchestration — Report

**Stamp:** 2026-06-21
**Component:** `LocalAIFactory.Reasoning/LocalModels/GpuAwareOrchestration`
**Telemetry:** `benchmarks/results/laf-gpu-ollama-telemetry.json`

## Purpose

Use a local GPU and Ollama **when present**, without ever making them a hard dependency. Heavy
model calls must be serialised and bounded so a GPU OOM or a slow model cannot spiral.

## GPU detection (`GpuCapabilityDetector`)

Reads environment signals — `CUDA_VISIBLE_DEVICES`, `HIP_VISIBLE_DEVICES`, `OLLAMA_GPU_LAYERS`,
`GPU_DEVICE_ORDINAL` — with **no hard dependency**. An absent or `-1` signal means "assume CPU".
Nothing in the engine depends on a GPU.

## Orchestration (`GpuAwareOrchestrator`)

| Behaviour | Detail |
| --- | --- |
| Serialised heavy calls | `SemaphoreSlim(1,1)` run-queue (avoids GPU OOM loops) |
| Per-run token budget | yes |
| Per-run timeout | yes |
| Max prompt chars | 8000 (over-large prompts are split) |
| Retry on failure | retry **SMALLER** (half prompt + fewer tokens) |
| Retry bound | `MaxRetries = 2` — bounded, never infinite |
| Telemetry | every attempt recorded (`OrchestrationTelemetry`) |
| Graceful unavailable | with no Ollama/GPU every call reports `Available=false` and the deterministic caller falls back |

## Test result

Tested **without a GPU and without Ollama**. The run-queue, token budget, timeout, bounded-retry,
and graceful-degradation behaviour are validated. Core behaviour requires no GPU.

## Honest limitations / not met

- **No live-GPU throughput or latency numbers are claimed.** Validation is of the orchestration
  logic (queue, budgets, bounded retries, degradation), not of real GPU performance.
- The orchestrator was **not exercised against a live Ollama** in this benchmark; behaviour with a
  real model is inferred from the router contract, not measured here.
- GPU detection is **signal-based** (environment variables), not a hardware probe; a misconfigured
  environment could under- or over-report GPU presence, which is why the engine never depends on it.
