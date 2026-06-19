using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Dtos;

public sealed class ChatMessageDto
{
    public ChatRole Role { get; set; }
    public string Content { get; set; } = "";

    public ChatMessageDto() { }
    public ChatMessageDto(ChatRole role, string content) { Role = role; Content = content; }
}

public sealed class ChatCompletionRequest
{
    public string Model { get; set; } = "";
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 2048;
    public string? SystemPrompt { get; set; }
    public List<ChatMessageDto> Messages { get; set; } = new();
}

public sealed class ChatCompletionResult
{
    public bool Success { get; set; }
    public string Content { get; set; } = "";
    public string? Error { get; set; }
    public int? PromptTokens { get; set; }
    public int? CompletionTokens { get; set; }

    public static ChatCompletionResult Ok(string content) => new() { Success = true, Content = content };
    public static ChatCompletionResult Fail(string error) => new() { Success = false, Error = error };
}

public sealed class EmbeddingResult
{
    public bool Success { get; set; }
    public float[]? Vector { get; set; }
    public string? Error { get; set; }

    public static EmbeddingResult Ok(float[] v) => new() { Success = true, Vector = v };
    public static EmbeddingResult Fail(string e) => new() { Success = false, Error = e };
}

public sealed class ProviderTestResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = "";
}

public sealed class RagContextItem
{
    public string Kind { get; set; } = "";          // BusinessRule | ApprovedCode | Knowledge
    public string Source { get; set; } = "";
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public bool IsApproved { get; set; }
    public double Score { get; set; }
}

public sealed class RetrievedContext
{
    public List<RagContextItem> Items { get; set; } = new();

    public string AsPromptText()
    {
        if (Items.Count == 0) return "(no project knowledge retrieved)";
        var sb = new System.Text.StringBuilder();
        foreach (var i in Items)
        {
            var flag = i.IsApproved ? "APPROVED" : "DRAFT";
            sb.AppendLine($"### [{i.Kind}] [{flag}] {i.Title}");
            sb.AppendLine(i.Content.Trim());
            sb.AppendLine();
        }
        return sb.ToString();
    }
}

public sealed class VectorSearchHit
{
    public string Id { get; set; } = "";
    public double Score { get; set; }
    public Dictionary<string, object> Payload { get; set; } = new();
}

public sealed class ChatTurnRequest
{
    public int? SessionId { get; set; }
    public int? ProjectId { get; set; }
    public int? ModelConfigurationId { get; set; }
    public string Message { get; set; } = "";
}

public sealed class ChatTurnResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public int SessionId { get; set; }
    public int UserMessageId { get; set; }
    public int AssistantMessageId { get; set; }
    public string Assistant { get; set; } = "";
    public RetrievedContext Context { get; set; } = new();
}
