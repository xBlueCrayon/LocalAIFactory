using System.Text;
using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Agent.Providers;

public sealed class ClaudeChatProvider : IChatModelProvider
{
    private readonly IHttpClientFactory _http;
    public ModelProvider Provider => ModelProvider.Claude;
    public ClaudeChatProvider(IHttpClientFactory http) => _http = http;

    public async Task<ChatCompletionResult> CompleteAsync(ModelConfiguration config, ChatCompletionRequest request, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("chat");
            client.Timeout = TimeSpan.FromMinutes(5);
            client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", config.ApiKeyEncrypted ?? "");
            client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", "2023-06-01");

            var messages = request.Messages
                .Where(m => m.Role != ChatRole.System)
                .Select(m => new { role = m.Role == ChatRole.Assistant ? "assistant" : "user", content = m.Content })
                .ToList();
            if (messages.Count == 0) messages.Add(new { role = "user", content = request.SystemPrompt is null ? "" : "Proceed." });

            var payload = JsonSerializer.Serialize(new
            {
                model = request.Model,
                max_tokens = request.MaxTokens,
                temperature = request.Temperature,
                system = request.SystemPrompt ?? "",
                messages
            });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = config.BaseUrl.TrimEnd('/') + "/v1/messages";
            using var resp = await client.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) return ChatCompletionResult.Fail($"Claude HTTP {(int)resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var sb = new StringBuilder();
            if (doc.RootElement.TryGetProperty("content", out var blocks) && blocks.ValueKind == JsonValueKind.Array)
                foreach (var b in blocks.EnumerateArray())
                    if (b.TryGetProperty("type", out var t) && t.GetString() == "text" && b.TryGetProperty("text", out var tx))
                        sb.Append(tx.GetString());
            return ChatCompletionResult.Ok(sb.ToString());
        }
        catch (Exception ex) { return ChatCompletionResult.Fail("Claude error: " + ex.Message); }
    }

    public async Task<ProviderTestResult> TestAsync(ModelConfiguration config, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("chat");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.TryAddWithoutValidation("x-api-key", config.ApiKeyEncrypted ?? "");
            client.DefaultRequestHeaders.TryAddWithoutValidation("anthropic-version", "2023-06-01");
            var payload = JsonSerializer.Serialize(new
            {
                model = config.ModelName,
                max_tokens = 1,
                messages = new[] { new { role = "user", content = "ping" } }
            });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            using var resp = await client.PostAsync(config.BaseUrl.TrimEnd('/') + "/v1/messages", content, ct);
            return new ProviderTestResult { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "Claude reachable." : $"HTTP {(int)resp.StatusCode}" };
        }
        catch (Exception ex) { return new ProviderTestResult { Success = false, Message = ex.Message }; }
    }
}
