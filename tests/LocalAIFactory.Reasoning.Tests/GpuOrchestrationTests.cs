using LocalAIFactory.Reasoning.LocalModels;
using Xunit;

namespace LocalAIFactory.Reasoning.Tests;

public class GpuOrchestrationTests
{
    private sealed class FakeClient : ILocalModelClient
    {
        private readonly Queue<bool> _availability;
        public int Calls { get; private set; }
        public FakeClient(params bool[] availabilitySequence) => _availability = new Queue<bool>(availabilitySequence);
        public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<string>>(new[] { "qwen2.5-coder:14b" });
        public Task<ModelResponse> GenerateAsync(string model, string prompt, int maxTokens, System.TimeSpan timeout, CancellationToken ct = default)
        {
            Calls++;
            var ok = _availability.Count > 0 ? _availability.Dequeue() : false;
            return Task.FromResult(new ModelResponse(ok, model, ok ? "review" : "", ok ? 10 : 0, ok ? null : "busy"));
        }
    }

    [Fact] public void Gpu_detector_reports_no_signal_by_default()
        => Assert.False(new GpuCapabilityDetector(_ => null).Detect().LikelyGpu);

    [Theory]
    [InlineData("CUDA_VISIBLE_DEVICES", "0", true)]
    [InlineData("OLLAMA_GPU_LAYERS", "35", true)]
    [InlineData("CUDA_VISIBLE_DEVICES", "-1", false)]
    public void Gpu_detector_reads_env_signal(string var, string val, bool expected)
        => Assert.Equal(expected, new GpuCapabilityDetector(v => v == var ? val : null).Detect().LikelyGpu);

    [Fact] public async Task Orchestrator_returns_available_response_on_first_success()
    {
        var router = new LocalModelRouter(new FakeClient(true));
        var o = new GpuAwareOrchestrator(router);
        var r = await o.RunAsync(ModelRole.CodeReviewer, "review this");
        Assert.True(r.Available);
        Assert.Single(o.Telemetry);
    }

    [Fact] public async Task Orchestrator_retries_smaller_then_succeeds()
    {
        var router = new LocalModelRouter(new FakeClient(false, true)); // first fails, retry succeeds
        var o = new GpuAwareOrchestrator(router) { MaxRetries = 2 };
        var r = await o.RunAsync(ModelRole.CodeReviewer, new string('x', 5000));
        Assert.True(r.Available);
        Assert.Equal(2, o.Telemetry.Count); // one failed attempt + one success
    }

    [Fact] public async Task Orchestrator_bounded_retries_then_degrades()
    {
        var router = new LocalModelRouter(new FakeClient(false, false, false, false));
        var o = new GpuAwareOrchestrator(router) { MaxRetries = 2 };
        var r = await o.RunAsync(ModelRole.CodeReviewer, "x");
        Assert.False(r.Available);
        Assert.Equal(3, o.Telemetry.Count); // 1 + MaxRetries, never infinite
    }

    [Fact] public async Task Orchestrator_with_no_backend_degrades_without_throwing()
    {
        var o = new GpuAwareOrchestrator(); // NullModelClient default
        var r = await o.RunAsync(ModelRole.Planner, "plan");
        Assert.False(r.Available);
    }

    [Fact] public void Over_large_prompt_is_split_to_budget()
        => Assert.True(new GpuAwareOrchestrator().MaxPromptChars <= 8000);
}
