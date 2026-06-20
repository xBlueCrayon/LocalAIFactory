using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Ingestion.Documents;

// R2-ACC-CAP4: deterministic, dependency-free PDF analysis. Hashes the document (provenance), detects whether
// it is a PDF, counts pages, classifies text-based vs scanned-image-only from resource/operator markers, and
// best-effort reads the Info /Title. It does NOT decode content streams — full text extraction needs a PDF
// parser library, which is stated honestly in Notes (never faked).
public sealed class PdfAnalyzer : IPdfAnalyzer
{
    private static readonly Regex PageRx = new(@"/Type\s*/Page\b(?!s)", RegexOptions.Compiled);
    private static readonly Regex TitleRx = new(@"/Title\s*\(([^)]{0,300})\)", RegexOptions.Compiled);

    public PdfAnalysis Analyze(byte[] bytes)
    {
        var hash = Convert.ToHexStringLower(SHA256.HashData(bytes ?? Array.Empty<byte>()));
        var notes = new List<string>();
        if (bytes is null || bytes.Length == 0)
            return new PdfAnalysis(hash, false, PdfClass.Empty, 0, null, false, false, new[] { "Empty document." });

        // PDFs are byte-oriented; read as Latin-1 so markers survive without UTF-8 decoding errors.
        var head = Encoding.Latin1.GetString(bytes, 0, Math.Min(bytes.Length, 1024));
        var text = Encoding.Latin1.GetString(bytes);
        var isPdf = head.Contains("%PDF-");
        if (!isPdf)
            return new PdfAnalysis(hash, false, PdfClass.NotPdf, 0, null, false, false, new[] { "Not a PDF (missing %PDF- header)." });

        var pages = PageRx.Matches(text).Count;
        bool hasFont = text.Contains("/Font");
        bool hasTextOp = text.Contains(" Tj") || text.Contains(" TJ") || text.Contains("BT\n") || text.Contains("BT ");
        bool hasImage = text.Contains("/Subtype/Image") || text.Contains("/Subtype /Image")
                        || text.Contains("/DCTDecode") || text.Contains("/CCITTFaxDecode") || text.Contains("/JPXDecode");
        bool hasText = hasFont || hasTextOp;

        PdfClass cls;
        if (!hasText && !hasImage) cls = PdfClass.Empty;
        else if (hasText && hasImage) cls = PdfClass.Mixed;
        else if (hasText) cls = PdfClass.TextBased;
        else cls = PdfClass.ScannedImageOnly;

        var title = TitleRx.Match(text) is { Success: true } m ? m.Groups[1].Value.Trim() : null;
        bool extractable = cls is PdfClass.TextBased or PdfClass.Mixed;
        bool ocrRequired = cls is PdfClass.ScannedImageOnly || (cls == PdfClass.Mixed && hasImage);

        notes.Add("Classification is a deterministic heuristic over PDF markers (resources/operators).");
        notes.Add("Full text extraction requires a PDF parser library (not bundled); this prototype provides classification + provenance only.");
        if (ocrRequired) notes.Add("Scanned/image-only content detected — OCR is required to read the text.");
        return new PdfAnalysis(hash, true, cls, pages, string.IsNullOrEmpty(title) ? null : title, extractable, ocrRequired, notes);
    }
}

// R2-ACC-CAP4: deterministic extractive summarizer. Selects the highest-signal SOURCE sentences (by significant-
// word frequency) and returns them verbatim WITH their page — it never generates or paraphrases text, so it
// cannot hallucinate. Output order follows the source (page, then position) for readability.
public sealed class ExtractiveSummarizer : IExtractiveSummarizer
{
    private static readonly Regex SentenceRx = new(@"[^.!?]+[.!?]", RegexOptions.Compiled);
    private static readonly HashSet<string> Stop = new(StringComparer.OrdinalIgnoreCase)
    { "this","that","with","from","have","they","were","will","would","there","their","which","about","into","than","then","them","these","those","such","also","been","being","over","under","more","most","some","other" };

    public ExtractiveSummary Summarize(IReadOnlyList<TextSegment> segments, int maxSentences = 5)
    {
        var sentences = new List<(int page, int order, string text)>();
        int order = 0;
        foreach (var seg in segments ?? Array.Empty<TextSegment>())
            foreach (Match s in SentenceRx.Matches(seg.Text ?? ""))
            {
                var t = s.Value.Trim();
                if (t.Length >= 12) sentences.Add((seg.Page, order++, t));
            }
        if (sentences.Count == 0) return new ExtractiveSummary(Array.Empty<SummarySentence>(), 0, true);

        // Significant-word frequency across the corpus.
        var freq = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var (_, _, t) in sentences)
            foreach (var w in Words(t))
                freq[w] = freq.GetValueOrDefault(w) + 1;

        var scored = sentences
            .Select(s => (s.page, s.order, s.text, score: Words(s.text).Sum(w => freq.GetValueOrDefault(w)) / (double)Math.Max(8, Words(s.text).Count())))
            .OrderByDescending(x => x.score).ThenBy(x => x.order)
            .Take(Math.Max(1, maxSentences))
            .OrderBy(x => x.page).ThenBy(x => x.order)
            .Select(x => new SummarySentence(x.page, x.text))
            .ToList();

        return new ExtractiveSummary(scored, sentences.Count, true);
    }

    private static IEnumerable<string> Words(string text) =>
        Regex.Matches(text, @"[A-Za-z]{4,}").Select(m => m.Value.ToLowerInvariant()).Where(w => !Stop.Contains(w));
}
