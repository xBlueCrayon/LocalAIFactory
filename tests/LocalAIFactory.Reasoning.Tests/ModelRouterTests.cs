using LocalAIFactory.Reasoning.LocalModels;
using Xunit;

namespace LocalAIFactory.Reasoning.Tests;

public class ModelRouterTests
{
    // A controllable fake client so router behaviour is testable without Ollama.
    private sealed class FakeClient : ILocalModelClient
    {
        private readonly string[] _installed;
        private readonly bool _throw;
        public FakeClient(string[] installed, bool @throw = false) { _installed = installed; _throw = @throw; }
        public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default)
            => _throw ? throw new System.Exception("down") : Task.FromResult<IReadOnlyList<string>>(_installed);
        public Task<ModelResponse> GenerateAsync(string model, string prompt, int maxTokens, System.TimeSpan timeout, CancellationToken ct = default)
            => _throw ? throw new System.Exception("down") : Task.FromResult(new ModelResponse(true, model, "reviewed: " + prompt[..System.Math.Min(5, prompt.Length)], 12));
    }

    [Fact] public async Task Absent_backend_review_is_unavailable_not_exception()
    {
        var r = new LocalModelRouter(); // NullModelClient default
        var resp = await r.ReviewAsync(ModelRole.CodeReviewer, "review this");
        Assert.False(resp.Available);
        Assert.NotNull(resp.Error);
    }

    [Fact] public async Task Absent_backend_available_list_is_empty()
        => Assert.Empty(await new LocalModelRouter().AvailableAsync());

    [Fact] public void Role_selection_maps_qwen_to_code_reviewer()
        => Assert.Equal("qwen2.5-coder:14b", new LocalModelRouter().SelectModel(ModelRole.CodeReviewer));

    [Fact] public void Role_selection_maps_deepseek_to_planner()
        => Assert.Equal("deepseek-r1:14b", new LocalModelRouter().SelectModel(ModelRole.Planner));

    [Fact] public async Task Available_reflects_installed_models()
    {
        var r = new LocalModelRouter(new FakeClient(new[] { "qwen2.5-coder:14b" }));
        var avail = await r.AvailableAsync();
        Assert.Contains("qwen2.5-coder:14b", avail);
        Assert.DoesNotContain("deepseek-r1:14b", avail);
    }

    [Fact] public async Task Healthy_when_list_succeeds()
        => Assert.True(await new LocalModelRouter(new FakeClient(new[] { "x" })).IsHealthyAsync());

    [Fact] public async Task Unhealthy_when_client_throws()
        => Assert.False(await new LocalModelRouter(new FakeClient(System.Array.Empty<string>(), @throw: true)).IsHealthyAsync());

    [Fact] public async Task Review_with_fake_client_returns_text()
    {
        var r = new LocalModelRouter(new FakeClient(new[] { "qwen2.5-coder:14b" }));
        var resp = await r.ReviewAsync(ModelRole.CodeReviewer, "review the lockout code");
        Assert.True(resp.Available);
        Assert.StartsWith("reviewed:", resp.Text);
    }

    [Fact] public async Task Throwing_client_degrades_to_unavailable()
    {
        var r = new LocalModelRouter(new FakeClient(new[] { "qwen2.5-coder:14b" }, @throw: true));
        var resp = await r.ReviewAsync(ModelRole.CodeReviewer, "x");
        Assert.False(resp.Available);
    }

    [Fact] public async Task Every_call_is_logged()
    {
        var r = new LocalModelRouter(new FakeClient(new[] { "qwen2.5-coder:14b" }));
        await r.ReviewAsync(ModelRole.CodeReviewer, "a");
        await r.ReviewAsync(ModelRole.Planner, "b");
        Assert.Equal(2, r.Log.Count);
    }

    [Fact] public async Task Role_without_model_is_handled()
    {
        var r = new LocalModelRouter(new FakeClient(System.Array.Empty<string>()), roster: System.Array.Empty<ModelDescriptor>());
        var resp = await r.ReviewAsync(ModelRole.CodeReviewer, "x");
        Assert.False(resp.Available);
        Assert.Contains("no model", resp.Error);
    }
}
