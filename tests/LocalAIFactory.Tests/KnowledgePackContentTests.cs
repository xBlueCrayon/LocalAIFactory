using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Backbone;
using LocalAIFactory.Data.Permanence;
using LocalAIFactory.Rag.Chunking;
using LocalAIFactory.Rag.KnowledgePacks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-20X: the THREE new shipped knowledge packs must install cleanly through the REAL installer (full
// in-memory validation + idempotent DB writes). This is genuine install proof — not a JSON lint — and it runs
// in CI on every build. Each pack is validated end-to-end against a fresh in-memory store.
public class KnowledgePackContentTests
{
    public static IEnumerable<object[]> NewPacks() => new[]
    {
        new object[] { "financial-institution-operations-v1", 16 },
        new object[] { "kyc-aml-transaction-approval-v1", 16 },
        new object[] { "market-intelligence-forecasting-v1", 16 },
    };

    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static KnowledgePackInstaller NewInstaller(AppDbContext db) =>
        new(db, new ContentHasher(), new InstanceContext(db), new KnowledgePermanenceService(db),
            new ChunkingService(), new NoopIndexer(), Options.Create(new RagOptions()),
            NullLogger<KnowledgePackInstaller>.Instance);

    private sealed class NoopIndexer : IKnowledgeIndexer
    {
        public Task IndexKnowledgeItemAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
        public Task RemoveKnowledgeItemAsync(int id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private static string PackDir(string name)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        for (int i = 0; i < 8 && dir is not null; i++, dir = dir.Parent)
        {
            var candidate = Path.Combine(dir.FullName, "knowledge-packs", name);
            if (Directory.Exists(candidate)) return candidate;
        }
        throw new DirectoryNotFoundException($"Could not locate knowledge-packs/{name} from {AppContext.BaseDirectory}");
    }

    [Theory]
    [MemberData(nameof(NewPacks))]
    public async Task New_pack_installs_cleanly_through_real_installer(string packName, int expectedItems)
    {
        var db = NewDb();
        var res = await NewInstaller(db).InstallAsync(PackDir(packName), "ship-test");

        Assert.True(res.Success, res.Errors.Count > 0 ? string.Join("; ", res.Errors) : "install failed");
        Assert.Empty(res.Errors);
        Assert.Equal(expectedItems, res.TotalItems);
        Assert.Equal(expectedItems, res.Created);
        Assert.Equal(expectedItems, await db.KnowledgeItems.CountAsync(k => k.KnowledgePackId != null));
        // Every shipped baseline item is Curated (protected) and global (not project-scoped).
        Assert.True(await db.KnowledgeItems.AllAsync(k => k.Tier == PermanenceTier.Curated && k.ProjectId == null));
        // Provenance records the pack origin for governance.
        var pack = await db.KnowledgePacks.SingleAsync();
        Assert.True(await db.ProvenanceEvents.AnyAsync(p => p.OriginPackUid == pack.Uid && p.Method == ProvenanceMethod.Import));
    }

    [Theory]
    [MemberData(nameof(NewPacks))]
    public async Task New_pack_reinstall_is_idempotent(string packName, int expectedItems)
    {
        var db = NewDb();
        var dir = PackDir(packName);
        await NewInstaller(db).InstallAsync(dir, "ship-test");
        var second = await NewInstaller(db).InstallAsync(dir, "ship-test");

        Assert.True(second.Success);
        Assert.Equal(0, second.Created);                              // nothing new on a re-install
        Assert.True(second.AlreadyCurrent || second.Unchanged == expectedItems);
        Assert.Equal(expectedItems, await db.KnowledgeItems.CountAsync(k => k.KnowledgePackId != null));
        Assert.Equal(1, await db.KnowledgePacks.CountAsync());        // no duplicate pack anchor
    }
}
