using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class KnowledgeEntity
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Name { get; set; } = "";
    public EntityType EntityType { get; set; } = EntityType.Other;
    public string? Description { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.NeedsReview;
    public int? SourceKnowledgeItemId { get; set; }
    public PermanenceTier Tier { get; set; } = PermanenceTier.Derived;
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // KE-003 portable identity.
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}

public class KnowledgeRelationship
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int FromEntityId { get; set; }
    public KnowledgeEntity? FromEntity { get; set; }
    public int ToEntityId { get; set; }
    public KnowledgeEntity? ToEntity { get; set; }
    public RelationType RelationType { get; set; } = RelationType.Uses;
    public string? Description { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.NeedsReview;
    public double Confidence { get; set; } = 0.5;
    public int? SourceKnowledgeItemId { get; set; }
    public PermanenceTier Tier { get; set; } = PermanenceTier.Derived;
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // KE-003 portable identity.
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
