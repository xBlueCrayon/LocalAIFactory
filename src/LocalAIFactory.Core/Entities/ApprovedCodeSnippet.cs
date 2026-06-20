using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class ApprovedCodeSnippet
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Title { get; set; } = "";
    public string Language { get; set; } = "csharp";
    public string? Framework { get; set; }
    public string Content { get; set; } = "";
    public string? Explanation { get; set; }
    public string? SourceReference { get; set; }
    public bool IsReusable { get; set; } = true;
    public DateTime ApprovedUtc { get; set; } = DateTime.UtcNow;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public PermanenceTier Tier { get; set; } = PermanenceTier.Curated;

    public ICollection<ApprovedCodeSnippetTag> Tags { get; set; } = new List<ApprovedCodeSnippetTag>();
}

public class ApprovedCodeSnippetTag
{
    public int ApprovedCodeSnippetId { get; set; }
    public ApprovedCodeSnippet? ApprovedCodeSnippet { get; set; }
    public int TagId { get; set; }
    public Tag? Tag { get; set; }
}
