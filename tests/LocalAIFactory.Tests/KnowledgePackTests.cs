using System.Text.Json;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Backbone;
using LocalAIFactory.Data.Permanence;
using LocalAIFactory.Data.Security;
using LocalAIFactory.Rag.Chunking;
using LocalAIFactory.Rag.KnowledgePacks;
using LocalAIFactory.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-B1: Professional Base Knowledge Pack — installer + UI security regression tests.
public class KnowledgePackTests
{
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

    private static readonly Guid PackA = Guid.Parse("0a510000-0001-4000-8000-0000000000aa");

    // Writes a minimal but schema-valid pack to a temp dir; returns the directory.
    private static string WritePack(Guid packUid, string version, params (string uid, string title, string desc, double conf)[] items)
    {
        var dir = Path.Combine(Path.GetTempPath(), "lafpack_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var itemObjs = items.Select(i => (object)new
        {
            uid = i.uid, category = "Test Category", title = i.title, knowledgeType = "Standard", scope = "Global",
            description = i.desc, applicability = "When testing.", example = "An example.", limitation = "A limitation.",
            confidence = i.conf, sourceType = "ArchitectureNote", version, lastReviewedUtc = "2026-06-20",
            reviewStatus = "Approved", tags = new[] { "test", "pack" }
        }).ToArray();
        File.WriteAllText(Path.Combine(dir, "a.json"),
            JsonSerializer.Serialize(new { category = "Test Category", items = itemObjs }));
        File.WriteAllText(Path.Combine(dir, "manifest.json"),
            JsonSerializer.Serialize(new
            {
                packUid = packUid.ToString(), name = "Test Pack", version, description = "desc", license = "lic",
                createdUtc = "2026-06-20", lastReviewedUtc = "2026-06-20", itemCount = items.Length,
                files = new[] { "a.json" }, legalLimitations = "none", sourcePolicy = "original", reviewStatus = "Approved"
            }));
        return dir;
    }

    private static void WriteRaw(string dir, string file, string content) => File.WriteAllText(Path.Combine(dir, file), content);

