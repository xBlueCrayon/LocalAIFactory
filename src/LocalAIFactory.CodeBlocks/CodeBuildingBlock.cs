namespace LocalAIFactory.CodeBlocks;

/// <summary>
/// A reusable engineering "brick": a named, well-understood pattern (secure login, maker/checker, a report
/// endpoint, ...) with the files it generates, the tests/Playwright that prove it, its security + validation
/// rules, its failure modes, and links (by id) to the knowledge, code-graph nodes, generator templates and
/// experiences that back it. Blocks are composed into feature plans by the <see cref="BlockComposer"/>.
/// </summary>
public sealed class CodeBuildingBlock
{
    public required string BlockId { get; init; }
    public required string Name { get; init; }
    public required string Purpose { get; init; }
    public string ProblemSolved { get; init; } = "";
    public List<string> RequiredInputs { get; init; } = new();
    public List<string> GeneratedFiles { get; init; } = new();
    public string CodePatternSummary { get; init; } = "";
    /// <summary>Other BlockIds this block depends on (e.g. login depends on password-hashing + audit-event).</summary>
    public List<string> Dependencies { get; init; } = new();
    public List<string> SecurityRules { get; init; } = new();
    public List<string> ValidationRules { get; init; } = new();
    public string TestPattern { get; init; } = "";
    public string? PlaywrightPattern { get; init; }
    public List<string> FailureModes { get; init; } = new();
    public List<string> ExampleSourceFiles { get; init; } = new();
    public List<string> KnowledgeItemIds { get; init; } = new();
    public List<string> GeneratorTemplatePaths { get; init; } = new();
    public List<string> ExperienceIds { get; init; } = new();
    /// <summary>Lower-case keywords used to match a requirement to this block.</summary>
    public List<string> Keywords { get; init; } = new();
    public double Confidence { get; init; } = 0.8;

    public bool RequiresMigration => ValidationRules.Concat(SecurityRules).Concat(new[] { CodePatternSummary, Purpose })
        .Any(s => s.Contains("migration", StringComparison.OrdinalIgnoreCase) || s.Contains("schema", StringComparison.OrdinalIgnoreCase));
    public bool HasPlaywright => !string.IsNullOrEmpty(PlaywrightPattern);
}
