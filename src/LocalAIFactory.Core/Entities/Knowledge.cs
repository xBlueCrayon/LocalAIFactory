using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class KnowledgeItem
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
