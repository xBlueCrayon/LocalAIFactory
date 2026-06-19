using System.Text;
using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Rag.Embeddings;

public sealed class OllamaEmbeddingProvider : IEmbeddingProvider
{
    private readonly IHttpClientFactory _http;
    public ModelProvider Provider => ModelProvider.Ollama;
    public OllamaEmbeddingProvider(IHttpClientFactory http) => _http = http;

    public async Task<EmbeddingResult> EmbedAsync(string baseUrl, string? apiKey, string model, string text, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("ollama");
            client.Timeout = TimeSpan.FromMinutes(2);
            var payload = JsonSerializer.Serialize(new { model, prompt = text });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = baseUrl.TrimEnd('/') + "/api/embeddings";
            using var resp = await client.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) return EmbeddingResult.Fail($"Ollama embeddings HTTP {(int)resp.StatusCode}: {body}");
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("embedding", out var arr) || arr.ValueKind != JsonValueKind.Array)
                return EmbeddingResult.Fail("Ollama embeddings: no 'embedding' array in response.");
            var vec = new float[arr.GetArrayLength()];
            int i = 0;
            foreach (var n in arr.EnumerateArray()) vec[i++] = (float)n.GetDouble();
            return EmbeddingResult.Ok(vec);
        }
        catch (Exception ex) { return EmbeddingResult.Fail("Ollama embeddings error: " + ex.Message); }
    }
}