    // ---- 1. manifest validation: missing packUid is rejected with no DB writes ----
    [Fact]
    public async Task Manifest_missing_packUid_is_rejected_and_writes_nothing()
    {
        var db = NewDb();
        var dir = Path.Combine(Path.GetTempPath(), "lafpack_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        WriteRaw(dir, "a.json", JsonSerializer.Serialize(new { category = "C", items = new[] { new { uid = Guid.NewGuid().ToString(), title = "t", description = "d", confidence = 0.8 } } }));
        WriteRaw(dir, "manifest.json", JsonSerializer.Serialize(new { name = "X", version = "1.0.0", files = new[] { "a.json" } }));

        var res = await NewInstaller(db).InstallAsync(dir, "test");
        Assert.False(res.Success);
        Assert.Contains(res.Errors, e => e.Contains("packUid"));
        Assert.Equal(0, await db.KnowledgeItems.CountAsync());
        Assert.Equal(0, await db.KnowledgePacks.CountAsync());
    }

    // ---- 2. item validation: missing title is rejected ----
    [Fact]
    public async Task Item_missing_title_is_rejected()
    {
        var db = NewDb();
        var dir = Path.Combine(Path.GetTempPath(), "lafpack_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        WriteRaw(dir, "a.json", JsonSerializer.Serialize(new { category = "C", items = new[] { new { uid = Guid.NewGuid().ToString(), description = "d", confidence = 0.8 } } }));
        WriteRaw(dir, "manifest.json", JsonSerializer.Serialize(new { packUid = PackA.ToString(), name = "X", version = "1.0.0", files = new[] { "a.json" } }));

        var res = await NewInstaller(db).InstallAsync(dir, "test");
        Assert.False(res.Success);
        Assert.Contains(res.Errors, e => e.Contains("title"));
        Assert.Equal(0, await db.KnowledgeItems.CountAsync());
    }

    // ---- 3. duplicate UID across the pack is rejected ----
    [Fact]
    public async Task Duplicate_uid_is_rejected()
    {
        var db = NewDb();
        var dup = "33333333-3333-4333-8333-333333330099";
        var dir = WritePack(PackA, "1.0.0", (dup, "One", "desc one", 0.8), (dup, "Two", "desc two", 0.8));
        var res = await NewInstaller(db).InstallAsync(dir, "test");
        Assert.False(res.Success);
        Assert.Contains(res.Errors, e => e.Contains("duplicate uid", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(0, await db.KnowledgeItems.CountAsync());
    }

    // ---- 4. confidence out of range is rejected ----
    [Fact]
    public async Task Confidence_out_of_range_is_rejected()
    {
        var db = NewDb();
        var dir = WritePack(PackA, "1.0.0", ("44444444-4444-4444-8444-444444440001", "Bad", "desc", 1.5));
        var res = await NewInstaller(db).InstallAsync(dir, "test");
        Assert.False(res.Success);
        Assert.Contains(res.Errors, e => e.Contains("confidence"));
        Assert.Equal(0, await db.KnowledgeItems.CountAsync());
    }

    // ---- 5. import installs all items, stamped as baseline ----
    [Fact]
    public async Task Import_installs_all_items_as_baseline()
    {
        var db = NewDb();
        var dir = WritePack(PackA, "1.0.0",
            ("aaaa0001-0000-4000-8000-000000000001", "Alpha", "first item", 0.9),
            ("aaaa0002-0000-4000-8000-000000000002", "Beta", "second item", 0.85));
        var res = await NewInstaller(db).InstallAsync(dir, "test");
        Assert.True(res.Success);
        Assert.Equal(2, res.Created);
        Assert.Equal(2, await db.KnowledgeItems.CountAsync(k => k.KnowledgePackId != null));
        var pack = await db.KnowledgePacks.SingleAsync();
        Assert.Equal(PackA, pack.Uid);
        Assert.Equal(2, pack.ItemCount);
        // baseline items are Curated (protected) and globally scoped
        Assert.True(await db.KnowledgeItems.AllAsync(k => k.Tier == PermanenceTier.Curated && k.ProjectId == null));
    }

    // ---- 6 & 7. idempotent reinstall: no duplicates ----
    [Fact]
    public async Task Reinstall_is_idempotent_and_creates_no_duplicates()
    {
        var db = NewDb();
        var dir = WritePack(PackA, "1.0.0", ("aaaa0001-0000-4000-8000-000000000001", "Alpha", "first item", 0.9));
        await NewInstaller(db).InstallAsync(dir, "test");
        var second = await NewInstaller(db).InstallAsync(dir, "test");

        Assert.True(second.Success);
        Assert.Equal(0, second.Created);
        Assert.True(second.AlreadyCurrent || second.Unchanged == 1);
        Assert.Equal(1, await db.KnowledgeItems.CountAsync(k => k.KnowledgePackId != null));
        Assert.Equal(1, await db.KnowledgePacks.CountAsync());
    }

    // ---- 8. baseline is distinguishable from imported project knowledge ----
    [Fact]
    public async Task Baseline_is_distinguishable_from_imported_knowledge()
    {
        var db = NewDb();
        db.KnowledgeItems.Add(new KnowledgeItem { Title = "Imported", Content = "from a project", ProjectId = 5 });
        await db.SaveChangesAsync();
        var dir = WritePack(PackA, "1.0.0", ("aaaa0001-0000-4000-8000-000000000001", "Baseline item", "shipped", 0.9));
        await NewInstaller(db).InstallAsync(dir, "test");

        Assert.Equal(1, await db.KnowledgeItems.CountAsync(k => k.KnowledgePackId != null));   // baseline
        Assert.Equal(1, await db.KnowledgeItems.CountAsync(k => k.KnowledgePackId == null && k.ProjectId != null)); // imported
    }

    // ---- 9. baseline items are keyword-searchable ----
    [Fact]
    public async Task Search_returns_baseline_items()
    {
        var db = NewDb();
        var dir = WritePack(PackA, "1.0.0",
            ("aaaa0001-0000-4000-8000-000000000001", "RBAC model", "Role-based access control assigns permissions to roles.", 0.9),
            ("aaaa0002-0000-4000-8000-000000000002", "Backup and restore", "A backup is unproven until a restore is tested.", 0.9));
        await NewInstaller(db).InstallAsync(dir, "test");

        var hits = await db.KnowledgeItems.Where(k => k.KnowledgePackId != null && (k.Title.Contains("RBAC") || k.Content.Contains("RBAC"))).ToListAsync();
        Assert.Single(hits);
        Assert.Equal("RBAC model", hits[0].Title);
    }

    // ---- 10. install records provenance carrying the pack origin ----
    [Fact]
    public async Task Install_records_provenance_with_pack_origin()
    {
        var db = NewDb();
        var dir = WritePack(PackA, "1.0.0", ("aaaa0001-0000-4000-8000-000000000001", "Alpha", "first", 0.9));
        await NewInstaller(db).InstallAsync(dir, "test-actor");
        Assert.True(await db.ProvenanceEvents.AnyAsync(p => p.OriginPackUid == PackA && p.Method == ProvenanceMethod.Import));
        Assert.True(await db.KnowledgeVersions.AnyAsync(v => v.VersionNumber == 1));
    }

    // ---- 11. pack upgrade on an unedited baseline item updates in place + records a new version ----
    [Fact]
    public async Task Pack_upgrade_updates_unedited_item_and_records_version()
    {
        var db = NewDb();
        var uid = "aaaa0001-0000-4000-8000-000000000001";
        await NewInstaller(db).InstallAsync(WritePack(PackA, "1.0.0", (uid, "Alpha", "original body", 0.9)), "test");
        var v2 = await NewInstaller(db).InstallAsync(WritePack(PackA, "1.1.0", (uid, "Alpha", "revised body for v1.1", 0.92)), "test");

        Assert.True(v2.Success);
        Assert.Equal(1, v2.Updated);
        var item = await db.KnowledgeItems.SingleAsync(k => k.Uid == Guid.Parse(uid));
        Assert.Contains("revised body", item.Content);
        Assert.Equal(2, item.VersionNumber);
        Assert.True(await db.KnowledgeVersions.AnyAsync(ver => ver.KnowledgeItemId == item.Id && ver.VersionNumber == 2));
    }

    // ---- 12. a user-edited baseline item is NOT overwritten — a proposed revision is raised instead ----
    [Fact]
    public async Task User_edited_baseline_item_is_not_overwritten()
    {
        var db = NewDb();
        var uid = "aaaa0001-0000-4000-8000-000000000001";
        await NewInstaller(db).InstallAsync(WritePack(PackA, "1.0.0", (uid, "Alpha", "original body", 0.9)), "test");

        // Simulate a human edit: change content + record a Human provenance event (as the Knowledge UI does).
        var item = await db.KnowledgeItems.SingleAsync(k => k.Uid == Guid.Parse(uid));
        item.Content = "MY LOCAL EDIT — keep this";
        item.ContentHash = new ContentHasher().Compute(item.Content);
        db.ProvenanceEvents.Add(new ProvenanceEvent { KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, Method = ProvenanceMethod.Human, Actor = "alice", Reason = "manual edit" });
        await db.SaveChangesAsync();

        var v2 = await NewInstaller(db).InstallAsync(WritePack(PackA, "1.1.0", (uid, "Alpha", "vendor revised body", 0.92)), "test");
        Assert.True(v2.Success);
        Assert.Equal(1, v2.ProposedRevisions);
        Assert.Equal(0, v2.Updated);

        var after = await db.KnowledgeItems.SingleAsync(k => k.Uid == Guid.Parse(uid));
        Assert.Equal("MY LOCAL EDIT — keep this", after.Content); // not overwritten
        Assert.True(await db.ProposedRevisions.AnyAsync(r => r.OriginalKnowledgeItemId == item.Id && r.Status == KnowledgeStatus.NeedsReview));
    }

    // ---- 13. a malformed pack fails safely (invalid JSON), writing nothing ----
    [Fact]
    public async Task Malformed_pack_fails_safely()
    {
        var db = NewDb();
        var dir = Path.Combine(Path.GetTempPath(), "lafpack_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        WriteRaw(dir, "manifest.json", JsonSerializer.Serialize(new { packUid = PackA.ToString(), name = "X", version = "1.0.0", files = new[] { "a.json" } }));
        WriteRaw(dir, "a.json", "{ this is not valid json ]");

        var res = await NewInstaller(db).InstallAsync(dir, "test");
        Assert.False(res.Success);
        Assert.NotEmpty(res.Errors);
        Assert.Equal(0, await db.KnowledgeItems.CountAsync());
        Assert.Equal(0, await db.KnowledgePacks.CountAsync());
    }

    // ---- 14. Base Knowledge install is Admin-only (server-side); a non-admin is denied and nothing installs ----
    [Fact]
    public async Task Non_admin_cannot_install_pack()
    {
        var db = NewDb();
        var installer = new RecordingInstaller();
        var me = new FakeCurrentUser { User = new UserAccount { WindowsIdentity = "DOM\\analyst", Role = UserRole.Analyst, Enabled = true } };
        var ctrl = new BaseKnowledgeController(db, installer, new ConfigurationBuilder().Build(), new FakeEnv(),
            me, new AccessControlService(db), new AuditTrailService(db))
        { ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() } };

        var result = await ctrl.Install(default);
        Assert.IsType<ViewResult>(result);       // AccessDenied view
        Assert.Equal(403, ctrl.Response.StatusCode);
        Assert.False(installer.Called);          // installer never invoked for a non-admin
    }

    private sealed class FakeCurrentUser : ICurrentUserService
    {
        public UserAccount? User { get; init; }
        public string? IpAddress => "127.0.0.1";
    }

    private sealed class RecordingInstaller : IKnowledgePackInstaller
    {
        public bool Called { get; private set; }
        public Task<KnowledgePackInstallResult> InstallAsync(string packDirectory, string actor, CancellationToken ct = default)
        {
            Called = true;
            return Task.FromResult(new KnowledgePackInstallResult(true, Guid.Empty, "x", "1", 0, 0, 0, 0, 0, false, Array.Empty<string>()));
        }
    }

    private sealed class FakeEnv : Microsoft.AspNetCore.Hosting.IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public string EnvironmentName { get; set; } = "Development";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.GetTempPath();
    }
}
