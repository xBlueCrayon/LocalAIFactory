using System.Linq;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Ingestion.Imports;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-20X: chat-import learning. The extractor turns an exported conversation into PROPOSALS only
// (never auto-approved). These tests pin the deterministic segmentation + classification behaviour.
public class ChatLearningTests
{
    private static readonly IChatKnowledgeExtractor X = new ChatKnowledgeExtractor();

    [Fact]
    public void Segments_chatgpt_style_into_user_and_assistant_turns()
    {
        var chat = """
            ## User
            We decided to use separate CountAsync calls instead of GroupBy.
            Never use GroupBy(_ => 1) on SQL Server, it hangs the page.

            ## ChatGPT
            The root cause was client-side materialization of large text columns.
            The fix is to project list queries to lightweight record rows.
            """;
        var r = X.Extract(chat, ChatExportFormat.ChatGptMarkdown);

        Assert.Equal(2, r.MessageCount);
        Assert.Equal("user", r.Messages[0].Role);
        Assert.Equal("assistant", r.Messages[1].Role);
    }

    [Fact]
    public void Extracts_decision_donotrepeat_and_fix_kinds()
    {
        var chat = """
            ## User
            We decided to use separate CountAsync calls instead of GroupBy.
            Never use GroupBy(_ => 1) on SQL Server, it hangs the page.

            ## Assistant
            The root cause was client-side materialization of large text columns.
            """;
        var r = X.Extract(chat, ChatExportFormat.ChatGptMarkdown);
        var kinds = r.Proposals.Select(p => p.Kind).ToHashSet();

        Assert.Contains(ChatKnowledgeKind.Decision, kinds);
        Assert.Contains(ChatKnowledgeKind.DoNotRepeat, kinds);
        Assert.Contains(ChatKnowledgeKind.FixPattern, kinds);
        Assert.All(r.Proposals, p => Assert.InRange(p.Confidence, 0.0, 0.95));
    }

    [Fact]
    public void Extracts_fenced_code_snippet_with_language()
    {
        var chat = """
            ## User
            Here is the helper I want to reuse:

            ```csharp
            var count = await db.Items.CountAsync(ct);
            ```
            """;
        var r = X.Extract(chat, ChatExportFormat.Markdown);
        var code = Assert.Single(r.Proposals, p => p.Kind == ChatKnowledgeKind.CodeSnippet);

        Assert.Equal("csharp", code.Language);
        Assert.Contains("CountAsync", code.Content);
    }

    [Fact]
    public void Extracts_explicit_prompt_label()
    {
        var r = X.Extract("prompt: Summarize the repository structure in five bullet points.",
            ChatExportFormat.PlainText);
        var prompt = Assert.Single(r.Proposals, p => p.Kind == ChatKnowledgeKind.Prompt);

        Assert.Equal("Summarize the repository structure in five bullet points.", prompt.Content);
    }

    [Fact]
    public void Handles_claude_bold_role_markers()
    {
        var chat = """
            **Human:** Never store secrets in appsettings.json.

            **Assistant:** Understood — the fix is to use environment variables and Data Protection.
            """;
        var r = X.Extract(chat, ChatExportFormat.ClaudeMarkdown);

        Assert.Equal(2, r.MessageCount);
        Assert.Equal("user", r.Messages[0].Role);
        Assert.Equal("assistant", r.Messages[1].Role);
        Assert.Contains(r.Proposals, p => p.Kind == ChatKnowledgeKind.DoNotRepeat);
    }

    [Fact]
    public void Suppresses_duplicate_lessons_within_a_run()
    {
        var chat = """
            ## User
            Never use GroupBy(_ => 1) on SQL Server.
            Never use GroupBy(_ => 1) on SQL Server.
            """;
        var r = X.Extract(chat, ChatExportFormat.Markdown);

        Assert.Equal(1, r.Proposals.Count(p => p.Kind == ChatKnowledgeKind.DoNotRepeat));
        Assert.True(r.DuplicatesSuppressed >= 1);
    }

    [Fact]
    public void Plain_text_without_headers_is_one_unknown_segment()
    {
        var r = X.Extract("Always run the build before claiming done.", ChatExportFormat.PlainText);

        Assert.Equal(1, r.MessageCount);
        Assert.Equal("unknown", r.Messages[0].Role);
        Assert.Contains(r.Proposals, p => p.Kind == ChatKnowledgeKind.ReusableRule);
    }

    [Fact]
    public void Routes_project_hint_onto_every_proposal()
    {
        var r = X.Extract("We decided to migrate BDM mandates to the new schema.",
            ChatExportFormat.PlainText, projectHint: "BDM");

        Assert.NotEmpty(r.Proposals);
        Assert.All(r.Proposals, p => Assert.Equal("BDM", p.ProjectHint));
    }

    [Fact]
    public void Extraction_is_deterministic()
    {
        const string chat = """
            ## User
            We decided to cache the health snapshot.
            Never block the request path on Qdrant.
            ```sql
            SELECT TOP 1 * FROM dbo.HealthSnapshots ORDER BY Id DESC;
            ```
            """;
        var a = X.Extract(chat, ChatExportFormat.ChatGptMarkdown);
        var b = X.Extract(chat, ChatExportFormat.ChatGptMarkdown);

        Assert.Equal(a.Proposals.Count, b.Proposals.Count);
        Assert.Equal(
            a.Proposals.Select(p => p.NormalizedKey),
            b.Proposals.Select(p => p.NormalizedKey));
    }
}
