using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Symbols;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class CodeSymbolTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static CodeSymbolStore NewStore(AppDbContext db) =>
        new(db, new CodeSymbolExtractorRouter(new[] { new CSharpSymbolExtractor() }));

    private const string Sample = @"
namespace Acme.Banking
{
    public class Account
    {
        public int Id { get; set; }
        private decimal _balance;
        public Account(int id) { Id = id; }
        public void Deposit(decimal amount) { if (amount > 0) _balance += amount; }
        public decimal Withdraw(decimal amount)
        {
            if (amount <= 0 || amount > _balance) return 0;
            _balance -= amount;
            return amount;
        }
    }
    public interface ILedger { void Post(int accountId, decimal amount); }
    public enum AccountKind { Checking, Savings }
}";

    private static async Task<ImportedFile> SeedArtifactAsync(AppDbContext db, string text, string path = "src/Account.cs", int projectId = 1)
    {
        var art = new ImportedFile
        {
            ProjectId = projectId, RelativePath = path, FileName = "Account.cs",
            DetectedLanguage = "csharp", RawText = text, Status = ImportStatus.Processed
        };
        db.ImportedFiles.Add(art);
        await db.SaveChangesAsync();
        return art;
    }

    // ---- Extractor (pure, no DB) ----

    [Fact]
    public void Extractor_finds_namespace_type_and_member_kinds()
    {
        var syms = new CSharpSymbolExtractor().Extract(Sample).Symbols;

        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Namespace && s.FullName == "Acme.Banking");
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Class && s.FullName == "Acme.Banking.Account");
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Interface && s.FullName == "Acme.Banking.ILedger");
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Enum && s.FullName == "Acme.Banking.AccountKind");
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Constructor && s.FullName == "Acme.Banking.Account..ctor");
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Method && s.Name == "Deposit");
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Property && s.Name == "Id");
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Field && s.Name == "_balance");
        // Interface members default to public; private fields are not public.
        Assert.True(syms.Single(s => s.Name == "Deposit").IsPublic);
        Assert.False(syms.Single(s => s.Name == "_balance").IsPublic);
        // Enum members are emitted as fields under the enum.
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Field && s.FullName == "Acme.Banking.AccountKind.Checking");
    }

    [Fact]
    public void Extractor_computes_complexity_from_decision_points()
    {
        var syms = new CSharpSymbolExtractor().Extract(Sample).Symbols;
        // Deposit: base 1 + one `if`.
        Assert.Equal(2, syms.Single(s => s.Name == "Deposit").ComplexitySignal);
        // Withdraw: base 1 + `if` + `||` => 3.
        Assert.Equal(3, syms.Single(s => s.Name == "Withdraw").ComplexitySignal);
    }

    [Fact]
    public void Extractor_distinguishes_overloads_by_signature()
    {
        const string code = "namespace N { class C { void M(int a){} void M(string a){} void M(){} } }";
        var ms = new CSharpSymbolExtractor().Extract(code).Symbols.Where(s => s.Name == "M").ToList();
        Assert.Equal(3, ms.Count);
        Assert.Equal(3, ms.Select(m => m.Signature).Distinct().Count());
    }

    [Fact]
    public void Extractor_does_not_throw_on_malformed_code()
    {
        // Error-tolerant parsing: a truncated file still yields the symbols it does declare.
        var syms = new CSharpSymbolExtractor().Extract("namespace N { public class Broken { public void M( ").Symbols;
        Assert.Contains(syms, s => s.FullName == "N.Broken");
    }

    // ---- Store (convergent upsert) ----

    [Fact]
    public async Task Store_persists_symbols_with_containment_links()
    {
        var db = NewDb();
        var art = await SeedArtifactAsync(db, Sample);
        var n = await NewStore(db).UpsertForArtifactAsync(art.Id);

        Assert.True(n > 0);
        var all = await db.CodeSymbols.ToListAsync();
        var ns = all.Single(s => s.Kind == CodeSymbolKind.Namespace);
        var cls = all.Single(s => s.FullName == "Acme.Banking.Account");
        var deposit = all.Single(s => s.Name == "Deposit");

        Assert.Null(ns.ParentSymbolId);                 // top-level namespace has no parent
        Assert.Equal(ns.Id, cls.ParentSymbolId);        // class -> namespace
        Assert.Equal(cls.Id, deposit.ParentSymbolId);   // method -> class
        Assert.All(all, s => Assert.Equal(art.Id, s.SourceArtifactId));
    }

    [Fact]
    public async Task Reextraction_of_identical_content_is_idempotent_and_keeps_uids()
    {
        var db = NewDb();
        var art = await SeedArtifactAsync(db, Sample);
        var store = NewStore(db);

        await store.UpsertForArtifactAsync(art.Id);
        var before = await db.CodeSymbols.ToDictionaryAsync(s => s.SourceLocusKey, s => s.Uid);

        await store.UpsertForArtifactAsync(art.Id);
        var after = await db.CodeSymbols.ToDictionaryAsync(s => s.SourceLocusKey, s => s.Uid);

        Assert.Equal(before.Count, after.Count);          // no duplicates
        Assert.Equal(before, after);                      // same loci, same Uids
    }

    [Fact]
    public async Task Convergence_keeps_uids_adds_new_and_deletes_removed_symbols()
    {
        var db = NewDb();
        var store = NewStore(db);

        // Initial import.
        var art1 = await SeedArtifactAsync(db, Sample);
        await store.UpsertForArtifactAsync(art1.Id);
        var depositUid = (await db.CodeSymbols.SingleAsync(s => s.Name == "Deposit")).Uid;

        // Re-import the same logical file (same project + path) with Withdraw removed and Freeze added.
        const string changed = @"
namespace Acme.Banking
{
    public class Account
    {
        public int Id { get; set; }
        private decimal _balance;
        public Account(int id) { Id = id; }
        public void Deposit(decimal amount) { if (amount > 0) _balance += amount; }
        public void Freeze() { }
    }
    public interface ILedger { void Post(int accountId, decimal amount); }
    public enum AccountKind { Checking, Savings }
}";
        var art2 = await SeedArtifactAsync(db, changed); // same path => same FileLocusKey
        await store.UpsertForArtifactAsync(art2.Id);

        var symbols = await db.CodeSymbols.ToListAsync();
        // Deposit survived re-extraction with its Uid intact (graph/pack references stay valid).
        Assert.Equal(depositUid, symbols.Single(s => s.Name == "Deposit").Uid);
        // Withdraw was removed; Freeze was added.
        Assert.DoesNotContain(symbols, s => s.Name == "Withdraw");
        Assert.Contains(symbols, s => s.Name == "Freeze");
        // Surviving symbols now point at the latest artifact.
        Assert.Equal(art2.Id, symbols.Single(s => s.Name == "Deposit").SourceArtifactId);
    }

    [Fact]
    public async Task Store_ignores_non_csharp_artifacts()
    {
        var db = NewDb();
        var art = await SeedArtifactAsync(db, "SELECT 1;", "db/init.sql");
        art.DetectedLanguage = "sql";
        await db.SaveChangesAsync();

        var n = await NewStore(db).UpsertForArtifactAsync(art.Id);
        Assert.Equal(0, n);
        Assert.Empty(await db.CodeSymbols.ToListAsync());
    }
}
