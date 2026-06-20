using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

// KE-007: the ImportBatch — one ingestion episode and the provenance root for everything it produced.
public class IngestionJob : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // KE-007 portable identity.
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string FileName { get; set; } = "";
    public ArtifactSourceSystem SourceSystem { get; set; } = ArtifactSourceSystem.Upload; // KE-007
    public string? SourceReference { get; set; } // KE-007: repo URL / zip name / folder path.
    public string? SourceRevision { get; set; }  // KE-007: git commit / branch / version tag (reproducibility).
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
