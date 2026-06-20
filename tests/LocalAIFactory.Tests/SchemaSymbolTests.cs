using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Symbols;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class SchemaSymbolTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static SchemaSymbolStore NewStore(AppDbContext db) =>
        new(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() }));

    private const string Schema = @"
CREATE TABLE dbo.Customer
(
    CustomerId INT IDENTITY(1,1) NOT NULL,
    Name NVARCHAR(200) NOT NULL,
    CONSTRAINT PK_Customer PRIMARY KEY (CustomerId)
);
GO
CREATE TABLE dbo.Account
(
    AccountId INT IDENTITY(1,1) NOT NULL,
    CustomerId INT NOT NULL,
    Balance DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_Account PRIMARY KEY (AccountId),
    CONSTRAINT FK_Account_Customer FOREIGN KEY (CustomerId) REFERENCES dbo.Customer (CustomerId)
);
GO
CREATE VIEW dbo.vCustomerAccounts AS
    SELECT c.CustomerId, c.Name, a.Balance
    FROM dbo.Customer c
    JOIN dbo.Account a ON a.CustomerId = c.CustomerId;
GO
CREATE PROCEDURE dbo.usp_GetBalance @AccountId INT AS
BEGIN
    IF @AccountId <= 0 RETURN;
    SELECT Balance FROM dbo.Account WHERE AccountId = @AccountId AND Balance > 0;
END
GO
CREATE FUNCTION dbo.ufn_Double (@x INT) RETURNS INT AS BEGIN RETURN @x * 2; END
GO
CREATE TRIGGER dbo.trg_Account_Audit ON dbo.Account AFTER INSERT AS
BEGIN
    INSERT INTO dbo.AuditLog (Msg) SELECT 'ins' FROM inserted;
