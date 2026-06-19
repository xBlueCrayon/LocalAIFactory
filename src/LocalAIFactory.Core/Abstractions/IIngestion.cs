using LocalAIFactory.Core.Dtos;

namespace LocalAIFactory.Core.Abstractions;

public interface IZipExtractionService
{
    // Safe extraction (zip-slip protection, size/count caps). Returns the extracted root path.
    Task<string> ExtractAsync(string archiveFileName, byte[] zipBytes, string destinationRoot, CancellationToken ct = default);
}

public interface IFileClassifier
{
    LocalAIFactory.Core.Enums.FileClass Classify(string relativePath);
    bool IsTextual(LocalAIFactory.Core.Enums.FileClass fileClass, string extension);
    LocalAIFactory.Core.Enums.SourceType ToSourceType(LocalAIFactory.Core.Enums.FileClass fileClass);
}

public interface IIngestionQueue
{
    ValueTask EnqueueAsync(int ingestionJobId, CancellationToken ct = default);
    IAsyncEnumerable<int> DequeueAllAsync(CancellationToken ct);
}

public interface IProjectIngestionService
{
    Task<int> CreateZipJobAsync(int? projectId, string fileName, byte[] zipBytes, CancellationToken ct = default);
    Task ProcessJobAsync(int ingestionJobId, CancellationToken ct = default);
    Task<IngestionProgress?> GetProgressAsync(int ingestionJobId, CancellationToken ct = default);
}

public interface IProjectProfileService
{
    Task GenerateAsync(int projectId, int? ingestionJobId, CancellationToken ct = default);
}

public interface IKnowledgeGraphService
{
    Task ExtractAsync(int? projectId, int knowledgeItemId, string title, string content, CancellationToken ct = default);
    Task<List<RagContextItem>> NeighborsAsync(int? projectId, string query, int max, CancellationToken ct = default);
}
