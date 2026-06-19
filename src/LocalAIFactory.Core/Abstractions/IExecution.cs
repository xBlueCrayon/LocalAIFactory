using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// Resolves a TaskType to a concrete execution plan, with a fallback chain that guarantees
// single-model operation (primary -> system default -> any enabled model).
public interface ITaskProfileResolver
{
    Task<ResolvedTaskProfile> ResolveAsync(TaskType taskType, CancellationToken ct = default);
}

// The single execution path used by both chat and ingestion phases.
public interface IModelExecutionService
{
    Task<ModelExecutionResult> ExecuteAsync(ModelExecutionRequest request, CancellationToken ct = default);
    Task<string> CompleteSimpleAsync(TaskType taskType, string systemPrompt, string userPrompt, CancellationToken ct = default);
}
