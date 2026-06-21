namespace LocalAIFactory.CodeBlocks;

public sealed record BlockEvidence(string BlockId, string Name, IReadOnlyList<string> MatchedFiles, double Confidence);

/// <summary>
/// Detects which catalogue blocks are evidenced in a set of file paths, by matching a block's generated/example
/// file names against the actual files. Deterministic; used to mine blocks out of an existing codebase.
/// </summary>
public sealed class BlockExtractor
{
    private readonly CodeBlockCatalog _catalog;
    public BlockExtractor(CodeBlockCatalog catalog) => _catalog = catalog;

    public IReadOnlyList<BlockEvidence> Extract(IEnumerable<string> filePaths)
    {
        var files = filePaths.Select(p => p.Replace('\\', '/')).ToList();
        var result = new List<BlockEvidence>();
        foreach (var b in _catalog.All)
        {
            // Each signature is a concrete file name (wildcards stripped) the block is known to generate.
            var signatures = b.GeneratedFiles.Concat(b.ExampleSourceFiles)
                .Where(f => !f.Contains('*'))
                .Select(f => Path.GetFileName(f.Replace('\\', '/')))
                .Where(s => s.Length > 3).Distinct().ToList();
            if (signatures.Count == 0) continue;
            var matched = files.Where(f =>
            {
                var name = Path.GetFileName(f);
                return signatures.Any(sig => string.Equals(name, sig, StringComparison.OrdinalIgnoreCase));
            }).ToList();
            if (matched.Count > 0)
                result.Add(new BlockEvidence(b.BlockId, b.Name, matched, b.Confidence));
        }
        return result;
    }
}
