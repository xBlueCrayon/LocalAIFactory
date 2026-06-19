namespace LocalAIFactory.Core.Entities;

public class SystemSetting
{
    public int Id { get; set; }
    public string Key { get; set; } = "";
    public string? Value { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public class AuditLog
{
    public int Id { get; set; }
    public string Action { get; set; } = "";
    public string? EntityName { get; set; }
    public string? EntityId { get; set; }
    public string? Details { get; set; }
    public string? UserName { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
