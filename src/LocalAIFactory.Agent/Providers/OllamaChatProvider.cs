using System.Text;
using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Agent.Providers;

public sealed class OllamaChatProvider : IChatModelProvider
{
    private readonly IHttpClientFactory _http;
    public ModelProvider Provider => ModelProvider.Ollama;
    public OllamaChatProvider(IHttpClientFactory http) => _http = http;

    public async Task<ChatCompletionResult> CompleteAsync(ModelConfiguration config, ChatCompletionRequest request, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("chat");
            client.Timeout = TimeSpan.FromMinutes(10);

            var messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(request.SystemPrompt))
                messages.Add(new { role = "system", content = request.SystemPrompt });
            foreach (var m in request.Messages)
                messages.Add(new { role = RoleName(m.Role), content = m.Content });

            var payload = JsonSerializer.Serialize(new
            {
                model = request.Model,
                messages,
                stream = false,
                options = new { temperature = request.Temperature, num_predict = request.MaxTokens }
            });
            using var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var url = config.BaseUrl.TrimEnd('/') + "/api/chat";
            using var resp = await client.PostAsync(url, content, ct);
            var body = await resp.Content.ReadAsStringAsync(ct);
            if (!resp.IsSuccessStatusCode) return ChatCompletionResult.Fail($"Ollama HTTP {(int)resp.StatusCode}: {body}");

            using var doc = JsonDocument.Parse(body);
            var text = doc.RootElement.TryGetProperty("message", out var msg) && msg.TryGetProperty("content", out var c)
                ? c.GetString() ?? "" : "";
            return ChatCompletionResult.Ok(text);
        }
        catch (Exception ex) { return ChatCompletionResult.Fail("Ollama error: " + ex.Message); }
    }

    public async Task<ProviderTestResult> TestAsync(ModelConfiguration config, CancellationToken ct = default)
    {
        try
        {
            var client = _http.CreateClient("chat");
            client.Timeout = TimeSpan.FromSeconds(15);
            using var resp = await client.GetAsync(config.BaseUrl.TrimEnd('/') + "/api/tags", ct);
            return new ProviderTestResult { Success = resp.IsSuccessStatusCode, Message = resp.IsSuccessStatusCode ? "Ollama reachable." : $"HTTP {(int)resp.StatusCode}" };
        }
        catch (Exception ex) { return new ProviderTestResult { Success = false, Message = "Ollama not reachable: " + ex.Message }; }
    }

    private static string RoleName(ChatRole role) => role switch
    {
        ChatRole.System => "system",
        ChatRole.Assistant => "assistant",
        _ => "user"
    };
}
