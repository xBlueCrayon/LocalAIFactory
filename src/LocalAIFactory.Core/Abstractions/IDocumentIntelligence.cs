namespace LocalAIFactory.Core.Abstractions;

// R2-ACC-CAP4: PDF reader/classifier/summarizer prototype. Provenance-first and honest: a document is always
// hashed; text-based vs scanned-image-only is detected deterministically from the raw bytes; summaries are
// EXTRACTIVE (selected source sentences, never invented) and carry page provenance. Full content-stream text
// extraction needs a parser library — that boundary is stated, never faked.

public enum PdfClass { NotPdf = 0, Empty = 1, TextBased = 2, ScannedImageOnly = 3, Mixed = 4 }

// Structured result of analysing a PDF's raw bytes. ExtractableText is true only when the prototype is
// confident text can be recovered (text-based); scanned/image-only documents are flagged OCR-required.
public sealed record PdfAnalysis(
    string DocumentHash,        // SHA-256 of the bytes — provenance/tamper-evidence
    bool IsPdf,
    PdfClass Class,
    int PageCount,
    string? Title,              // best-effort from the Info dictionary, else null
    bool ExtractableText,       // true => text-based; false => OCR required or empty
    bool OcrRequired,
    IReadOnlyList<string> Notes); // honest notes (e.g. "full text extraction requires a PDF parser library")

public interface IPdfAnalyzer
{
    PdfAnalysis Analyze(byte[] bytes);
}

// A page-tagged text segment used as the provenance unit for summarization.
public sealed record TextSegment(int Page, string Text);

// An extractive summary sentence with the page it came from (provenance preserved).
public sealed record SummarySentence(int Page, string Text);

public sealed record ExtractiveSummary(
    IReadOnlyList<SummarySentence> Sentences,
    int SourceSegmentCount,
    bool IsExtractive); // always true — these are selected source sentences, not generated text

// Deterministic extractive summarizer over page-tagged segments. Never hallucinates: every output sentence is
// a verbatim source sentence with its page. (Optional model-based abstractive summary is a separate, clearly
// experimental path that is not implemented in this prototype.)
public interface IExtractiveSummarizer
{
    ExtractiveSummary Summarize(IReadOnlyList<TextSegment> segments, int maxSentences = 5);
}
