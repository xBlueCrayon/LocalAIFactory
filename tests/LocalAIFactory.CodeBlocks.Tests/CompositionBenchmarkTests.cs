using System.Text.Json;
using LocalAIFactory.CodeBlocks;
using Xunit;

namespace LocalAIFactory.CodeBlocks.Tests;

/// <summary>
/// The "put 2 + 2 together" benchmark: 20 real-life feature requirements composed deterministically from
/// building blocks. Scores composition accuracy (expected blocks present AND uncovered capabilities honestly
/// flagged as missing) and writes the result JSON. No model required.
/// </summary>
public class CompositionBenchmarkTests
{
    private sealed record Task(string Id, string Requirement, string[] ExpectBlocks, string[] ExpectMissing);

    private static readonly Task[] Tasks =
    {
        new("1","Build a secure login", new[]{"secure-login","password-hashing","audit-event"}, System.Array.Empty<string>()),
        new("2","Build login lockout", new[]{"login-lockout","secure-login"}, System.Array.Empty<string>()),
        new("3","Build maker/checker approval", new[]{"maker-checker","audit-event"}, System.Array.Empty<string>()),
        new("4","Build ERP document lifecycle", new[]{"document-lifecycle","maker-checker"}, System.Array.Empty<string>()),
        new("5","Build a stock transfer", new[]{"stock-movement"}, System.Array.Empty<string>()),
        new("6","Build a manufacturing production order", new[]{"manufacturing-order","stock-movement"}, System.Array.Empty<string>()),
        new("7","Build a report endpoint", new[]{"report-endpoint"}, System.Array.Empty<string>()),
        new("8","Build a Playwright proof", new[]{"playwright-proof"}, System.Array.Empty<string>()),
        new("9","Build an EF migration-safe feature", new[]{"ef-migration"}, System.Array.Empty<string>()),
        new("10","Build a production smoke test", new[]{"production-smoke"}, System.Array.Empty<string>()),
        new("11","Build an Odoo inventory connector", System.Array.Empty<string>(), new[]{"odoo-inventory-connector"}),
        new("12","Build a WooCommerce CSV mapper", System.Array.Empty<string>(), new[]{"woocommerce-csv-mapper"}),
        new("13","Build a cheque OCR extraction pipeline", System.Array.Empty<string>(), new[]{"cheque-ocr-pipeline"}),
        new("14","Build an SFTP file transfer service", System.Array.Empty<string>(), new[]{"sftp-file-transfer"}),
        new("15","Build a ticketing asset workflow", System.Array.Empty<string>(), new[]{"ticketing-asset-workflow"}),
        new("16","Build an MCIB XML export flow", System.Array.Empty<string>(), new[]{"mcib-xml-export"}),
        new("17","Build a Direct Debit mandate approval flow", new[]{"maker-checker"}, new[]{"direct-debit-mandate"}),
        new("18","Build import/export with a rejected-row report", new[]{"import-export"}, System.Array.Empty<string>()),
        new("19","Build a web scraper knowledge proposal", System.Array.Empty<string>(), new[]{"web-scraper-knowledge-proposal"}),
        new("20","Build a generated CRUD module", new[]{"crud-module"}, System.Array.Empty<string>()),
    };

    [Fact]
    public void Composition_benchmark_scores_at_least_80_percent_and_writes_result()
    {
        var composer = new BlockComposer(CodeBlockCatalog.Default());
        var results = new List<object>();
        int passed = 0;
        foreach (var t in Tasks)
        {
            var plan = composer.Compose(t.Requirement);
            var ids = plan.Blocks.Select(b => b.Block.BlockId).ToHashSet(System.StringComparer.OrdinalIgnoreCase);
            var blocksOk = t.ExpectBlocks.All(ids.Contains);
            var missingOk = t.ExpectMissing.All(m => plan.MissingBlocks.Contains(m));
            var ok = blocksOk && missingOk;
            if (ok) passed++;
            results.Add(new
            {
                t.Id, t.Requirement, ok, blocksOk, missingOk,
                composedBlocks = ids.ToArray(), missing = plan.MissingBlocks,
                files = plan.Files.Count, tests = plan.Tests.Count, playwright = plan.PlaywrightProofs.Count,
                securityRisks = plan.SecurityRisks.Count, migrationRisks = plan.MigrationRisks.Count,
                confidence = plan.Confidence
            });
        }
        double score = (double)passed / Tasks.Length;

        var output = new
        {
            kind = "laf-building-block-composition-benchmark",
            stamp = "2026-06-21",
            tasks = Tasks.Length,
            passed,
            scorePct = System.Math.Round(score * 100, 1),
            modelRequired = false,
            note = "Deterministic composition: a task passes when all expected blocks are composed AND every uncovered capability is honestly flagged as a missing brick.",
            results
        };
        var json = JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
        for (var d = new System.IO.DirectoryInfo(System.AppContext.BaseDirectory); d != null; d = d.Parent)
            if (System.IO.File.Exists(System.IO.Path.Combine(d.FullName, "LocalAIFactory.sln")))
            { try { System.IO.File.WriteAllText(System.IO.Path.Combine(d.FullName, "benchmarks", "results", "laf-building-block-composition-benchmark.json"), json); } catch { } break; }

        Assert.True(score >= 0.80, $"composition score {score:P0} below 80% ({passed}/{Tasks.Length})");
    }
}
