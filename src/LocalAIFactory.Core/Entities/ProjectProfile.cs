using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class ProjectProfile : IPortableEntity
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.NeedsReview;
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // KE-003 portable identity.
    public string? Summary { get; set; }
    public string? StructuredJson { get; set; }
    public int? GeneratedByModelConfigurationId { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ProjectProfileSection> Sections { get; set; } = new List<ProjectProfileSection>();
}

public class ProjectProfileSection : IPortableEntity
{
    public int Id { get; set; }
    public int ProjectProfileId { get; set; }
    public ProjectProfile? ProjectProfile { get; set; }
    public string SectionKey { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Content { get; set; }
    public KnowledgeStatus Status { get; set; } = KnowledgeStatus.NeedsReview;
    public int OrderIndex { get; set; }
    public PermanenceTier Tier { get; set; } = PermanenceTier.Derived;
    public Guid Uid { get; set; } = Guid.CreateVersion7(); // KE-003 portable identity.
}
