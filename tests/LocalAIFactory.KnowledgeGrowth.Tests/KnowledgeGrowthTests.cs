using LocalAIFactory.KnowledgeGrowth;
using Xunit;

namespace LocalAIFactory.KnowledgeGrowth.Tests;

public class KnowledgeGrowthTests
{
    private static FetchedDocument Doc(string url, string html = "<h1>Title</h1><p>EF Core migrations evolve the schema. Apply them with dotnet ef database update.</p><p>Use a design-time factory for tooling.</p>")
        => new(url, "Doc", html);

    [Theory]
    [InlineData("https://learn.microsoft.com/ef/core/migrations", true)]
    [InlineData("https://docs.python.org/3/library/json.html", true)]
    [InlineData("https://docs.ollama.com/api", true)]
    [InlineData("https://evil.example.com/x", false)]
    [InlineData("http://learn.microsoft.com/x", false)]  // plain http rejected
    [InlineData("not-a-url", false)]
    public void Allowlist_accepts_only_https_listed_domains(string url, bool allowed)
        => Assert.Equal(allowed, new ScrapeAllowlist().IsAllowed(url));

    [Fact] public void Disallowed_url_is_rejected_no_proposal()
    {
        var outcome = new KnowledgeGrowthService().Ingest(Doc("https://evil.example.com/x"));
        Assert.False(outcome.Accepted);
        Assert.Null(outcome.Proposal);
    }

    [Fact] public void Allowed_url_produces_a_proposal_with_citation()
    {
        var outcome = new KnowledgeGrowthService().Ingest(Doc("https://learn.microsoft.com/ef/core/migrations"));
        Assert.True(outcome.Accepted);
        Assert.NotNull(outcome.Proposal);
        Assert.Equal("https://learn.microsoft.com/ef/core/migrations", outcome.Proposal!.Citation.Url);
        Assert.False(string.IsNullOrEmpty(outcome.Proposal.Citation.SourceHash));
        Assert.False(string.IsNullOrEmpty(outcome.Proposal.Citation.FetchDateUtc));
    }

    [Fact] public void Proposal_is_not_auto_approved()
        => Assert.False(new KnowledgeGrowthService().Ingest(Doc("https://learn.microsoft.com/x")).Proposal!.Approved);

    [Fact] public void Proposal_stores_summarised_facts_not_raw_html()
    {
        var bigBody = "<p>" + new string('A', 5000) + "</p><p>Second useful sentence about migrations.</p>";
        var outcome = new KnowledgeGrowthService().Ingest(Doc("https://learn.microsoft.com/x", "<h1>T</h1>" + bigBody));
        Assert.All(outcome.Proposal!.Facts, f => Assert.True(f.Length <= 300)); // never a giant raw dump
        Assert.True(outcome.Proposal.Facts.Count <= 20);
    }

    [Fact] public void Duplicate_source_is_detected_by_content_hash()
    {
        var svc = new KnowledgeGrowthService();
        var first = svc.Ingest(Doc("https://learn.microsoft.com/x"));
        var second = svc.Ingest(Doc("https://learn.microsoft.com/x"));
        Assert.True(first.Accepted);
        Assert.False(second.Accepted);
        Assert.Contains("duplicate", second.Reason);
        Assert.Single(svc.Cached);
    }

    [Fact] public void Empty_document_yields_no_proposal()
        => Assert.False(new KnowledgeGrowthService().Ingest(Doc("https://learn.microsoft.com/x", "<html></html>")).Accepted);

    [Fact] public void Github_docs_are_allowed_on_request()
        => Assert.True(new ScrapeAllowlist().IsAllowed("https://github.com/org/repo/blob/main/README.md"));
}
