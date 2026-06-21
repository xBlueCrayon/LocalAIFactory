using LocalAIFactory.CodeBlocks;
using Xunit;

namespace LocalAIFactory.CodeBlocks.Tests;

public class CodeBlocksTests
{
    private static CodeBlockCatalog Cat() => CodeBlockCatalog.Default();

    [Fact] public void Default_catalog_has_blocks()
        => Assert.True(Cat().Count >= 16);

    [Fact] public void Get_by_id_returns_the_block()
        => Assert.Equal("Secure login", Cat().GetById("secure-login")!.Name);

    [Theory]
    [InlineData("secure login", "secure-login")]
    [InlineData("login lockout after failed attempts", "login-lockout")]
    [InlineData("maker checker approval", "maker-checker")]
    [InlineData("ef migration", "ef-migration")]
    [InlineData("report endpoint", "report-endpoint")]
    [InlineData("stock inventory movement", "stock-movement")]
    [InlineData("double entry accounting posting", "accounting-posting")]
    [InlineData("playwright proof", "playwright-proof")]
    [InlineData("crud list create edit deactivate", "crud-module")]
    [InlineData("bom production order", "manufacturing-order")]
    public void Requirement_matches_expected_block(string req, string expectedId)
        => Assert.Contains(Cat().FindByRequirement(req), m => m.Block.BlockId == expectedId);

    [Fact] public void Compose_secure_login_pulls_transitive_dependencies()
    {
        var plan = new BlockComposer(Cat()).Compose("build a secure login");
        var ids = plan.Blocks.Select(b => b.Block.BlockId).ToList();
        Assert.Contains("secure-login", ids);
        Assert.Contains("password-hashing", ids); // dependency
        Assert.Contains("audit-event", ids);      // dependency
        Assert.Contains("anti-forgery", ids);     // dependency
    }

    [Fact] public void Compose_login_lockout_pulls_two_levels_of_dependencies()
    {
        var ids = new BlockComposer(Cat()).Compose("add login lockout").Blocks.Select(b => b.Block.BlockId).ToList();
        Assert.Contains("login-lockout", ids);
        Assert.Contains("secure-login", ids);     // dep
        Assert.Contains("password-hashing", ids); // dep of dep
    }

    [Fact] public void Compose_aggregates_files_tests_and_security()
    {
        var plan = new BlockComposer(Cat()).Compose("secure login with lockout");
        Assert.NotEmpty(plan.Files);
        Assert.NotEmpty(plan.Tests);
        Assert.NotEmpty(plan.SecurityRisks);
        Assert.Contains(plan.KnowledgeUsed, k => k.Contains("auth"));
    }

    [Fact] public void Compose_reports_missing_block_for_uncovered_capability()
    {
        var plan = new BlockComposer(Cat()).Compose("build an Odoo inventory connector");
        Assert.Contains("odoo-inventory-connector", plan.MissingBlocks);
    }

    [Fact] public void Missing_block_lowers_confidence()
    {
        var withMissing = new BlockComposer(Cat()).Compose("build a cheque OCR pipeline");
        Assert.Contains("cheque-ocr-pipeline", withMissing.MissingBlocks);
        Assert.True(withMissing.Confidence < 0.9);
    }

    [Fact] public void Compose_unrelated_text_yields_no_blocks()
        => Assert.Empty(new BlockComposer(Cat()).Compose("xyzzy frobnicate the plover").Blocks);

    [Fact] public void Migration_block_flags_migration_risk()
    {
        var plan = new BlockComposer(Cat()).Compose("add an ef migration for a new column");
        Assert.Contains(plan.MigrationRisks, r => r.Contains("migration", System.StringComparison.OrdinalIgnoreCase));
    }

    [Fact] public void Extractor_detects_password_hashing_from_filename()
    {
        var ev = new BlockExtractor(Cat()).Extract(new[] { "x/src/LafErp.Services/PasswordHasher.cs", "y/Other.cs" });
        Assert.Contains(ev, e => e.BlockId == "password-hashing");
    }

    [Fact] public void Extractor_detects_multiple_blocks()
    {
        var ev = new BlockExtractor(Cat()).Extract(new[]
        {
            "src/LafErp.Services/AuditService.cs", "src/LafErp.Services/StockService.cs", "src/LafErp.Services/AccountingService.cs"
        });
        var ids = ev.Select(e => e.BlockId).ToList();
        Assert.Contains("audit-event", ids);
        Assert.Contains("stock-movement", ids);
        Assert.Contains("accounting-posting", ids);
    }

    [Fact] public void Every_block_has_purpose_and_test_pattern()
    {
        foreach (var b in Cat().All)
        {
            Assert.False(string.IsNullOrWhiteSpace(b.Purpose), b.BlockId);
            Assert.False(string.IsNullOrWhiteSpace(b.TestPattern), b.BlockId);
        }
    }

    [Fact] public void All_dependencies_resolve_to_real_blocks()
    {
        var cat = Cat();
        foreach (var b in cat.All)
            foreach (var dep in b.Dependencies)
                Assert.NotNull(cat.GetById(dep));
    }
}
