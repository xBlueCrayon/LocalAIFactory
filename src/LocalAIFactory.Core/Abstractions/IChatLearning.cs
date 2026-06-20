namespace LocalAIFactory.Core.Abstractions;

// R2-ACC-20X: Chat-import learning. Turn an exported AI conversation (ChatGPT / Claude / pasted markdown or
// plain text) into CANDIDATE knowledge proposals — decisions, fix patterns, reusable rules, "do not repeat"
// lessons, code snippets and prompts. This is a deterministic, offline text transform: it produces PROPOSALS
// only. Nothing it emits is auto-approved or written over curated truth — proposals flow through the normal
// approval / IPermanenceGuard lifecycle, exactly like any other extracted knowledge. MSSQL stays the source of
// truth; the extractor never decides what is "true", only what is worth a human's review.
public enum ChatExportFormat
{
    PlainText = 0,
    Markdown = 1,
    ChatGptMarkdown = 2,
    ClaudeMarkdown = 3
}

// What kind of reusable knowledge a line/segment represents. Ordered roughly by how actionable it is.
public enum ChatKnowledgeKind
{
    Decision = 0,       // "we decided to…", "we'll use…", "chosen approach…"
    FixPattern = 1,     // "the bug was…", "root cause…", "fixed by…"
    ReusableRule = 2,   // "always…", "convention:…", "best practice…"
    DoNotRepeat = 3,    // "never…", "don't…", "must not…", "avoid…"
    CodeSnippet = 4,    // fenced code block
    Prompt = 5,         // a reusable prompt the user wrote
    Insight = 6         // substantive explanatory takeaway (lowest confidence, most conservative)
}

// One speaker turn after segmentation. Role is normalised to "user", "assistant" or "unknown".
public sealed record ChatMessageSegment(int Index, string Role, string Text);

// A single candidate piece of knowledge. NormalizedKey is the dedup key (kind + normalised text). Confidence is
// a conservative 0..1 score; ProjectHint carries an optional project association for routing to project memory.
public sealed record ChatKnowledgeProposal(
    ChatKnowledgeKind Kind,
    string Title,
    string Content,
    string? Language,
    double Confidence,
    int SourceMessageIndex,
    string SourceRole,
    string NormalizedKey,
    string? ProjectHint);

// Result of one extraction run. DuplicatesSuppressed counts proposals collapsed because an equal NormalizedKey
// was already emitted (within this run) — the same lesson stated twice yields one proposal, not two.
public sealed record ChatExtractionResult(
    ChatExportFormat Format,
    int MessageCount,
    IReadOnlyList<ChatMessageSegment> Messages,
    IReadOnlyList<ChatKnowledgeProposal> Proposals,
    int DuplicatesSuppressed);

public interface IChatKnowledgeExtractor
{
    // Deterministic: the same input always yields the same proposals in the same order. No network, no model,
    // no DB. projectHint (optional) is attached to every proposal so the importer can route it to the right
    // project memory; null/empty means general/unassigned and the caller decides scope at approval time.
    ChatExtractionResult Extract(string chatText, ChatExportFormat format, string? projectHint = null);
}
