using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Symbols;
using LocalAIFactory.Rag.Retrieval;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-CAP1: C#↔SQL bridge. Deterministic detection of SQL objects named in C# string literals, resolved
// across languages into AccessesSql edges so impact flows both ways (C#→SQL and SQL→C#), with honest
// confidence + evidence and no fabrication of unresolved names.
public class CSharpSqlBridgeTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private const string Sql = @"
CREATE TABLE dbo.Orders (Id INT NOT NULL, CustomerId INT NOT NULL);
GO
CREATE PROCEDURE dbo.usp_GetOrders AS SELECT Id, CustomerId FROM dbo.Orders;
GO";

    private const string CSharp = @"
namespace Shop.Data
{
    public class OrderRepository
    {
        public void GetAll()
        {
            var sql = ""SELECT Id, CustomerId FROM dbo.Orders WHERE CustomerId > 0"";
            Db.FromSqlRaw(sql);
        }
        public void RunProc()
        {
            Db.ExecuteSqlRaw(""EXEC dbo.usp_GetOrders"");
        }
        public void Ghost()
        {
            var q = ""SELECT * FROM dbo.GhostTable"";  // no such table in the corpus
        }
    }
}";

    private static async Task<AppDbContext> SeededAsync(int projectId = 1)
    {
        var db = NewDb();
        var sqlArt = new ImportedFile { ProjectId = projectId, RelativePath = "db/schema.sql", FileName = "schema.sql", DetectedLanguage = "sql", RawText = Sql, Status = ImportStatus.Processed };
        var csArt = new ImportedFile { ProjectId = projectId, RelativePath = "src/OrderRepository.cs", FileName = "OrderRepository.cs", DetectedLanguage = "csharp", RawText = CSharp, Status = ImportStatus.Processed };
        db.ImportedFiles.AddRange(sqlArt, csArt);
        await db.SaveChangesAsync();

        await new SchemaSymbolStore(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() })).UpsertForArtifactAsync(sqlArt.Id);
        await new CodeSymbolStore(db, new CodeSymbolExtractorRouter(new[] { new CSharpSymbolExtractor() })).UpsertForArtifactAsync(csArt.Id);
        await new CodeGraphBuilder(db).RebuildForProjectAsync(projectId);
        return db;
    }

    // ---- 1. the extractor detects SQL objects in C# string literals, with confidence + evidence ----
    [Fact]
    public void Extractor_detects_table_and_proc_in_sql_strings()
    {
        var refs = new CSharpSymbolExtractor().Extract(CSharp).References;

        var table = Assert.Single(refs, r => r.Kind == CodeReferenceKind.SqlObjectAccess && r.ReferencedName == "dbo.orders");
        Assert.Equal("Shop.Data.OrderRepository.GetAll", table.FromFullName);
        Assert.True(table.Confidence is >= 0.7 and < 1.0);          // syntactic, not max-confidence
        Assert.False(string.IsNullOrWhiteSpace(table.Evidence));    // evidence snippet captured
        Assert.Contains("Orders", table.Evidence);

        var proc = Assert.Single(refs, r => r.Kind == CodeReferenceKind.SqlObjectAccess && r.ReferencedName == "dbo.usp_getorders");
        Assert.Equal("Shop.Data.OrderRepository.RunProc", proc.FromFullName);
        Assert.True(proc.Confidence >= 0.85);                       // EXEC of a proc -> higher confidence
    }

    // ---- 2. C# → SQL bridge edge: method accesses the SQL table, with evidence + sub-1.0 confidence ----
    [Fact]
    public async Task Bridge_links_csharp_method_to_sql_table()
    {
        var db = await SeededAsync();
        var sym = await db.CodeSymbols.ToListAsync();
        int Id(string f) => sym.Single(s => s.FullName == f).Id;
        var edges = await db.CodeEdges.ToListAsync();

        var edge = Assert.Single(edges, e =>
            e.RelationType == RelationType.AccessesSql &&
            e.FromSymbolId == Id("Shop.Data.OrderRepository.GetAll") &&
            e.ToSymbolId == Id("dbo.Orders"));
        Assert.True(edge.Confidence is >= 0.7 and < 1.0);
        Assert.False(string.IsNullOrWhiteSpace(edge.Evidence));

        // EXEC -> the stored procedure
        Assert.Contains(edges, e =>
            e.RelationType == RelationType.AccessesSql &&
            e.FromSymbolId == Id("Shop.Data.OrderRepository.RunProc") &&
            e.ToSymbolId == Id("dbo.usp_GetOrders"));
    }

    // ---- 3. SQL → C# blast radius: who in C# touches this table/proc ----
    [Fact]
    public async Task Reverse_blast_radius_sql_to_csharp()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        var tableDependents = await svc.DependentsOfAsync(1, "dbo.Orders");
        Assert.Contains(tableDependents, d => d.Symbol.FullName == "Shop.Data.OrderRepository.GetAll"
            && d.RelationType == RelationType.AccessesSql && d.Evidence != null);

        var procDependents = await svc.DependentsOfAsync(1, "dbo.usp_GetOrders");
        Assert.Contains(procDependents, d => d.Symbol.FullName == "Shop.Data.OrderRepository.RunProc");

        // impact analysis surfaces the C# caller too
        var impact = await svc.ImpactOfAsync(1, "dbo.Orders");
        Assert.NotNull(impact);
        Assert.Contains(impact!.Direct.Concat(impact.Transitive), n => n.Symbol.FullName == "Shop.Data.OrderRepository.GetAll");
    }

    // ---- 4. C# → SQL dependencies: what SQL a C# method touches ----
    [Fact]
    public async Task Forward_dependencies_csharp_to_sql()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        var deps = await svc.DependenciesOfAsync(1, "Shop.Data.OrderRepository.GetAll");
        Assert.Contains(deps, d => d.Symbol.FullName == "dbo.Orders" && d.RelationType == RelationType.AccessesSql);
    }

    // ---- 5. honesty: a SQL name with no matching symbol is NOT fabricated into an edge ----
    [Fact]
    public async Task Unresolved_sql_name_is_not_fabricated()
    {
        var db = await SeededAsync();
        // GhostTable referenced in C# but never defined -> no symbol, no AccessesSql edge to it.
        Assert.False(await db.CodeSymbols.AnyAsync(s => s.NormalizedKey == "dbo.ghosttable"));
        var ghostEdges = await db.CodeEdges.Where(e => e.RelationType == RelationType.AccessesSql).ToListAsync();
        var sym = await db.CodeSymbols.ToListAsync();
        // every AccessesSql edge points at a real SQL object (table/view/proc/function)
        Assert.All(ghostEdges, e =>
        {
            var to = sym.Single(s => s.Id == e.ToSymbolId);
            Assert.Contains(to.Kind, new[] { CodeSymbolKind.Table, CodeSymbolKind.View, CodeSymbolKind.StoredProcedure, CodeSymbolKind.SqlFunction });
        });
    }
}
