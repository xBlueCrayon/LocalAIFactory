using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Agent.Chat;

public sealed class ChatOrchestrator : IChatOrchestrator
{
    private readonly AppDbContext _db;
    private readonly IModelExecutionService _execution;
    private readonly RagOptions _rag;

    public ChatOrchestrator(AppDbContext db, IModelExecutionService execution, IOptions<RagOptions> rag)
    {
        _db = db; _execution = execution; _rag = rag.Value;
    }

    public async Task<ChatTurnResult> SendAsync(ChatTurnRequest request, CancellationToken ct = default)
    {
        ChatSession session;
        if (request.SessionId is int sid)
        {
            session = await _db.ChatSessions.FirstOrDefaultAsync(s => s.Id == sid, ct)
                      ?? throw new InvalidOperationException("Chat session not found.");
            if (request.ModelConfigurationId is int mc) session.ModelConfigurationId = mc;
        }
        else
        {
            session = new ChatSession
            {
                ProjectId = request.ProjectId,
                ModelConfigurationId = request.ModelConfigurationId,
                Title = BuildTitle(request.Message)
            };
            _db.ChatSessions.Add(session);
            await _db.SaveChangesAsync(ct);
        }

        var history = await _db.ChatMessages
            .Where(m => m.ChatSessionId == session.Id)
            .OrderByDescending(m => m.Id)
            .Take(_rag.MaxHistoryMessages)
            .OrderBy(m => m.Id)
            .Select(m => new ChatMessageDto(m.Role, m.Content))
            .ToListAsync(ct);

        var userMsg = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = ChatRole.User,
            Content = request.Message,
            CreatedUtc = DateTime.UtcNow
        };
        _db.ChatMessages.Add(userMsg);
        session.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        var exec = await _execution.ExecuteAsync(new ModelExecutionRequest
        {
            TaskType = TaskType.Chat,
            ProjectId = session.ProjectId,
            ChatSessionId = session.Id,
            OverrideModelId = session.ModelConfigurationId,
            UserPrompt = request.Message,
            History = history,
            AllowCloud = session.ModelConfigurationId.HasValue   // an explicit model choice is treated as consent
        }, ct);

        var assistantText = exec.Success
            ? exec.PrimaryOutput
            : ("I could not complete that: " + (exec.Error ?? "unknown error"));

        var assistantMsg = new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = ChatRole.Assistant,
            Content = assistantText,
            ModelConfigurationId = session.ModelConfigurationId,
            ModelOutputId = exec.PrimaryOutputId,
            RetrievedContextJson = JsonSerializer.Serialize(exec.Context.Items),
            CreatedUtc = DateTime.UtcNow
        };
        _db.ChatMessages.Add(assistantMsg);
        session.UpdatedUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return new ChatTurnResult
        {
            Success = exec.Success,
            Error = exec.Error,
            SessionId = session.Id,
            UserMessageId = userMsg.Id,
            AssistantMessageId = assistantMsg.Id,
            Assistant = assistantText,
            Context = exec.Context
        };
    }

    private static string BuildTitle(string message)
    {
        var t = (message ?? "").Trim().Replace('\n', ' ');
        if (t.Length > 60) t = t.Substring(0, 60) + "…";
        return string.IsNullOrWhiteSpace(t) ? "New chat" : t;
    }
}
