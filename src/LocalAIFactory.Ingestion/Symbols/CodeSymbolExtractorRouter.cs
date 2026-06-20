using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Ingestion.Symbols;

// KE-008: dispatches content to the extractor registered for a DetectedLanguage. Adding a language
// (VB.NET, Razor) means registering one more ICodeSymbolExtractor — no change here or in the pipeline.
public sealed class CodeSymbolExtractorRouter : ICodeSymbolExtractorRouter
{
    private readonly Dictionary<string, ICodeSymbolExtractor> _byLanguage;

    public CodeSymbolExtractorRouter(IEnumerable<ICodeSymbolExtractor> extractors)
    {
        _byLanguage = extractors.ToDictionary(e => e.Language, StringComparer.OrdinalIgnoreCase);
    }

    public bool CanExtract(string? detectedLanguage)
        => !string.IsNullOrEmpty(detectedLanguage) && _byLanguage.ContainsKey(detectedLanguage);

    public CodeExtractionResult Extract(string? detectedLanguage, string content)
        => !string.IsNullOrEmpty(detectedLanguage) && _byLanguage.TryGetValue(detectedLanguage, out var ex)
            ? ex.Extract(content)
            : CodeExtractionResult.Empty;
}
