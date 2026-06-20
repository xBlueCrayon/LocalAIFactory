using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class KnowledgeItem : IPortableEntity
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public SourceType SourceType { get; set; } = SourceType.UserExplanation;
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.Draft;
    public double Confidence { get; set; } = 0.5;
    public bool IsApproved { get; set; }
    public bool IsDeprecated { get; set; }
    public PermanenceTier Tier { get; set; } = PermanenceTier.Derived;

    // KE-003 backbone: portable identity, content fingerprint, current version pointer, short summary.
    public Guid Uid { get; set; } = Guid.CreateVersion7();
    public string ContentHash { get; set; } = "";
    // KE-004: stable per-file extraction locus so re-extraction converges on the same logical item.
    // Null = no stable locus (e.g. human-authored), so it is never auto-converged. Designed to extend
    // to per-symbol granularity in M2 (KE-008) by widening the canonical locus string.
    public string? SourceLocusKey { get; set; }
    public int VersionNumber { get; set; } = 1;
    public string? Summary { get; set; }

    // KE-003 inert backbone metadata. Persisted and portable, but no behavior reads these yet
    // (scope/precedence is KE-005; quality computation is KE-006).
    public KnowledgeType KnowledgeType { get; set; } = KnowledgeType.Unspecified;
    public KnowledgeScope Scope { get; set; } = KnowledgeScope.Unspecified;
    public int? KnowledgeDomainId { get; set; } // KE-005: optional domain taxonomy link.
    public AuthorityLevel Authority { get; set; } = AuthorityLevel.Normal;
    public QualityBand QualityBand { get; set; } = QualityBand.Provisional;
    public DateTime? EffectiveUtc { get; set; }
    public DateTime? ExpiryUtc { get; set; }

    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<KnowledgeChunk> Chunks { get; set; } = new List<KnowledgeChunk>();
    public ICollection<KnowledgeItemTag> Tags { get; set; } = new List<KnowledgeItemTag>();
}

public class KnowledgeChunk
{
    public int Id { get; set; }
    public int KnowledgeItemId { get; set; }
    public KnowledgeItem? KnowledgeItem { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = "";
    public int TokenCount { get; set; }
    public string? VectorId { get; set; }
    public string? EmbeddingModel { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
}

public class KnowledgeItemTag
{
    public int KnowledgeItemId { get; set; }
    public KnowledgeItem? KnowledgeItem { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