END
GO";

    private static async Task<ImportedFile> SeedAsync(AppDbContext db, string text, string path = "db/schema.sql", string lang = "sql", int projectId = 1)
    {
        var art = new ImportedFile
        {
            ProjectId = projectId, RelativePath = path, FileName = System.IO.Path.GetFileName(path),
            DetectedLanguage = lang, RawText = text, Status = ImportStatus.Processed
        };
        db.ImportedFiles.Add(art);
        await db.SaveChangesAsync();
        return art;
    }

    // ---- Extractor (pure, no DB) ----

    [Fact]
    public void Extractor_finds_all_object_kinds()
    {
        var r = new TSqlSchemaExtractor().Extract(Schema);
        var s = r.Symbols;

        Assert.Contains(s, x => x.Kind == CodeSymbolKind.Table && x.FullName == "dbo.Customer");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.Table && x.FullName == "dbo.Account");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.Column && x.FullName == "dbo.Account.Balance");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.Constraint && x.Name == "PK_Account");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.ForeignKey && x.Name == "FK_Account_Customer");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.View && x.FullName == "dbo.vCustomerAccounts");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.StoredProcedure && x.FullName == "dbo.usp_GetBalance");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.SqlFunction && x.FullName == "dbo.ufn_Double");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.Trigger && x.FullName == "dbo.trg_Account_Audit");
        Assert.Contains(s, x => x.Kind == CodeSymbolKind.Namespace && x.FullName == "dbo");
    }

    [Fact]
    public void Extractor_computes_procedure_complexity_from_decision_points()
    {
        var r = new TSqlSchemaExtractor().Extract(Schema);
        // usp_GetBalance: base 1 + IF + AND => 3.
        Assert.Equal(3, r.Symbols.Single(x => x.Name == "usp_GetBalance").ComplexitySignal);
    }

    [Fact]
    public void Extractor_captures_references()
    {
        var r = new TSqlSchemaExtractor().Extract(Schema);

        // FK reference: FK_Account_Customer -> dbo.Customer(CustomerId)
        Assert.Contains(r.References, x => x.Kind == CodeReferenceKind.ForeignKeyReference
            && x.ReferencedObject == "Customer" && x.ReferencedColumn == "CustomerId");
        // View references both base tables.
        Assert.Contains(r.References, x => x.Kind == CodeReferenceKind.TableReference
            && x.FromFullName == "dbo.vCustomerAccounts" && x.ReferencedObject == "Account");
        // Proc reads dbo.Account.
        Assert.Contains(r.References, x => x.Kind == CodeReferenceKind.TableReference
            && x.FromFullName == "dbo.usp_GetBalance" && x.ReferencedObject == "Account");
        // Trigger is bound to dbo.Account.
        Assert.Contains(r.References, x => x.Kind == CodeReferenceKind.TriggerTable
            && x.FromFullName == "dbo.trg_Account_Audit" && x.ReferencedObject == "Account");
    }

    [Fact]
    public void Extractor_yields_no_symbols_for_dml_only_script()
    {
        var r = new TSqlSchemaExtractor().Extract("INSERT INTO dbo.Account (AccountId) VALUES (1); UPDATE dbo.Account SET Balance = 0;");
        Assert.Empty(r.Symbols);
    }

    [Fact]
    public void Extractor_does_not_throw_on_malformed_sql()
    {
        // Batch isolation: the broken second batch is dropped; the first still yields its table.
        var r = new TSqlSchemaExtractor().Extract("CREATE TABLE dbo.Good (Id INT);\nGO\nCREATE TABLE dbo.Broken (");
        Assert.Contains(r.Symbols, x => x.FullName == "dbo.Good");
    }

    // ---- Store (object-scoped upsert) ----

    [Fact]
    public async Task Store_persists_symbols_with_containment_and_references()
    {
        var db = NewDb();
        var art = await SeedAsync(db, Schema);
        var n = await NewStore(db).UpsertForArtifactAsync(art.Id);

        Assert.True(n > 0);
        var all = await db.CodeSymbols.ToListAsync();
        var schema = all.Single(s => s.Kind == CodeSymbolKind.Namespace && s.FullName == "dbo");
        var account = all.Single(s => s.FullName == "dbo.Account");
        var balance = all.Single(s => s.FullName == "dbo.Account.Balance");

        Assert.Null(schema.ParentSymbolId);                 // schema is top-level
        Assert.Equal(schema.Id, account.ParentSymbolId);    // table -> schema
        Assert.Equal(account.Id, balance.ParentSymbolId);   // column -> table

        // FK reference staged for KE-010 with canonical join key.
        var fk = await db.CodeSymbolReferences.SingleAsync(r => r.ReferenceKind == CodeReferenceKind.ForeignKeyReference);
        Assert.Equal("customer", fk.ReferencedObject);
        Assert.Equal("customerid", fk.ReferencedColumn);
        Assert.Equal("dbo.customer", fk.ReferencedKey);
    }

    [Fact]
    public async Task Reextraction_is_idempotent_and_keeps_uids()
    {
        var db = NewDb();
        var art = await SeedAsync(db, Schema);
        var store = NewStore(db);

        await store.UpsertForArtifactAsync(art.Id);
        var before = await db.CodeSymbols.ToDictionaryAsync(s => s.SourceLocusKey, s => s.Uid);
        var refsBefore = await db.CodeSymbolReferences.CountAsync();

        await store.UpsertForArtifactAsync(art.Id);
        var after = await db.CodeSymbols.ToDictionaryAsync(s => s.SourceLocusKey, s => s.Uid);

        Assert.Equal(before.Count, after.Count);   // no duplicates
        Assert.Equal(before, after);               // same loci, same Uids
        Assert.Equal(refsBefore, await db.CodeSymbolReferences.CountAsync()); // references not duplicated
    }

    [Fact]
    public async Task Create_then_alter_in_separate_files_converge_on_one_object()
    {
        var db = NewDb();
        var store = NewStore(db);

        // CREATE in one file.
        var f1 = await SeedAsync(db, "CREATE TABLE dbo.Account (AccountId INT NOT NULL);", "db/01_create.sql");
        await store.UpsertForArtifactAsync(f1.Id);
        var accountUid = (await db.CodeSymbols.SingleAsync(s => s.FullName == "dbo.Account")).Uid;

        // ALTER ADD COLUMN in a different file — object-scoped identity must converge.
        var f2 = await SeedAsync(db, "ALTER TABLE dbo.Account ADD Nickname NVARCHAR(50) NULL;", "db/02_alter.sql");
        await store.UpsertForArtifactAsync(f2.Id);

        var symbols = await db.CodeSymbols.ToListAsync();
        // Same logical table, same Uid (not duplicated by the second file).
        Assert.Single(symbols.Where(s => s.FullName == "dbo.Account"));
        Assert.Equal(accountUid, symbols.Single(s => s.FullName == "dbo.Account").Uid);
        // The altered-in column now exists and is parented to the same table.
        var nickname = symbols.Single(s => s.FullName == "dbo.Account.Nickname");
        Assert.Equal(symbols.Single(s => s.FullName == "dbo.Account").Id, nickname.ParentSymbolId);
    }

    [Fact]
    public async Task Store_ignores_non_sql_artifacts()
    {
        var db = NewDb();
        var art = await SeedAsync(db, "public class C {}", "src/C.cs", lang: "csharp");
        var n = await NewStore(db).UpsertForArtifactAsync(art.Id);
        Assert.Equal(0, n);
        Assert.Empty(await db.CodeSymbols.ToListAsync());
    }

    [Fact]
    public async Task Dml_only_script_stores_no_symbols()
    {
        var db = NewDb();
        var art = await SeedAsync(db, "INSERT INTO dbo.Account (AccountId) VALUES (1);", "db/seed.sql");
        var n = await NewStore(db).UpsertForArtifactAsync(art.Id);
        Assert.Equal(0, n);
        Assert.Empty(await db.CodeSymbols.ToListAsync());
    }
}
