using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Maintenance;
using LocalAIFactory.Ingestion.Symbols;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class ConsolidationTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static StructuralConsolidationService NewConsolidation(AppDbContext db) =>
        new(db,
            new CodeSymbolStore(db, new CodeSymbolExtractorRouter(new[] { new CSharpSymbolExtractor() })),
            new SchemaSymbolStore(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() })),
            new CodeGraphBuilder(db));

    private static SchemaSymbolStore NewSchema(AppDbContext db) =>
        new(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() }));

    private static async Task<ImportedFile> SeedAsync(AppDbContext db, string text, string path, int projectId = 1)
    {
        var art = new ImportedFile
        {
            ProjectId = projectId, RelativePath = path, FileName = System.IO.Path.GetFileName(path),
            DetectedLanguage = "sql", RawText = text, Status = ImportStatus.Processed
        };
        db.ImportedFiles.Add(art);
        await db.SaveChangesAsync();
        return art;
    }

    private const string XandY = @"
CREATE TABLE dbo.X ( Id INT NOT NULL, CONSTRAINT PK_X PRIMARY KEY (Id) );
GO
CREATE TABLE dbo.Y ( Id INT NOT NULL, XId INT NOT NULL,
    CONSTRAINT PK_Y PRIMARY KEY (Id),
    CONSTRAINT FK_Y_X FOREIGN KEY (XId) REFERENCES dbo.X (Id) );
GO";

    [Fact]
    public async Task Consolidation_is_idempotent_and_preserves_uids()
    {
        var db = NewDb();
        var art = await SeedAsync(db, XandY, "db/schema.sql");
        await NewSchema(db).UpsertForArtifactAsync(art.Id);
        await new CodeGraphBuilder(db).RebuildForProjectAsync(1);

        var symBefore = await db.CodeSymbols.ToDictionaryAsync(s => s.SourceLocusKey, s => s.Uid);
        var edgeBefore = await db.CodeEdges.ToDictionaryAsync(e => e.EdgeKey, e => e.Uid);

        var r1 = await NewConsolidation(db).ConsolidateProjectAsync(1);
        var r2 = await NewConsolidation(db).ConsolidateProjectAsync(1);

        var symAfter = await db.CodeSymbols.ToDictionaryAsync(s => s.SourceLocusKey, s => s.Uid);
        var edgeAfter = await db.CodeEdges.ToDictionaryAsync(e => e.EdgeKey, e => e.Uid);

        Assert.Equal(symBefore, symAfter);   // no duplicate symbols; same Uids
        Assert.Equal(edgeBefore, edgeAfter); // no duplicate edges; same Uids
        Assert.Equal(0, r1.OrphanSymbolsRemoved);
        Assert.Equal(0, r2.OrphanSymbolsRemoved); // second pass is a true no-op
    }

    [Fact]
    public async Task Deleted_artifact_orphans_are_pruned_and_survivors_keep_uids()
    {
        var db = NewDb();

        // Initial import: X and Y (Y has an FK to X).
        var a = await SeedAsync(db, XandY, "db/schema.sql");
        await NewSchema(db).UpsertForArtifactAsync(a.Id);
        await new CodeGraphBuilder(db).RebuildForProjectAsync(1);
        var yUid = (await db.CodeSymbols.SingleAsync(s => s.FullName == "dbo.Y")).Uid;
        Assert.Contains(await db.CodeSymbols.ToListAsync(), s => s.FullName == "dbo.X");
        Assert.True(await db.CodeEdges.AnyAsync()); // FK_Y_X -> X edge exists

        // The file is re-imported without X and without the FK: a new live artifact supersedes the old one.
        a.Skipped = true; // old artifact superseded
        var b = await SeedAsync(db, "CREATE TABLE dbo.Y ( Id INT NOT NULL, CONSTRAINT PK_Y PRIMARY KEY (Id) );", "db/schema.sql");
        await db.SaveChangesAsync();

        var result = await NewConsolidation(db).ConsolidateProjectAsync(1);

        var symbols = await db.CodeSymbols.ToListAsync();
        Assert.DoesNotContain(symbols, s => s.FullName == "dbo.X");            // orphan table pruned
        Assert.DoesNotContain(symbols, s => s.Name == "FK_Y_X");              // orphan FK pruned
        Assert.Equal(yUid, symbols.Single(s => s.FullName == "dbo.Y").Uid);   // survivor keeps its Uid
        Assert.False(await db.CodeEdges.AnyAsync());                          // edge to removed X is gone
        Assert.True(result.OrphanSymbolsRemoved >= 2);
        Assert.True(result.OrphanEdgesRemoved >= 1);
    }

    [Fact]
    public async Task Consolidation_converges_to_same_state_as_fresh_extraction()
    {
        // Build the graph via consolidation...
        var db1 = NewDb();
        var a1 = await SeedAsync(db1, XandY, "db/schema.sql");
        await NewSchema(db1).UpsertForArtifactAsync(a1.Id);
        await NewConsolidation(db1).ConsolidateProjectAsync(1);
        var consolidated = await db1.CodeEdges.Select(e => e.EdgeKey).OrderBy(k => k).ToListAsync();

        // ...vs a plain fresh extraction + graph build. The structural edge set is identical.
        var db2 = NewDb();
        var a2 = await SeedAsync(db2, XandY, "db/schema.sql");
        await NewSchema(db2).UpsertForArtifactAsync(a2.Id);
        await new CodeGraphBuilder(db2).RebuildForProjectAsync(1);
        var fresh = await db2.CodeEdges.Select(e => e.EdgeKey).OrderBy(k => k).ToListAsync();

        Assert.Equal(fresh, consolidated);
    }

    [Fact]
    public async Task Consolidation_does_not_touch_curated_knowledge()
    {
        var db = NewDb();
        var a = await SeedAsync(db, XandY, "db/schema.sql");
        await NewSchema(db).UpsertForArtifactAsync(a.Id);

        // A curated knowledge node (propose-never-overwrite must leave it alone).
        db.KnowledgeEntities.Add(new KnowledgeEntity
        {
            ProjectId = 1, Name = "ApprovedRule", EntityType = EntityType.Other, Status = KnowledgeStatus.Approved
        });
        await db.SaveChangesAsync();
        var curatedUid = (await db.KnowledgeEntities.SingleAsync(e => e.Name == "ApprovedRule")).Uid;

        await NewConsolidation(db).ConsolidateProjectAsync(1);

        var curated = await db.KnowledgeEntities.SingleAsync(e => e.Name == "ApprovedRule");
        Assert.Equal(curatedUid, curated.Uid);                 // untouched
        Assert.Equal(KnowledgeStatus.Approved, curated.Status); // still approved, not overwritten
    }
}
