using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Backbone;
using LocalAIFactory.Data.Identity;
using LocalAIFactory.Data.Permanence;
using LocalAIFactory.Data.Quality;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class IdentityResolverTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (IdentityResolver resolver, AppDbContext db) NewResolver()
    {
        var db = NewDb();
        var backbone = new KnowledgeBackboneService(db, new ContentHasher(), new InstanceContext(db));
        var resolver = new IdentityResolver(db, backbone, new KnowledgePermanenceService(db), new ContentHasher(),
            new QualityService(db, new QualityEvaluator()));
        return (resolver, db);
    }

    [Fact]
    public void ComputeFileLocusKey_is_normalized_project_scoped_and_64_hex()
    {
        var (r, _) = NewResolver();
        var k1 = r.ComputeFileLocusKey(1, "Src/A.cs");
        var k2 = r.ComputeFileLocusKey(1, "src\\a.cs"); // different case + separator => same logical file
        Assert.Equal(k1, k2);
        Assert.Equal(64, k1.Length);
        Assert.NotEqual(k1, r.ComputeFileLocusKey(2, "Src/A.cs")); // project-scoped
    }

    [Fact]
    public async Task ResolveFile_new_locus_creates_item_with_v1()
    {
        var (r, db) = NewResolver();
        var res = await r.ResolveFileAsync(1, "src/A.cs", "A", "content v1", SourceType.SourceCode);
        Assert.Equal(LocusOutcome.Created, res.Outcome);
        var item = await db.KnowledgeItems.FindAsync(res.KnowledgeItemId);
        Assert.NotNull(item!.SourceLocusKey);
        Assert.Equal(1, item.VersionNumber);
        Assert.Equal(PermanenceTier.Derived, item.Tier);
    }

    [Fact]
    public async Task ResolveFile_same_locus_changed_content_updates_and_versions_then_converges()
    {
        var (r, db) = NewResolver();
        var r1 = await r.ResolveFileAsync(1, "src/A.cs", "A", "v1", SourceType.SourceCode);
        var r2 = await r.ResolveFileAsync(1, "src/A.cs", "A", "v2 changed", SourceType.SourceCode);
        Assert.Equal(LocusOutcome.Updated, r2.Outcome);
        Assert.Equal(r1.KnowledgeItemId, r2.KnowledgeItemId); // same logical item — convergence, not a duplicate

        var item = await db.KnowledgeItems.FindAsync(r2.KnowledgeItemId);
        Assert.Equal(2, item!.VersionNumber);
        Assert.Equal(2, await db.KnowledgeVersions.CountAsync(v => v.KnowledgeItemId == item.Id));
        Assert.Equal(1, await db.KnowledgeItems.CountAsync()); // exactly one item, not two

        // Re-extracting identical content is idempotent (no new version).
        var r3 = await r.ResolveFileAsync(1, "src/A.cs", "A", "v2 changed", SourceType.SourceCode);
        Assert.Equal(LocusOutcome.Unchanged, r3.Outcome);
        Assert.Equal(2, (await db.KnowledgeItems.FindAsync(r2.KnowledgeItemId))!.VersionNumber);
    }

    [Fact]
    public async Task ResolveFile_curated_item_proposes_revision_never_overwrites()
    {
        var (r, db) = NewResolver();
        var res = await r.ResolveFileAsync(2, "src/B.cs", "B", "orig", SourceType.SourceCode);
        var item = (await db.KnowledgeItems.FindAsync(res.KnowledgeItemId))!;
        item.Tier = PermanenceTier.Curated; // simulate human approval/curation
        await db.SaveChangesAsync();

        var rp = await r.ResolveFileAsync(2, "src/B.cs", "B", "changed by re-extraction", SourceType.SourceCode);
        Assert.Equal(LocusOutcome.ProposedRevision, rp.Outcome);
        Assert.Equal(1, await db.ProposedRevisions.CountAsync());
        Assert.Equal("orig", (await db.KnowledgeItems.FindAsync(res.KnowledgeItemId))!.Content); // not overwritten
    }

    [Fact]
    public async Task DetectExactDuplicates_records_one_candidate_for_identical_content()
    {
        var (r, db) = NewResolver();
        await r.ResolveFileAsync(3, "a.txt", "a", "identical body", SourceType.Documentation);
        await r.ResolveFileAsync(3, "b.txt", "b", "identical body", SourceType.Documentation);

        var n = await r.DetectExactDuplicatesAsync(3);
        Assert.Equal(1, n);
        var dup = await db.KnowledgeDuplicates.SingleAsync();
        Assert.Equal(DuplicateMatchKind.Exact, dup.MatchKind);
        Assert.Equal(DuplicateStatus.Candidate, dup.Status);
        Assert.NotEqual(dup.KnowledgeItemUid, dup.DuplicateOfUid);

        // Idempotent: running again records no new candidates.
        Assert.Equal(0, await r.DetectExactDuplicatesAsync(3));
    }
}
