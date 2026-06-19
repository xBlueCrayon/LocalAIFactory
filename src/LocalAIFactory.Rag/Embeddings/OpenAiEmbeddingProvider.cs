using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Rag.Embeddings;

public sealed class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly IHttpClientFactory _http;
    public ModelProvider Provider => ModelProvider.OpenAi;
    public OpenAiEmbeddingProvider(IHttpClientFactory http) => _http = http;

    public async Task<EmbeddingResult> EmbedAsync(string baseUrl, string? apiKey, string model, string text, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("openai");
            client.Timeout = TimeSpan.FromMinutes(2);
            if (!string.IsNullOrEmpty(apiKey))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
            var payload = JsonSerializer.Serialize(new { model, input = text });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = baseUrl.TrimEnd('/') + "/embeddings";
            using var resp = await client.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) return EmbeddingResult.Fail($"OpenAI embeddings HTTP {(int)resp.StatusCode}: {body}");
            using var doc = JsonDocument.Parse(body);
            var arr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
            var vec = new float[arr.GetArrayLength()];
            int i = 0;
            foreach (var n in arr.EnumerateArray()) vec[i++] = (float)n.GetDouble();
            return EmbeddingResult.Ok(vec);
        }
        catch (Exception ex) { return EmbeddingResult.Fail("OpenAI embeddings error: " + ex.Message); }
    }
}
