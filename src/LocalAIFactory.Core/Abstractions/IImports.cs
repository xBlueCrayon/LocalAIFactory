using LocalAIFactory.Core.Entities;

namespace LocalAIFactory.Core.Abstractions;

public interface IFileImportService
{
    Task<ImportedFile> ImportAsync(int? projectId, string fileName, byte[] content, CancellationToken ct = default);
}

public interface IChatGptImportService
{
    Task<IReadOnlyList<ImportedConversation>> ImportAsync(int? projectId, string fileName, byte[] content, CancellationToken ct = default);
}
