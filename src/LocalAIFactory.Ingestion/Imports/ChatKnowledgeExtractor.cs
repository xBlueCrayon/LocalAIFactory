using System.Text;
using System.Text.RegularExpressions;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Ingestion.Imports;

// R2-ACC-20X: deterministic chat-export → knowledge-proposal extractor. Pure text transform (no network, no
// model, no DB). It segments a conversation into speaker turns, then mines each turn for reusable knowledge:
//   - fenced code blocks                -> CodeSnippet
//   - "never / don't / must not"        -> DoNotRepeat
//   - "root cause / fixed by"           -> FixPattern
//   - "we decided / we'll use"          -> Decision
//   - "always / convention / rule"      -> ReusableRule
//   - "prompt: …"                       -> Prompt
//   - assistant explanations            -> Insight (most conservative)
// Output is PROPOSALS only — never auto-approved, never written over curated truth. Same input -> same output.
public sealed class ChatKnowledgeExtractor : IChatKnowledgeExtractor
{
    private const int MinProse = 15;     // ignore trivial fragments
    private const int TitleMax = 80;

    // Role headers (tested against a "probe": the line with leading #'s and **bold** markers stripped, so
    // "## User", "**Human:**", "ChatGPT said:" and "Assistant:" all reduce to a clean role token).
    private static readonly Regex StripHeading = new(@"^#{1,6}\s*", RegexOptions.Compiled);
    private static readonly Regex UserHeader = new(
        @"^(user|you|me|human)\s*(?:said)?\s*:?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex AsstHeader = new(
        @"^(assistant|chatgpt|claude|ai|gpt)\s*(?:said)?\s*:?\s*$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    // Inline marker: "Human: text…" / "Assistant: text…" on one line (content after the colon is required).
    private static readonly Regex InlineRole = new(
        @"^(user|you|me|human|assistant|chatgpt|claude|ai|gpt)\s*(?:said)?\s*:\s*(.+)$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex CodeFence = new(
        @"```([A-Za-z0-9_+\-]*)\r?\n(.*?)```",
        RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex RxDoNotRepeat = new(@"\b(never|don'?t|do not|must not|avoid|stop doing|should not|shouldn'?t)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxFix = new(@"\b(root cause|the bug was|the issue was|fixed by|the fix\b|to fix\b|fix:|resolved by|work[- ]?around)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxDecision = new(@"\b(we decided|decision:|we'?ll use|we will use|let'?s use|let'?s go with|we chose|we chose to|going with|we should use|chosen)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxRule = new(@"\b(always|rule:|convention|best practice|make sure to|ensure that|prefer )\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxPrompt = new(@"^\s*prompt\s*:\s*(.+)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex RxInsightCue = new(@"\b(because|this means|in other words|the reason|note that|important:)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex WhitespaceRun = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex NonAlnum = new(@"[^a-z0-9 ]", RegexOptions.Compiled);

    public ChatExtractionResult Extract(string chatText, ChatExportFormat format, string? projectHint = null)
    {
        chatText ??= "";
        var hint = string.IsNullOrWhiteSpace(projectHint) ? null : projectHint.Trim();
        var messages = Segment(chatText);

        var proposals = new List<ChatKnowledgeProposal>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        int suppressed = 0;

        void TryAdd(ChatKnowledgeProposal p)
        {
            if (seen.Add(p.NormalizedKey)) proposals.Add(p);
            else suppressed++;
        }

        foreach (var msg in messages)
        {
            // 1) Code blocks first; strip them so their lines aren't reclassified as prose.
            var prose = CodeFence.Replace(msg.Text, m =>
            {
                var lang = m.Groups[1].Value.Trim();
                var body = m.Groups[2].Value.Trim('\r', '\n');
                if (body.Length == 0) return " ";
                var norm = Normalize(body);
                if (norm.Length == 0) return " ";
                var firstLine = body.Split('\n')[0].Trim();
                var title = $"Code snippet{(lang.Length > 0 ? $" ({lang})" : "")}: {Clip(firstLine, 50)}".Trim();
                TryAdd(new ChatKnowledgeProposal(
                    ChatKnowledgeKind.CodeSnippet, Clip(title, TitleMax), body,
                    lang.Length > 0 ? lang : null,
                    Score(ChatKnowledgeKind.CodeSnippet, msg.Role), msg.Index, msg.Role,
                    "CodeSnippet|" + norm, hint));
                return " ";
            });

            // 2) Prose lines/sentences, classified by first matching rule (priority order).
            foreach (var raw in SplitCandidates(prose))
            {
                var text = CleanLead(raw);
                if (text.Length < MinProse) continue;

                var promptM = RxPrompt.Match(text);
                ChatKnowledgeKind kind;
                string content = text;
                if (promptM.Success) { kind = ChatKnowledgeKind.Prompt; content = promptM.Groups[1].Value.Trim(); }
                else if (RxDoNotRepeat.IsMatch(text)) kind = ChatKnowledgeKind.DoNotRepeat;
                else if (RxFix.IsMatch(text)) kind = ChatKnowledgeKind.FixPattern;
                else if (RxDecision.IsMatch(text)) kind = ChatKnowledgeKind.Decision;
                else if (RxRule.IsMatch(text)) kind = ChatKnowledgeKind.ReusableRule;
                else if (msg.Role == "assistant" && text.Length >= 40 && RxInsightCue.IsMatch(text)) kind = ChatKnowledgeKind.Insight;
                else continue;

                var norm = Normalize(content);
                if (norm.Length == 0) continue;
                TryAdd(new ChatKnowledgeProposal(
                    kind, Clip(content, TitleMax), content, null,
                    Score(kind, msg.Role), msg.Index, msg.Role,
                    kind + "|" + norm, hint));
            }
        }

        return new ChatExtractionResult(format, messages.Count, messages, proposals, suppressed);
    }

    // ---- segmentation ----
    private static List<ChatMessageSegment> Segment(string text)
    {
        var lines = text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var segs = new List<ChatMessageSegment>();
        var buf = new StringBuilder();
        string role = "unknown";
        bool any = false;

        void Flush()
        {
            var t = buf.ToString().Trim();
            if (t.Length > 0) segs.Add(new ChatMessageSegment(segs.Count, role, t));
            buf.Clear();
        }

        foreach (var line in lines)
        {
            // Probe = line with leading heading #'s and **bold** markers removed (for header detection only).
            var probe = StripHeading.Replace(line.Trim(), "").Replace("**", "").Replace("__", "").Trim();
            if (UserHeader.IsMatch(probe)) { Flush(); role = "user"; any = true; continue; }
            if (AsstHeader.IsMatch(probe)) { Flush(); role = "assistant"; any = true; continue; }
            var inline = InlineRole.Match(probe);
            if (inline.Success)
            {
                Flush();
                var marker = inline.Groups[1].Value.ToLowerInvariant();
                role = (marker is "assistant" or "chatgpt" or "claude" or "ai" or "gpt") ? "assistant" : "user";
                any = true;
                buf.AppendLine(inline.Groups[2].Value);
                continue;
            }
            buf.AppendLine(line);
        }
        Flush();

        if (!any && segs.Count == 0)
            segs.Add(new ChatMessageSegment(0, "unknown", text.Trim()));
        return segs;
    }

    // Split a prose block into candidate strings: by line, and long lines further by sentence terminators.
    private static IEnumerable<string> SplitCandidates(string prose)
    {
        foreach (var line in prose.Replace("\r\n", "\n").Split('\n'))
        {
            var l = line.Trim();
            if (l.Length == 0) continue;
            if (l.Length <= 200) { yield return l; continue; }
            foreach (var s in Regex.Split(l, @"(?<=[.!?])\s+"))
                if (s.Trim().Length > 0) yield return s.Trim();
        }
    }

    private static double Score(ChatKnowledgeKind kind, string role)
    {
        double b = kind switch
        {
            ChatKnowledgeKind.DoNotRepeat => 0.80,
            ChatKnowledgeKind.FixPattern => 0.78,
            ChatKnowledgeKind.Decision => 0.75,
            ChatKnowledgeKind.ReusableRule => 0.72,
            ChatKnowledgeKind.CodeSnippet => 0.70,
            ChatKnowledgeKind.Prompt => 0.68,
            _ => 0.55
        };
        bool boost =
            ((kind is ChatKnowledgeKind.Decision or ChatKnowledgeKind.DoNotRepeat or ChatKnowledgeKind.Prompt) && role == "user") ||
            ((kind is ChatKnowledgeKind.FixPattern or ChatKnowledgeKind.Insight) && role == "assistant");
        return Math.Round(Math.Min(0.95, b + (boost ? 0.05 : 0.0)), 2);
    }

    private static string Normalize(string s) =>
        NonAlnum.Replace(WhitespaceRun.Replace(s.ToLowerInvariant(), " "), "").Trim();

    private static string CleanLead(string s) =>
        s.TrimStart('#', '-', '*', '>', ' ', '\t', '•', '·').Trim();

    private static string Clip(string s, int max)
    {
        s = WhitespaceRun.Replace(s, " ").Trim();
        return s.Length <= max ? s : s[..max].TrimEnd() + "…";
    }
}
