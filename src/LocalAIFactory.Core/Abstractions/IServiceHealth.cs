using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// Phase 1.2: an immutable, instantly-readable view of optional-service health.
// Produced by a background monitor and read by the dashboard so page rendering never
// performs (or waits on) an external service call.
public sealed record ServiceHealthSnapshot
{
    public ServiceState Qdrant { get; init; } = ServiceState.Unknown;
    public ServiceState Ollama { get; init; } = ServiceState.Unknown;
    public ServiceState Embeddings { get; init; } = ServiceState.Unknown;
    public EnvironmentMode Mode { get; init; } = EnvironmentMode.Minimal;

    // True when chat can produce a response: a reachable local model or an enabled cloud model.
    public bool ChatAvailable { get; init; } = true;

    // Null until the first background probe completes.
    public DateTime? LastCheckedUtc { get; init; }

    public string ModeLabel => Mode switch
    {
        EnvironmentMode.FullAi => "Full AI",
        EnvironmentMode.Standard => "Standard",
        _ => "Minimal"
    };

    public static string Label(ServiceState s) => s switch
    {
        ServiceState.Online => "Online",
        ServiceState.Offline => "Offline",
        ServiceState.Disabled => "Disabled",
        _ => "Checking…"
    };
}

// Thread-safe holder for the latest health snapshot. Registered as a singleton.
// Readers get the current snapshot with no locking and no I/O.
public interface IServiceHealthCache
{
    ServiceHealthSnapshot Current { get; }
    void Set(ServiceHealthSnapshot snapshot);
}
