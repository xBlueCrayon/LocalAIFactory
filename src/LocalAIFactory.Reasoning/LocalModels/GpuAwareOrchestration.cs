namespace LocalAIFactory.Reasoning.LocalModels;

public sealed record GpuSignal(bool LikelyGpu, string Evidence);
public sealed record OrchestrationTelemetry(string Model, ModelRole Role, bool Available, long ElapsedMs, int Attempt, string? Error);

/// <summary>
/// Best-effort GPU detection WITHOUT a hard dependency. Reads environment signals (CUDA/HIP/Ollama GPU layers).
/// Never required: an empty/unknown signal means "assume CPU", and nothing in the engine depends on a GPU.
/// </summary>
public sealed class GpuCapabilityDetector
{
    private readonly Func<string, string?> _env;
    public GpuCapabilityDetector(Func<string, string?>? env = null) => _env = env ?? Environment.GetEnvironmentVariable;

    public GpuSignal Detect()
    {
        foreach (var (var, label) in new[] { ("CUDA_VISIBLE_DEVICES", "CUDA"), ("HIP_VISIBLE_DEVICES", "ROCm"), ("OLLAMA_GPU_LAYERS", "Ollama GPU layers"), ("GPU_DEVICE_ORDINAL", "GPU ordinal") })
        {
            var v = _env(var);
            if (!string.IsNullOrWhiteSpace(v) && v != "-1") return new GpuSignal(true, $"{label}={v}");
        }
        return new GpuSignal(false, "no GPU env signal (assuming CPU)");
    }
}

/// <summary>
/// GPU-aware orchestration over the <see cref="LocalModelRouter"/>: serialises heavy calls through a run queue,
/// enforces a per-run token budget, splits an over-large prompt and retries SMALLER on failure (bounded retries,
/// never infinite), and records telemetry. Degrades gracefully — with no Ollama/GPU every call is unavailable and
/// the deterministic caller falls back. Core behaviour requires no GPU.
/// </summary>
public sealed class GpuAwareOrchestrator
{
    private readonly LocalModelRouter _router;
    private readonly GpuCapabilityDetector _gpu;
    private readonly SemaphoreSlim _queue = new(1, 1); // serialise heavy model calls (avoid GPU OOM loops)
    private readonly List<OrchestrationTelemetry> _telemetry = new();
    public int MaxRetries { get; init; } = 2;
    public int MaxPromptChars { get; init; } = 8000;

    public GpuAwareOrchestrator(LocalModelRouter? router = null, GpuCapabilityDetector? gpu = null)
    { _router = router ?? new LocalModelRouter(); _gpu = gpu ?? new GpuCapabilityDetector(); }

    public IReadOnlyList<OrchestrationTelemetry> Telemetry => _telemetry;
    public GpuSignal Gpu => _gpu.Detect();

    public async Task<ModelResponse> RunAsync(ModelRole role, string prompt, int maxTokens = 256, int timeoutSeconds = 60, CancellationToken ct = default)
    {
        await _queue.WaitAsync(ct);
        try
        {
            string current = prompt;
            ModelResponse resp = new(false, "(none)", "", 0, "not attempted");
            for (int attempt = 1; attempt <= MaxRetries + 1; attempt++)
            {
                if (current.Length > MaxPromptChars) current = current[..MaxPromptChars]; // split: keep within budget
                resp = await _router.ReviewAsync(role, current, maxTokens, timeoutSeconds, ct);
                _telemetry.Add(new OrchestrationTelemetry(resp.Model, role, resp.Available, resp.ElapsedMs, attempt, resp.Error));
                if (resp.Available) return resp;
                // retry SMALLER (half the prompt + fewer tokens), bounded by MaxRetries
                current = current.Length > 200 ? current[..(current.Length / 2)] : current;
                maxTokens = Math.Max(64, maxTokens / 2);
            }
            return resp; // unavailable after bounded retries — caller falls back deterministically
        }
        finally { _queue.Release(); }
    }
}
