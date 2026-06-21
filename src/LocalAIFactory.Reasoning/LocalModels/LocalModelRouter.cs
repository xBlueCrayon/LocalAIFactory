namespace LocalAIFactory.Reasoning.LocalModels;

public enum ModelRole { CodeReviewer, Planner, AgentReviewer, Embedding }

public sealed record ModelDescriptor(string Name, ModelRole Role);
public sealed record ModelResponse(bool Available, string Model, string Text, long ElapsedMs, string? Error = null);
public sealed record ModelCallLogEntry(string Model, ModelRole Role, bool Available, long ElapsedMs, string? Error);

/// <summary>Abstraction over a local model backend (Ollama). Implementations must never throw; absence is a value, not an exception.</summary>
public interface ILocalModelClient
{
    Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default);
    Task<ModelResponse> GenerateAsync(string model, string prompt, int maxTokens, TimeSpan timeout, CancellationToken ct = default);
}

/// <summary>A deterministic offline client: no Ollama, every call reports unavailable. Used as the default fallback and in tests.</summary>
public sealed class NullModelClient : ILocalModelClient
{
    public Task<IReadOnlyList<string>> ListModelsAsync(CancellationToken ct = default) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());
    public Task<ModelResponse> GenerateAsync(string model, string prompt, int maxTokens, TimeSpan timeout, CancellationToken ct = default) =>
        Task.FromResult(new ModelResponse(false, model, "", 0, "no local model backend"));
}

/// <summary>
/// Routes reasoning/review requests to a local model by role, with health probing, prompt budget, timeout,
/// fallback and an evaluation log. Degrades gracefully: when no model is installed every call returns an
/// unavailable response and the caller falls back to deterministic reasoning. Never fails core behaviour.
/// </summary>
public sealed class LocalModelRouter
{
    private readonly ILocalModelClient _client;
    private readonly List<ModelCallLogEntry> _log = new();

    // Default role -> model mapping (overridable). qwen for code, deepseek for planning/review.
    public IReadOnlyList<ModelDescriptor> Roster { get; }

    public LocalModelRouter(ILocalModelClient? client = null, IReadOnlyList<ModelDescriptor>? roster = null)
    {
        _client = client ?? new NullModelClient();
        Roster = roster ?? new[]
        {
            new ModelDescriptor("qwen2.5-coder:14b", ModelRole.CodeReviewer),
            new ModelDescriptor("deepseek-r1:14b", ModelRole.Planner),
            new ModelDescriptor("deepseek-r1:14b", ModelRole.AgentReviewer),
            new ModelDescriptor("nomic-embed-text", ModelRole.Embedding),
        };
    }

    public IReadOnlyList<ModelCallLogEntry> Log => _log;

    public async Task<bool> IsHealthyAsync(CancellationToken ct = default)
    {
        try { return (await _client.ListModelsAsync(ct)).Count >= 0; } catch { return false; }
    }

    public string? SelectModel(ModelRole role)
    {
        var d = Roster.FirstOrDefault(m => m.Role == role);
        return d?.Name;
    }

    /// <summary>Returns which roster models are actually installed (empty when Ollama is absent).</summary>
    public async Task<IReadOnlyList<string>> AvailableAsync(CancellationToken ct = default)
    {
        IReadOnlyList<string> installed;
        try { installed = await _client.ListModelsAsync(ct); } catch { installed = Array.Empty<string>(); }
        var set = new HashSet<string>(installed, StringComparer.OrdinalIgnoreCase);
        return Roster.Select(r => r.Name).Distinct().Where(set.Contains).ToList();
    }

    /// <summary>
    /// Reviews a prompt with the model assigned to a role, honouring a token budget and timeout. Returns an
    /// unavailable response (never throws) on absence/timeout/failure so the caller can fall back deterministically.
    /// </summary>
    public async Task<ModelResponse> ReviewAsync(ModelRole role, string prompt, int maxTokens = 256, int timeoutSeconds = 60, CancellationToken ct = default)
    {
        var model = SelectModel(role);
        if (model is null)
        {
            var miss = new ModelResponse(false, "(none)", "", 0, $"no model assigned to role {role}");
            _log.Add(new ModelCallLogEntry("(none)", role, false, 0, miss.Error));
            return miss;
        }
        ModelResponse resp;
        try { resp = await _client.GenerateAsync(model, prompt, maxTokens, TimeSpan.FromSeconds(timeoutSeconds), ct); }
        catch (Exception ex) { resp = new ModelResponse(false, model, "", 0, ex.Message); }
        _log.Add(new ModelCallLogEntry(model, role, resp.Available, resp.ElapsedMs, resp.Error));
        return resp;
    }
}
