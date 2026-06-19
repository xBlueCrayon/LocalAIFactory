using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Web.Hosted;

// Phase 1.2: periodically probes optional AI infrastructure (Qdrant, Ollama) with short timeouts and
// publishes a cached snapshot. The dashboard reads the snapshot instantly — it never probes on render.
// Every probe is bounded to ~1s so a missing/black-holed service can never delay anything.
public sealed class HealthMonitorService : BackgroundService
{
    private static readonly TimeSpan ProbeTimeout = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(15);

    private readonly IServiceHealthCache _cache;
    private readonly IHttpClientFactory _http;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly QdrantOptions _qdrant;
    private readonly OllamaOptions _ollama;
    private readonly RagOptions _rag;
    private readonly ILogger<HealthMonitorService> _log;

    public HealthMonitorService(
        IServiceHealthCache cache, IHttpClientFactory http, IServiceScopeFactory scopeFactory,
        IOptions<QdrantOptions> qdrant, IOptions<OllamaOptions> ollama, IOptions<RagOptions> rag,
        ILogger<HealthMonitorService> log)
    {
        _cache = cache; _http = http; _scopeFactory = scopeFactory;
        _qdrant = qdrant.Value; _ollama = ollama.Value; _rag = rag.Value; _log = log;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // First pass runs immediately so the dashboard has live data within ~1s of startup.
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await ProbeOnceAsync(stoppingToken); }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _log.LogDebug(ex, "Health probe iteration failed (non-fatal)."); }

            try { await Task.Delay(Interval, stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task ProbeOnceAsync(CancellationToken ct)
    {
        // Phase 1.2 hotfix: probe Qdrant ONLY when it is enabled AND vector search is on.
        // Either flag off => no Qdrant HTTP call at all (matches the vector store's gate).
        bool qdrantUsable = _qdrant.Enabled && _rag.UseVectorSearch;
        var qdrant = qdrantUsable ? await ProbeHttpAsync("qdrant", _qdrant.BaseUrl, "/collections", _qdrant.ApiKey, ct)
                                  : ServiceState.Disabled;

        var ollama = _ollama.Enabled ? await ProbeHttpAsync("ollama", _ollama.BaseUrl, "/api/tags", null, ct)
                                     : ServiceState.Disabled;

        bool hasCloudModel = false;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            hasCloudModel = await db.ModelConfigurations
                .AsNoTracking()
                .AnyAsync(m => m.IsEnabled && m.Provider != ModelProvider.Ollama, ct);
        }
        catch (Exception ex) { _log.LogDebug(ex, "Cloud-model availability check failed (non-fatal)."); }

        bool useOpenAi = string.Equals(_rag.EmbeddingProvider, "OpenAI", StringComparison.OrdinalIgnoreCase);
        ServiceState embeddings;
        if (!_rag.UseVectorSearch || !_qdrant.Enabled)
            embeddings = ServiceState.Disabled;
        else if (useOpenAi)
            embeddings = hasCloudModel ? ServiceState.Online : ServiceState.Offline;
        else
            embeddings = ollama; // Ollama-backed embeddings track the Ollama probe

        bool ollamaOnline = ollama == ServiceState.Online;
        bool qdrantOnline = qdrant == ServiceState.Online;
        var mode = (ollamaOnline && qdrantOnline) ? EnvironmentMode.FullAi
                 : ollamaOnline ? EnvironmentMode.Standard
                 : EnvironmentMode.Minimal;

        _cache.Set(new ServiceHealthSnapshot
        {
            Qdrant = qdrant,
            Ollama = ollama,
            Embeddings = embeddings,
            Mode = mode,
            ChatAvailable = ollamaOnline || hasCloudModel,
            LastCheckedUtc = DateTime.UtcNow
        });
    }

    private async Task<ServiceState> ProbeHttpAsync(string clientName, string baseUrl, string path, string? apiKey, CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(ProbeTimeout);

            var client = _http.CreateClient(clientName);
            client.Timeout = ProbeTimeout;
            using var req = new HttpRequestMessage(HttpMethod.Get, baseUrl.TrimEnd('/') + path);
            if (!string.IsNullOrEmpty(apiKey)) req.Headers.TryAddWithoutValidation("api-key", apiKey);

            using var resp = await client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cts.Token);
            return resp.IsSuccessStatusCode ? ServiceState.Online : ServiceState.Offline;
        }
        catch
        {
            return ServiceState.Offline;
        }
    }
}
