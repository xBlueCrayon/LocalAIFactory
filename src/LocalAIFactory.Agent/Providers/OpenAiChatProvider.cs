using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Agent.Providers;

public sealed class OpenAiChatProvider : IChatModelProvider
{
    private readonly IHttpClientFactory _http;
    public ModelProvider Provider => ModelProvider.OpenAi;
    public OpenAiChatProvider(IHttpClientFactory http) => _http = http;

    public Task<ChatCompletionResult> CompleteAsync(ModelConfiguration config, ChatCompletionRequest request, CancellationToken ct = default)
        => OpenAiStyle.CompleteAsync(_http, config, request, ct);

    public Task<ProviderTestResult> TestAsync(ModelConfiguration config, CancellationToken ct = default)
        => OpenAiStyle.TestAsync(_http, config, ct);
}
