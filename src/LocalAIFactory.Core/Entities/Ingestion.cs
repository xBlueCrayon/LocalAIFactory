using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class IngestionJob
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string FileName { get; set; } = "";
    public string? ExtractedRoot { get; set; }
    public IngestionJobStatus Status { get; set; } = IngestionJobStatus.Pending;
    public IngestionPhase Phase { get; set; } = IngestionPhase.Pending;
    public int TotalFiles { get; set; }
    public int ProcessedFiles { get; set; }
    public int SkippedFiles { get; set; }
    public int ChunkCount { get; set; }
    public int EmbeddedCount { get; set; }
    public string? Error { get; set; }
    public DateTime? StartedUtc { get; set; }
    public DateTime? CompletedUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
