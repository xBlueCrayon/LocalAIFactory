using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Backbone;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class KnowledgeBackboneTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private static (KnowledgeBackboneService svc, AppDbContext db) NewService()
    {
        var db = NewDb();
        var svc = new KnowledgeBackboneService(db, new ContentHasher(), new InstanceContext(db));
        return (svc, db);
    }

    [Fact]
    public void New_items_get_unique_nonempty_portable_uids()
    {
        var a = new KnowledgeItem();
        var b = new KnowledgeItem();
        Assert.NotEqual(Guid.Empty, a.Uid);
        Assert.NotEqual(Guid.Empty, b.Uid);
        Assert.NotEqual(a.Uid, b.Uid);
    }

    [Fact]
    public async Task RecordInitial_creates_v1_version_and_provenance_with_origin()
    {
        var (svc, db) = NewService();
        var item = new KnowledgeItem { Title = "T", Content = "hello", Tier = PermanenceTier.Curated };
        db.KnowledgeItems.Add(item);
        await db.SaveChangesAsync();

        await svc.RecordInitialAsync(item, ProvenanceMethod.Human, "tester", "Created");

        Assert.NotEqual("", item.ContentHash);
        Assert.Equal(1, item.VersionNumber);

        var versions = await db.KnowledgeVersions.Where(v => v.KnowledgeItemId == item.Id).ToListAsync();
        Assert.Single(versions);
        Assert.Equal(1, versions[0].VersionNumber);
        Assert.Equal("hello", versions[0].ContentSnapshot);
        Assert.Equal(item.Uid, versions[0].KnowledgeItemUid);

        var prov = await db.ProvenanceEvents.Where(p => p.KnowledgeItemId == item.Id).ToListAsync();
        Assert.Single(prov);
        Assert.NotNull(prov[0].OriginInstanceId);
        Assert.NotEqual(Guid.Empty, prov[0].OriginInstanceId!.Value);
    }

    [Fact]
    public async Task RecordEdit_with_changed_content_creates_a_new_version_with_lineage()
    {
        var (svc, db) = NewService();
        var item = new KnowledgeItem { Title = "T", Content = "v1" };
        db.KnowledgeItems.Add(item);
        await db.SaveChangesAsync();
        await svc.RecordInitialAsync(item, ProvenanceMethod.Human, "t", "Created");

        item.Content = "v2 changed";
        await svc.RecordEditAsync(item, "edited", ProvenanceMethod.Human, "t");

        Assert.Equal(2, item.VersionNumber);
        var versions = await db.KnowledgeVersions.Where(v => v.KnowledgeItemId == item.Id)
            .OrderBy(v => v.VersionNumber).ToListAsync();
        Assert.Equal(2, versions.Count);
        Assert.Equal("v2 changed", versions[1].ContentSnapshot);
        Assert.Equal(versions[0].Uid, versions[1].PreviousVersionUid);
    }

    [Fact]
    public async Task RecordEdit_with_same_content_is_convergent_no_new_version()
    {
        var (svc, db) = NewService();
        var item = new KnowledgeItem { Title = "T", Content = "same" };
        db.KnowledgeItems.Add(item);
        await db.SaveChangesAsync();
        await svc.RecordInitialAsync(item, ProvenanceMethod.Human, "t", "Created");

        await svc.RecordEditAsync(item, "no-op edit", ProvenanceMethod.Human, "t");

        Assert.Equal(1, item.VersionNumber); // hash-guard: no spurious version
        Assert.Single(await db.KnowledgeVersions.Where(v => v.KnowledgeItemId == item.Id).ToListAsync());
        Assert.Equal(2, await db.ProvenanceEvents.CountAsync(p => p.KnowledgeItemId == item.Id)); // provenance still appended
    }

    [Fact]
    public async Task Reconciliation_identical_content_yields_same_hash_distinct_uid()
    {
        var (svc, db) = NewService();
        var a = new KnowledgeItem { Title = "A", Content = "shared body" };
        var b = new KnowledgeItem { Title = "B", Content = "shared body" };
        db.KnowledgeItems.AddRange(a, b);
        await db.SaveChangesAsync();
        await svc.RecordInitialAsync(a, ProvenanceMethod.Deterministic, "s", "x");
        await svc.RecordInitialAsync(b, ProvenanceMethod.Deterministic, "s", "x");

        Assert.Equal(a.ContentHash, b.ContentHash); // match-by-content-hash (dedup primitive)
        Assert.NotEqual(a.Uid, b.Uid);              // distinct portable identities (match-by-uid)
    }

    [Fact]
    public async Task Full_history_is_retrievable_in_order()
    {
        var (svc, db) = NewService();
        var item = new KnowledgeItem { Title = "T", Content = "1" };
        db.KnowledgeItems.Add(item);
        await db.SaveChangesAsync();
        await svc.RecordInitialAsync(item, ProvenanceMethod.Human, "t", "v1");
        item.Content = "2"; await svc.RecordEditAsync(item, "to v2", ProvenanceMethod.Human, "t");
        item.Content = "3"; await svc.RecordEditAsync(item, "to v3", ProvenanceMethod.Human, "t");

        var versions = await db.KnowledgeVersions.Where(v => v.KnowledgeItemId == item.Id)
            .OrderBy(v => v.VersionNumber).Select(v => v.VersionNumber).ToListAsync();
        Assert.Equal(new[] { 1, 2, 3 }, versions);
    }
}
