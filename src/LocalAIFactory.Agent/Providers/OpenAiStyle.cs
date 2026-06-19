using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;

namespace LocalAIFactory.Agent.Providers;

// Shared implementation for OpenAI and OpenAI-compatible (LM Studio, vLLM, etc.) chat endpoints.
internal static class OpenAiStyle
{
    public static async Task<ChatCompletionResult> CompleteAsync(
        IHttpClientFactory httpFactory, ModelConfiguration config, ChatCompletionRequest request, CancellationToken ct)
    {
        try
        {
            var client = httpFactory.CreateClient("chat");
            client.Timeout = TimeSpan.FromMinutes(5);
            if (!string.IsNullOrEmpty(config.ApiKeyEncrypted))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKeyEncrypted);

            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
                messages.Add(new { role = "system", content = request.SystemPrompt });
            foreach (var m in request.Messages)
                messages.Add(new { role = RoleName(m.Role), content = m.Content });

            var payload = JsonSerializer.Serialize(new
            {
                model = request.Model,
                temperature = request.Temperature,
                max_tokens = request.MaxTokens,
                messages
            });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = config.BaseUrl.TrimEnd('/') + "/chat/completions";
            using var resp = await client.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) return ChatCompletionResult.Fail($"HTTP {(int)resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            var result = ChatCompletionResult.Ok(text);
            if (doc.RootElement.TryGetProperty("usage", out var usage))
            {
                if (usage.TryGetProperty("prompt_tokens", out var pt) && pt.ValueKind == JsonValueKind.Number) result.PromptTokens = pt.GetInt32();
                if (usage.TryGetProperty("completion_tokens", out var c2) && c2.ValueKind == JsonValueKind.Number) result.CompletionTokens = c2.GetInt32();
            }
            return result;
        }
        catch (Exception ex) { return ChatCompletionResult.Fail(ex.Message); }
    }

    public static async Task<ProviderTestResult> TestAsync(IHttpClientFactory httpFactory, ModelConfiguration config, CancellationToken ct)
    {
        try
        {
            var client = httpFactory.CreateClient("chat");
            client.Timeout = TimeSpan.FromSeconds(20);
            if (!string.IsNullOrEmpty(config.ApiKeyEncrypted))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", config.ApiKeyEncrypted);
            using var resp = await client.GetAsync(config.BaseUrl.TrimEnd('/') + "/models", ct);
            return new ProviderTestResult { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "Reachable." : $"HTTP {(int)resp.StatusCode}" };
        }
        catch (Exception ex) { return new ProviderTestResult { Success = false, Message = ex.Message }; }
    }

    private static string RoleName(LocalAIFactory.Core.Enums.ChatRole role) => role switch
    {
        LocalAIFactory.Core.Enums.ChatRole.System => "system",
        LocalAIFactory.Core.Enums.ChatRole.Assistant => "assistant",
        _ => "user"
    };
}
