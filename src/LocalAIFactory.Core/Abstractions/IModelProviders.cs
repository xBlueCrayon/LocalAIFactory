using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

public interface IChatModelProvider
{
    ModelProvider Provider { get; }
    Task<ChatCompletionResult> CompleteAsync(ModelConfiguration config, ChatCompletionRequest request, CancellationToken ct = default);
    Task<ProviderTestResult> TestAsync(ModelConfiguration config, CancellationToken ct = default);
}

public interface IEmbeddingProvider
{
    ModelProvider Provider { get; }
    Task<EmbeddingResult> EmbedAsync(string baseUrl, string? apiKey, string model, string text, CancellationToken ct = default);
}

public interface IEmbeddingService
{
    bool IsConfigured { get; }
    int VectorSize { get; }
    Task<EmbeddingResult> EmbedAsync(string text, CancellationToken ct = default);
}

public interface IApiKeyProtector
{
    string Protect(string? plaintext);
    string Unprotect(string? ciphertext);
}

public interface IModelRouter
{
    IChatModelProvider Resolve(ModelProvider provider);
    Task<ModelConfiguration?> GetActiveAsync(int? modelConfigurationId, CancellationToken ct = default);
}
