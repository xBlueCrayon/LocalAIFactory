namespace LocalAIFactory.CodeBlocks;

public sealed record ComposedBlock(CodeBuildingBlock Block, string Reason);
public sealed record FeaturePlan(
    string Requirement,
    IReadOnlyList<ComposedBlock> Blocks,
    IReadOnlyList<string> MissingBlocks,
    IReadOnlyList<string> Files,
    IReadOnlyList<string> Tests,
    IReadOnlyList<string> PlaywrightProofs,
    IReadOnlyList<string> SecurityRisks,
    IReadOnlyList<string> MigrationRisks,
    IReadOnlyList<string> KnowledgeUsed,
    IReadOnlyList<string> GeneratorTemplates,
    double Confidence);

/// <summary>A registry of code building blocks with deterministic requirement matching and catalogue export.</summary>
public sealed class CodeBlockCatalog
{
    private readonly Dictionary<string, CodeBuildingBlock> _byId = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlyCollection<CodeBuildingBlock> All => _byId.Values;
    public int Count => _byId.Count;

    public CodeBuildingBlock Register(CodeBuildingBlock block) { _byId[block.BlockId] = block; return block; }
    public CodeBuildingBlock? GetById(string id) => _byId.TryGetValue(id, out var b) ? b : null;

    /// <summary>Rank blocks by how many of their keywords appear in the requirement text.</summary>
    public IReadOnlyList<(CodeBuildingBlock Block, int Score)> FindByRequirement(string requirement)
    {
        var text = (requirement ?? "").ToLowerInvariant();
        return _byId.Values
            .Select(b => (Block: b, Score: b.Keywords.Count(k => text.Contains(k))))
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ToList();
    }

    public static CodeBlockCatalog Default()
    {
        var c = new CodeBlockCatalog();
        foreach (var b in DefaultBlockLibrary.Blocks()) c.Register(b);
        return c;
    }
}

/// <summary>
/// Composes building blocks into a feature plan for a requirement: matches blocks, pulls in their transitive
/// dependencies, aggregates files/tests/Playwright/security/migration/knowledge, and honestly reports any
/// required capability that has NO block yet (a missing brick) instead of pretending it exists.
/// </summary>
public sealed class BlockComposer
{
    private readonly CodeBlockCatalog _catalog;
    public BlockComposer(CodeBlockCatalog catalog) => _catalog = catalog;

    // Capability tokens that the catalogue does NOT yet cover; if a requirement needs one, it is a missing brick.
    private static readonly (string token, string capability)[] UncoveredCapabilities =
    {
        ("odoo", "odoo-inventory-connector"), ("woocommerce", "woocommerce-csv-mapper"),
        ("ocr", "cheque-ocr-pipeline"), ("cheque", "cheque-ocr-pipeline"),
        ("sftp", "sftp-file-transfer"), ("mcib", "mcib-xml-export"),
        ("direct debit", "direct-debit-mandate"), ("mandate", "direct-debit-mandate"),
        ("ticketing", "ticketing-asset-workflow"), ("scraper", "web-scraper-knowledge-proposal")
    };

    public FeaturePlan Compose(string requirement)
    {
        var matched = _catalog.FindByRequirement(requirement);
        var chosen = new Dictionary<string, ComposedBlock>(StringComparer.OrdinalIgnoreCase);
        foreach (var (block, score) in matched)
            AddWithDependencies(block, $"matched {score} keyword(s)", chosen, depth: 0);

        var text = (requirement ?? "").ToLowerInvariant();
        var missing = UncoveredCapabilities
            .Where(u => text.Contains(u.token) && !chosen.Values.Any(cb => cb.Block.Keywords.Contains(u.token)))
            .Select(u => u.capability).Distinct().ToList();

        var blocks = chosen.Values.ToList();
        var files = blocks.SelectMany(b => b.Block.GeneratedFiles).Distinct().ToList();
        var tests = blocks.Where(b => !string.IsNullOrEmpty(b.Block.TestPattern)).Select(b => b.Block.TestPattern).Distinct().ToList();
        var playwright = blocks.Where(b => b.Block.HasPlaywright).Select(b => b.Block.PlaywrightPattern!).Distinct().ToList();
        var security = blocks.SelectMany(b => b.Block.SecurityRules).Distinct().ToList();
        var migration = blocks.Where(b => b.Block.RequiresMigration).Select(b => $"{b.Block.Name}: schema/migration impact").Distinct().ToList();
        var knowledge = blocks.SelectMany(b => b.Block.KnowledgeItemIds).Distinct().ToList();
        var templates = blocks.SelectMany(b => b.Block.GeneratorTemplatePaths).Distinct().ToList();

        double conf = blocks.Count == 0 ? 0 : blocks.Average(b => b.Block.Confidence);
        if (missing.Count > 0) conf *= Math.Max(0.3, 1.0 - 0.25 * missing.Count); // honesty penalty for missing bricks

        return new FeaturePlan(requirement ?? "", blocks, missing, files, tests, playwright, security, migration, knowledge, templates, Math.Round(conf, 3));
    }

    private void AddWithDependencies(CodeBuildingBlock block, string reason, Dictionary<string, ComposedBlock> into, int depth)
    {
        if (depth > 6 || into.ContainsKey(block.BlockId)) return;
        into[block.BlockId] = new ComposedBlock(block, reason);
        foreach (var dep in block.Dependencies)
            if (_catalog.GetById(dep) is { } d) AddWithDependencies(d, $"dependency of {block.Name}", into, depth + 1);
    }
}
