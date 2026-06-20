using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Ingestion.Imports;

// Imports ChatGPT and Claude conversation exports. Parses defensively (mapping tree, message arrays,
// or plain text), stores conversations + messages, extracts fenced code blocks as candidates, and
// indexes each transcript as a knowledge item. Everything is Draft / NeedsReview.
public sealed class ChatGptImportService : IChatGptImportService
{
    private static readonly Regex CodeFence = new("```(?<lang>[A-Za-z0-9_+-]*)\\n(?<code>.*?)```", RegexOptions.Singleline | RegexOptions.Compiled);

    private readonly AppDbContext _db;
    private readonly IChunkingService _chunking;
    private readonly IKnowledgeIndexer _indexer;
    private readonly IKnowledgeBackboneService _backbone;
    private readonly RagOptions _rag;

    public ChatGptImportService(AppDbContext db, IChunkingService chunking, IKnowledgeIndexer indexer, IKnowledgeBackboneService backbone, IOptions<RagOptions> rag)
    {
        _db = db; _chunking = chunking; _indexer = indexer; _backbone = backbone; _rag = rag.Value;
    }

    public async Task<IReadOnlyList<ImportedConversation>> ImportAsync(int? projectId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var raw = Encoding.UTF8.GetString(content);
        var source = fileName.Contains("claude", StringComparison.OrdinalIgnoreCase) ? ConversationSource.Claude : ConversationSource.ChatGpt;
        var results = new List<ImportedConversation>();

        List<ParsedConversation> parsed;
        try { parsed = Parse(raw, ref source); }
        catch { parsed = new List<ParsedConversation> { PlainText(fileName, raw) }; }

        if (parsed.Count == 0) parsed.Add(PlainText(fileName, raw));

        foreach (var pc in parsed)
        {
            ct.ThrowIfCancellationRequested();
            var convo = new ImportedConversation
            {
                ProjectId = projectId,
                Source = source,
                Title = string.IsNullOrWhiteSpace(pc.Title) ? "Imported conversation" : Trunc(pc.Title, 480),
                RawJson = null,
                MessageCount = pc.Messages.Count,
                Status = ImportStatus.NeedsReview
            };
            _db.ImportedConversations.Add(convo);
            await _db.SaveChangesAsync(ct);

            int order = 0;
            var transcript = new StringBuilder();
            foreach (var (role, text) in pc.Messages)
            {
                _db.ImportedConversationMessages.Add(new ImportedConversationMessage
                {
                    ImportedConversationId = convo.Id, Role = role, Content = text, OrderIndex = order++
                });
                transcript.AppendLine($"## {role}");
                transcript.AppendLine(text);
                transcript.AppendLine();

                if (role == ChatRole.Assistant)
                {
                    foreach (Match m in CodeFence.Matches(text))
                    {
                        var code = m.Groups["code"].Value.Trim();
                        if (code.Length < 12) continue;
                        _db.ExtractedCodeBlocks.Add(new ExtractedCodeBlock
                        {
                            ProjectId = projectId,
                            ImportedConversationId = convo.Id,
                            Language = string.IsNullOrWhiteSpace(m.Groups["lang"].Value) ? null : m.Groups["lang"].Value,
                            Content = code,
                            Status = KnowledgeStatus.NeedsReview
                        });
                    }
                }
            }
            await _db.SaveChangesAsync(ct);

            // Index the transcript as a knowledge item.
            var ki = new KnowledgeItem
            {
                ProjectId = projectId,
                Title = $"Transcript: {convo.Title}",
                Content = transcript.ToString(),
                SourceType = SourceType.ConversationTranscript,
                Status = KnowledgeStatus.NeedsReview,
                Confidence = 0.4,
                Tier = PermanenceTier.Derived // machine-extracted, regenerable.
            };
            _db.KnowledgeItems.Add(ki);
            await _db.SaveChangesAsync(ct);
            convo.KnowledgeItemId = ki.Id;
            await _db.SaveChangesAsync(ct);
            await _backbone.RecordInitialAsync(ki, ProvenanceMethod.Import, "system:conversation",
                $"Imported transcript: {convo.Title}", ct: ct);

            int idx = 0;
            foreach (var chunk in _chunking.Chunk(ki.Content, _rag.MaxChunkChars, _rag.ChunkOverlap))
            {
                _db.KnowledgeChunks.Add(new KnowledgeChunk
                {
                    KnowledgeItemId = ki.Id, ChunkIndex = idx++, Content = chunk, TokenCount = _chunking.EstimateTokens(chunk)
                });
            }
            await _db.SaveChangesAsync(ct);
            try { await _indexer.IndexKnowledgeItemAsync(ki.Id, ct); } catch { /* keyword fallback remains */ }

            results.Add(convo);
        }

        return results;
    }

    // ---- parsing ----

    private sealed class ParsedConversation
    {
        public string Title = "";
        public List<(ChatRole Role, string Text)> Messages = new();
    }

    private static ParsedConversation PlainText(string fileName, string raw)
        => new() { Title = Path.GetFileNameWithoutExtension(fileName), Messages = { (ChatRole.User, raw) } };

