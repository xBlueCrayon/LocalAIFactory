# LAF ScreenStream Assist — Local LLM Evaluation

**Status: LAN_READY.** This report records how a local LLM was (and was not) used to
build the screen-sharing sample.

## Setup

- Model: `qwen2.5-coder:14b` (local).
- Task: decompose the teen prompt, identify security requirements, and surface the NAT blocker.
- Evidence: `benchmarks/results/screenstream-local-llm-eval.json`.

## Result

In 15.5 seconds the model correctly surfaced:

- **Token authentication** for the stream.
- The **NAT / port-forward blocker** for internet use.
- The **consent / visible-client / disconnect** rules.

## Conclusion

The LLM is useful for **reasoning and review** — it independently identified the right
auth, network, and consent concerns. For **code generation**, deterministic templates plus
the LocalAIFactory knowledge packs were used because they produce a compiling, tested
solution reliably. The LLM was **not** allowed to write product source directly.

This is the intended division of labor: LLM for judgment, deterministic generation for code.
