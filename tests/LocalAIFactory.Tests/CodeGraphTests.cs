using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Symbols;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class CodeGraphTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static SchemaSymbolStore NewSchemaStore(AppDbContext db) =>
        new(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() }));

    private const string Schema = @"
CREATE TABLE dbo.Customer ( CustomerId INT NOT NULL, Name NVARCHAR(200) NOT NULL,
    CONSTRAINT PK_Customer PRIMARY KEY (CustomerId) );
GO
CREATE TABLE dbo.Account ( AccountId INT NOT NULL, CustomerId INT NOT NULL, Balance DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_Account PRIMARY KEY (AccountId),
    CONSTRAINT FK_Account_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (CustomerId) );
GO
CREATE PROCEDURE dbo.usp_GetBalance @AccountId INT AS
BEGIN
    SELECT Balance FROM dbo.Account WHERE AccountId = @AccountId;
    INSERT INTO dbo.AuditLog (Msg) VALUES ('read');
END
GO";

    private static async Task<ImportedFile> SeedAsync(AppDbContext db, string text, string path = "db/schema.sql", int projectId = 1)
    {
        var art = new ImportedFile
        {
            ProjectId = projectId, RelativePath = path, FileName = "schema.sql",
            DetectedLanguage = "sql", RawText = text, Status = ImportStatus.Processed
        };
        db.ImportedFiles.Add(art);
        await db.SaveChangesAsync();
        return art;
    }

    [Fact]
    public async Task Resolves_references_into_structural_edges()
    {
        var db = NewDb();
        var art = await SeedAsync(db, Schema);
        await NewSchemaStore(db).UpsertForArtifactAsync(art.Id);

        var result = await new CodeGraphBuilder(db).RebuildForProjectAsync(1);

        Assert.True(result.Edges > 0);
        var edges = await db.CodeEdges.ToListAsync();
        var symbols = await db.CodeSymbols.ToListAsync();
        int Id(string full) => symbols.Single(s => s.FullName == full).Id;

        // FK_Account_Customer references the Customer table.
        Assert.Contains(edges, e => e.FromSymbolId == Id("dbo.Account.FK_Account_Customer")
            && e.ToSymbolId == Id("dbo.Customer") && e.RelationType == RelationType.References);
        // Procedure reads the Account table.
        Assert.Contains(edges, e => e.FromSymbolId == Id("dbo.usp_GetBalance")
            && e.ToSymbolId == Id("dbo.Account") && e.RelationType == RelationType.References);
    }

    [Fact]
    public async Task Unresolved_references_are_counted_not_fabricated()
    {
        var db = NewDb();
        var art = await SeedAsync(db, Schema);
        await NewSchemaStore(db).UpsertForArtifactAsync(art.Id);

        var result = await new CodeGraphBuilder(db).RebuildForProjectAsync(1);

        // dbo.AuditLog is referenced by the proc but never defined in the corpus → unresolved, no edge.
        Assert.True(result.Unresolved >= 1);
        var symbols = await db.CodeSymbols.ToListAsync();
        Assert.DoesNotContain(symbols, s => s.FullName == "dbo.AuditLog"); // never invented
        var edges = await db.CodeEdges.ToListAsync();
        Assert.All(edges, e => Assert.Contains(symbols, s => s.Id == e.ToSymbolId)); // every edge has a real target
    }

    [Fact]
    public async Task Rebuild_is_idempotent_and_keeps_edge_uids()
    {
        var db = NewDb();
        var art = await SeedAsync(db, Schema);
        await NewSchemaStore(db).UpsertForArtifactAsync(art.Id);
        var builder = new CodeGraphBuilder(db);

        await builder.RebuildForProjectAsync(1);
        var before = await db.CodeEdges.ToDictionaryAsync(e => e.EdgeKey, e => e.Uid);

        await builder.RebuildForProjectAsync(1);
        var after = await db.CodeEdges.ToDictionaryAsync(e => e.EdgeKey, e => e.Uid);

        Assert.Equal(before.Count, after.Count); // no duplicate edges
        Assert.Equal(before, after);             // same edge keys, same Uids (Knowledge Pack stable)
    }

    [Fact]
    public async Task Incremental_artifact_rebuild_matches_project_rebuild()
    {
        var db = NewDb();
        var art = await SeedAsync(db, Schema);
        await NewSchemaStore(db).UpsertForArtifactAsync(art.Id);

        var incremental = await new CodeGraphBuilder(db).RebuildForArtifactAsync(art.Id);
        Assert.True(incremental.Edges > 0);
        var keys = await db.CodeEdges.Select(e => e.EdgeKey).OrderBy(k => k).ToListAsync();

        // A full project rebuild over the same single-artifact corpus converges on the same edge set.
        await new CodeGraphBuilder(db).RebuildForProjectAsync(1);
        var keys2 = await db.CodeEdges.Select(e => e.EdgeKey).OrderBy(k => k).ToListAsync();
        Assert.Equal(keys, keys2);
    }

    [Fact]
    public async Task Exec_reference_maps_to_dependson()
    {
        var db = NewDb();
        const string sql = @"
CREATE PROCEDURE dbo.usp_Child AS BEGIN SELECT 1; END
GO
CREATE PROCEDURE dbo.usp_Parent AS BEGIN EXEC dbo.usp_Child; END
GO";
        var art = await SeedAsync(db, sql);
        await NewSchemaStore(db).UpsertForArtifactAsync(art.Id);

        await new CodeGraphBuilder(db).RebuildForProjectAsync(1);
        var edges = await db.CodeEdges.ToListAsync();
        var symbols = await db.CodeSymbols.ToListAsync();
        int Id(string f) => symbols.Single(s => s.FullName == f).Id;

        Assert.Contains(edges, e => e.FromSymbolId == Id("dbo.usp_Parent")
            && e.ToSymbolId == Id("dbo.usp_Child") && e.RelationType == RelationType.DependsOn);
    }
}
