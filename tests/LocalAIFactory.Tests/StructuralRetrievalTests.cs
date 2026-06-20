using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Symbols;
using LocalAIFactory.Rag.Retrieval;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class StructuralRetrievalTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    // Customer <- FK (Account) ; Account <- proc. Lets us prove transitive blast radius from Customer.
    private const string Schema = @"
CREATE TABLE dbo.Customer ( CustomerId INT NOT NULL, CONSTRAINT PK_Customer PRIMARY KEY (CustomerId) );
GO
CREATE TABLE dbo.Account ( AccountId INT NOT NULL, CustomerId INT NOT NULL,
    CONSTRAINT PK_Account PRIMARY KEY (AccountId),
    CONSTRAINT FK_Account_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (CustomerId) );
GO
CREATE PROCEDURE dbo.usp_GetBalance @AccountId INT AS BEGIN SELECT 1 FROM dbo.Account WHERE AccountId=@AccountId; END
GO";

    private static async Task<AppDbContext> SeededAsync()
    {
        var db = NewDb();
        var art = new ImportedFile
        {
            ProjectId = 1, RelativePath = "db/schema.sql", FileName = "schema.sql",
            DetectedLanguage = "sql", RawText = Schema, Status = ImportStatus.Processed
        };
        db.ImportedFiles.Add(art);
        await db.SaveChangesAsync();
        await new SchemaSymbolStore(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() }))
            .UpsertForArtifactAsync(art.Id);
        await new CodeGraphBuilder(db).RebuildForProjectAsync(1);
        return db;
    }

    [Fact]
    public async Task FindByIdentifier_returns_exact_hit_with_citation()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        var hits = await svc.FindByIdentifierAsync(1, "dbo.Customer");

        var customer = Assert.Single(hits, h => h.FullName == "dbo.Customer");
        Assert.Equal(CodeSymbolKind.Table, customer.Kind);
        Assert.Equal("db/schema.sql", customer.ArtifactPath); // cited provenance
    }

    [Fact]
    public async Task DependentsOf_table_answers_what_references_it()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        var dependents = await svc.DependentsOfAsync(1, "dbo.Customer");

        // The FK from Account references Customer.
        Assert.Contains(dependents, d => d.Symbol.FullName == "dbo.Account.FK_Account_Customer"
            && d.RelationType == RelationType.References && d.Direction == "incoming");
    }

    [Fact]
    public async Task DependenciesOf_procedure_answers_what_it_touches()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        var deps = await svc.DependenciesOfAsync(1, "dbo.usp_GetBalance");

        Assert.Contains(deps, d => d.Symbol.FullName == "dbo.Account" && d.EdgeSource == "reference");
        Assert.Contains(deps, d => d.EdgeSource == "containment"); // its schema (PartOf)
    }

    [Fact]
    public async Task ImpactOf_table_returns_direct_and_transitive_blast_radius()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        var impact = await svc.ImpactOfAsync(1, "dbo.Customer");

        Assert.NotNull(impact);
        Assert.Equal("dbo.Customer", impact!.Target.FullName);
        // Direct: the FK that references Customer.
        Assert.Contains(impact.Direct, n => n.Symbol.FullName == "dbo.Account.FK_Account_Customer");
        // Transitive: the procedure that reads Account (reached via FK -> Account -> proc).
        Assert.Contains(impact.Transitive, n => n.Symbol.FullName == "dbo.usp_GetBalance");
    }

    [Fact]
    public async Task Unknown_identifier_returns_empty_not_error()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        Assert.Empty(await svc.FindByIdentifierAsync(1, "dbo.DoesNotExist"));
        Assert.Empty(await svc.DependentsOfAsync(1, "dbo.DoesNotExist"));
        Assert.Null(await svc.ImpactOfAsync(1, "dbo.DoesNotExist"));
    }

    [Fact]
    public async Task Each_query_logs_a_capture_only_retrieval_event()
    {
        var db = await SeededAsync();
        var svc = new StructuralRetrievalService(db);

        await svc.FindByIdentifierAsync(1, "dbo.Customer");
        await svc.DependentsOfAsync(1, "dbo.Customer");
        await svc.ImpactOfAsync(1, "dbo.Customer");

        var events = await db.RetrievalEvents.ToListAsync();
        Assert.Equal(3, events.Count);
        Assert.Contains(events, e => e.Mode == "lexical");
        Assert.Contains(events, e => e.Mode == "dependents");
        Assert.Contains(events, e => e.Mode == "impact");
    }
}
