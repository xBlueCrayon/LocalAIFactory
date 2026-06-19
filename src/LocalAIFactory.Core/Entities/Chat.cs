using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Entities;

public class ChatSession
{
    public int Id { get; set; }
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? ModelConfigurationId { get; set; }
    public ModelConfiguration? ModelConfiguration { get; set; }
    public string Title { get; set; } = "New chat";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;

    public ICollection<ChatMessage> Messages { get; set; } = new List<ChatMessage>();
}

public class ChatMessage
{
    public int Id { get; set; }
    public int ChatSessionId { get; set; }
    public ChatSession? ChatSession { get; set; }
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";
    public int TokenCount { get; set; }
    public string? RetrievedContextJson { get; set; }
    public int? ModelConfigurationId { get; set; }
    public int? ModelOutputId { get; set; }
    public MessageRating Rating { get; set; } = MessageRating.None;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
