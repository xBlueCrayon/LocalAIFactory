using LocalAIFactory.Core.Dtos;

namespace LocalAIFactory.Core.Abstractions;

public interface IChatOrchestrator
{
    Task<ChatTurnResult> SendAsync(ChatTurnRequest request, CancellationToken ct = default);
}

public interface IAuditService
{
    Task LogAsync(string action, string? entityName = null, string? entityId = null, string? details = null, CancellationToken ct = default);
}