    private static List<ParsedConversation> Parse(string raw, ref ConversationSource source)
    {
        var list = new List<ParsedConversation>();
        using var doc = JsonDocument.Parse(raw);
        var root = doc.RootElement;

        if (root.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in root.EnumerateArray())
                AddConversation(el, list, ref source);
        }
        else if (root.ValueKind == JsonValueKind.Object)
        {
            if (root.TryGetProperty("conversations", out var convos) && convos.ValueKind == JsonValueKind.Array)
            {
                foreach (var el in convos.EnumerateArray())
                    AddConversation(el, list, ref source);
            }
            else
            {
                AddConversation(root, list, ref source);
            }
        }
        return list;
    }

    private static void AddConversation(JsonElement el, List<ParsedConversation> list, ref ConversationSource source)
    {
        if (el.ValueKind != JsonValueKind.Object) return;
        var pc = new ParsedConversation
        {
            Title = TryString(el, "title") ?? TryString(el, "name") ?? "Imported conversation"
        };

        // ChatGPT: mapping tree
        if (el.TryGetProperty("mapping", out var mapping) && mapping.ValueKind == JsonValueKind.Object)
        {
            var nodes = new List<(double order, ChatRole role, string text)>();
            double seq = 0;
            foreach (var node in mapping.EnumerateObject())
            {
                if (!node.Value.TryGetProperty("message", out var msg) || msg.ValueKind != JsonValueKind.Object) continue;
                var role = RoleFromAuthor(msg);
                var text = ContentText(msg);
                if (string.IsNullOrWhiteSpace(text)) continue;
                double t = msg.TryGetProperty("create_time", out var ctv) && ctv.ValueKind == JsonValueKind.Number ? ctv.GetDouble() : seq;
                nodes.Add((t, role, text));
                seq += 1;
            }
            foreach (var n in nodes.OrderBy(x => x.order))
                pc.Messages.Add((n.role, n.text));
            list.Add(pc);
            return;
        }

        // Claude / generic: message arrays
        foreach (var key in new[] { "chat_messages", "messages" })
        {
            if (el.TryGetProperty(key, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                if (key == "chat_messages") source = ConversationSource.Claude;
                foreach (var m in arr.EnumerateArray())
                {
                    var role = RoleFromGeneric(m);
                    var text = GenericText(m);
                    if (!string.IsNullOrWhiteSpace(text)) pc.Messages.Add((role, text));
                }
                list.Add(pc);
                return;
            }
        }

        // Fallback: stringify object
        pc.Messages.Add((ChatRole.User, el.ToString()));
        list.Add(pc);
    }

    private static ChatRole RoleFromAuthor(JsonElement msg)
    {
        if (msg.TryGetProperty("author", out var author) && author.ValueKind == JsonValueKind.Object
            && author.TryGetProperty("role", out var r))
            return MapRole(r.GetString());
        return ChatRole.User;
    }

    private static string ContentText(JsonElement msg)
    {
        if (!msg.TryGetProperty("content", out var content)) return "";
        if (content.ValueKind == JsonValueKind.String) return content.GetString() ?? "";
        if (content.ValueKind == JsonValueKind.Object && content.TryGetProperty("parts", out var parts) && parts.ValueKind == JsonValueKind.Array)
        {
            var sb = new StringBuilder();
            foreach (var p in parts.EnumerateArray())
                if (p.ValueKind == JsonValueKind.String) sb.AppendLine(p.GetString());
                else if (p.ValueKind == JsonValueKind.Object && p.TryGetProperty("text", out var pt)) sb.AppendLine(pt.GetString());
            return sb.ToString().Trim();
        }
        return "";
    }

    private static ChatRole RoleFromGeneric(JsonElement m)
    {
        foreach (var key in new[] { "role", "sender", "from" })
            if (m.TryGetProperty(key, out var r) && r.ValueKind == JsonValueKind.String)
                return MapRole(r.GetString());
        return ChatRole.User;
    }

    private static string GenericText(JsonElement m)
    {
        foreach (var key in new[] { "text", "content", "message" })
        {
            if (!m.TryGetProperty(key, out var v)) continue;
            if (v.ValueKind == JsonValueKind.String) return v.GetString() ?? "";
            if (v.ValueKind == JsonValueKind.Array)
            {
                var sb = new StringBuilder();
                foreach (var p in v.EnumerateArray())
                    if (p.ValueKind == JsonValueKind.String) sb.AppendLine(p.GetString());
                    else if (p.ValueKind == JsonValueKind.Object && p.TryGetProperty("text", out var pt)) sb.AppendLine(pt.GetString());
                return sb.ToString().Trim();
            }
        }
        return "";
    }

    private static ChatRole MapRole(string? role) => (role ?? "").ToLowerInvariant() switch
    {
        "assistant" or "claude" or "ai" or "bot" => ChatRole.Assistant,
        "system" or "tool" => ChatRole.System,
        _ => ChatRole.User
    };

    private static string? TryString(JsonElement el, string name)
        => el.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    private static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n);
}
