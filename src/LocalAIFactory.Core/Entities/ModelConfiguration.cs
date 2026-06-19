using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class ModelConfiguration
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ModelProvider Provider { get; set; } = ModelProvider.Ollama;
    public string ModelName { get; set; } = "";
    public string BaseUrl { get; set; } = "";
    public string? ApiKeyEncrypted { get; set; }
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public int ContextWindowHint { get; set; } = 8192;
    public string? EmbeddingModel { get; set; }
    public bool IsEnabled { get; set; } = true;
    public bool IsDefault { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

public class PromptTemplate
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Kind { get; set; } = "ChatSystem";
    public string Content { get; set; } = "";
    public bool IsDefault { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}
