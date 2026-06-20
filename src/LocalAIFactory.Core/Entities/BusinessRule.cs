using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class BusinessRule
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public BusinessRuleStatus Status { get; set; } = BusinessRuleStatus.Draft;
    public bool IsApproved { get; set; }
    public DateTime? ApprovedUtc { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public PermanenceTier Tier { get; set; } = PermanenceTier.Derived;

    public ICollection<BusinessRuleTag> Tags { get; set; } = new List<BusinessRuleTag>();
}

public class BusinessRuleTag
{
    public int BusinessRuleId { get; set; }
    public BusinessRule? BusinessRule { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
