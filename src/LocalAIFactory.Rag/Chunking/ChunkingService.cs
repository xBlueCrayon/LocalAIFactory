using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Rag.Chunking;

public sealed class ChunkingService : IChunkingService
{
    public IReadOnlyList<string> Chunk(string text, int maxChars, int overlap)
    {
        var result = new List<string>();
        if (string.IsNullOrWhiteSpace(text)) return result;
        text = text.Replace("\r\n", "\n").Trim();
        if (maxChars <= 0) maxChars = 1200;
        if (overlap < 0 || overlap >= maxChars) overlap = Math.Min(150, maxChars / 4);

        int pos = 0;
        while (pos < text.Length)
        {
            int end = Math.Min(pos + maxChars, text.Length);
            if (end < text.Length)
            {
                int br = text.LastIndexOf('\n', end - 1, Math.Min(end - pos, 200));
                if (br <= pos) br = text.LastIndexOf(' ', end - 1, Math.Min(end - pos, 200));
                if (br > pos) end = br;
            }
            var piece = text.Substring(pos, end - pos).Trim();
            if (piece.Length > 0) result.Add(piece);
            if (end >= text.Length) break;
            pos = Math.Max(end - overlap, pos + 1);
        }
        return result;
    }

    public int EstimateTokens(string text)
        => string.IsNullOrEmpty(text) ? 0 : Math.Max(1, text.Length / 4);
}
