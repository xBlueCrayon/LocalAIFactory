using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Agent.Providers;

// For local OpenAI-compatible servers (LM Studio, vLLM, text-generation-webui, etc.).
public sealed class OpenAiCompatibleChatProvider : IChatModelProvider
{
    private readonly IHttpClientFactory _http;
    public ModelProvider Provider => ModelProvider.OpenAiCompatible;
    public OpenAiCompatibleChatProvider(IHttpClientFactory http) => _http = http;

    public Task<ChatCompletionResult> CompleteAsync(ModelConfiguration config, ChatCompletionRequest request, CancellationToken ct = default)
        => OpenAiStyle.CompleteAsync(_http, config, request, ct);

    public Task<ProviderTestResult> TestAsync(ModelConfiguration config, CancellationToken ct = default)
        => OpenAiStyle.TestAsync(_http, config, ct);
}
