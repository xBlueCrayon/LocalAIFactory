using System.Text;
using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Options;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Rag.Vector;

// Talks to Qdrant over its REST API (no gRPC dependency). Degrades quietly when Qdrant is down.
public sealed class QdrantVectorStore : IVectorStore
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly QdrantOptions _opt;
    private readonly RagOptions _rag;

    public QdrantVectorStore(IHttpClientFactory httpFactory, IOptions<QdrantOptions> opt, IOptions<RagOptions> rag)
    {
        _httpFactory = httpFactory; _opt = opt.Value; _rag = rag.Value;
    }

    // Phase 1.2 hotfix: a single authoritative gate. Qdrant is contacted ONLY when it is enabled AND
    // vector search is on. Turning EITHER flag off guarantees zero Qdrant HTTP calls from every method.
    private bool VectorEnabled => _opt.Enabled && _rag.UseVectorSearch;

    private HttpClient Client()
    {
        var c = _httpFactory.CreateClient("qdrant");
        c.Timeout = TimeSpan.FromSeconds(30);
        if (!string.IsNullOrEmpty(_opt.ApiKey)) c.DefaultRequestHeaders.TryAddWithoutValidation("api-key", _opt.ApiKey);
        return c;
    }

    private string Base => _opt.BaseUrl.TrimEnd('/');

    public async Task<bool> HealthAsync(CancellationToken ct = default)
    {
        if (!VectorEnabled) return false;
        try
        {
            // Phase 1.2: health must be fast — cap at 2s even though the operation client allows 30s.
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(2));
            using var resp = await Client().GetAsync(Base + "/collections", cts.Token);
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task EnsureCollectionAsync(int vectorSize, CancellationToken ct = default)
    {
        if (!VectorEnabled) return;
        var client = Client();
        var url = $"{Base}/collections/{_opt.Collection}";
        try
        {
            using var get = await client.GetAsync(url, ct);
            if (get.IsSuccessStatusCode) return;
        }
        catch { /* fall through to create */ }

        var body = JsonSerializer.Serialize(new { vectors = new { size = vectorSize, distance = "Cosine" } });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var put = await client.PutAsync(url, content, ct);
        put.EnsureSuccessStatusCode();
    }

    public async Task UpsertAsync(string id, float[] vector, IDictionary<string, object> payload, CancellationToken ct = default)
    {
        if (!VectorEnabled) return;
        var url = $"{Base}/collections/{_opt.Collection}/points";
        var body = JsonSerializer.Serialize(new
        {
            points = new[] { new { id, vector, payload } }
        });
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp = await Client().PutAsync(url, content, ct);
        resp.EnsureSuccessStatusCode();
    }

    public async Task DeleteAsync(IEnumerable<string> ids, CancellationToken ct = default)
    {
        if (!VectorEnabled) return;
        var list = ids?.Where(s => !string.IsNullOrWhiteSpace(s)).Distinct().ToArray() ?? Array.Empty<string>();
        if (list.Length == 0) return;
        try
        {
            var url = $"{Base}/collections/{_opt.Collection}/points/delete";
            var body = JsonSerializer.Serialize(new { points = list });
            using var content = new StringContent(body, Encoding.UTF8, "application/json");
            using var resp = await Client().PostAsync(url, content, ct);
            // best-effort: ignore non-success (e.g. collection missing)
        }
        catch { /* quiet: deprecation/delete must not fail because the vector store is down */ }
    }

    public async Task<IReadOnlyList<VectorSearchHit>> SearchAsync(float[] vector, int topK, int? projectId, CancellationToken ct = default)
    {
        var hits = new List<VectorSearchHit>();
        if (!VectorEnabled) return hits;

        object request;
        if (projectId is int pid)
        {
            request = new
            {
                vector,
                limit = topK,
                with_payload = true,
                filter = new
                {
                    should = new object[]
                    {
                        new { key = "projectId", match = new { value = pid } },
                        new { key = "projectId", match = new { value = 0 } }
                    }
                }
            };
        }
        else
        {
            request = new { vector, limit = topK, with_payload = true };
        }

        var url = $"{Base}/collections/{_opt.Collection}/points/search";
        var body = JsonSerializer.Serialize(request);
        using var content = new StringContent(body, Encoding.UTF8, "application/json");
        using var resp = await Client().PostAsync(url, content, ct);
        var text = await resp.Content.ReadAsStringAsync(ct);
        if (!resp.IsSuccessStatusCode) return hits;

        using var doc = JsonDocument.Parse(text);
        if (!doc.RootElement.TryGetProperty("result", out var result) || result.ValueKind != JsonValueKind.Array)
            return hits;

        foreach (var r in result.EnumerateArray())
        {
            var hit = new VectorSearchHit();
            if (r.TryGetProperty("id", out var idEl))
                hit.Id = idEl.ValueKind == JsonValueKind.String ? (idEl.GetString() ?? "") : idEl.ToString();
            if (r.TryGetProperty("score", out var sc) && sc.ValueKind == JsonValueKind.Number)
                hit.Score = sc.GetDouble();
            if (r.TryGetProperty("payload", out var pl) && pl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in pl.EnumerateObject())
                    hit.Payload[prop.Name] = ToClr(prop.Value);
            }
            hits.Add(hit);
        }
        return hits;
    }

    private static object ToClr(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => e.GetString() ?? "",
        JsonValueKind.Number => e.TryGetInt64(out var l) ? l : e.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => e.ToString()
    };
}
