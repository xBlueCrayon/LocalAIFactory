using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public string? Description { get; set; }
    public bool IsGlobal { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ProjectSource> Sources { get; set; } = new List<ProjectSource>();
}

public class ProjectSource
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public string Path { get; set; } = "";
    public ProjectSourceKind Kind { get; set; } = ProjectSourceKind.LocalFolder;
    public string? Description { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
