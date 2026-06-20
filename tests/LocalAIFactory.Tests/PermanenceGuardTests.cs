using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Permanence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

// KE-002 regression guard: the permanence contract must still hold after KE-003.
public class PermanenceGuardTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    [Fact]
    public void IsCurated_true_only_for_curated_tier()
    {
        var g = new KnowledgePermanenceService(NewDb());
        Assert.True(g.IsCurated(PermanenceTier.Curated));
        Assert.False(g.IsCurated(PermanenceTier.Derived));
        Assert.False(g.IsCurated(PermanenceTier.Raw));
    }

    [Fact]
    public async Task ProposeRevision_dedups_identical_open_proposal_but_allows_distinct_content()
    {
        var db = NewDb();
        var g = new KnowledgePermanenceService(db);

        var id1 = await g.ProposeRevisionAsync("ProjectProfileSection", 5, null, "T", "new content", "reason", RevisionSource.Extraction);
        var id2 = await g.ProposeRevisionAsync("ProjectProfileSection", 5, null, "T", "new content", "reason", RevisionSource.Extraction);
        Assert.Equal(id1, id2); // H1 dedup: identical open proposal is reused, not duplicated
        Assert.Equal(1, await db.ProposedRevisions.CountAsync());

        var id3 = await g.ProposeRevisionAsync("ProjectProfileSection", 5, null, "T", "different content", "reason", RevisionSource.Extraction);
        Assert.NotEqual(id1, id3); // genuinely different content => new proposal
        Assert.Equal(2, await db.ProposedRevisions.CountAsync());
    }
}
