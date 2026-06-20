using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Symbols;
using LocalAIFactory.Rag.Retrieval;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class CSharpReferenceTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private const string Sample = @"
namespace Shop.Domain
{
    public interface IOrderRepository { }
    public abstract class EntityBase { }
}
namespace Shop.App
{
    public class OrderRepository : Shop.Domain.IOrderRepository { }

    public class OrderService
    {
        private readonly Shop.Domain.IOrderRepository _repo;
        public int Count;
        public OrderService(Shop.Domain.IOrderRepository repo) { _repo = repo; }
    }

    public class Order : Shop.Domain.EntityBase
    {
        public Shop.Domain.IOrderRepository Repo { get; set; }
    }
}";

    private static async Task<AppDbContext> SeededAsync(string code, string path = "src/File.cs", int projectId = 1)
    {
        var db = NewDb();
        var art = new ImportedFile
        {
            ProjectId = projectId, RelativePath = path, FileName = System.IO.Path.GetFileName(path),
            DetectedLanguage = "csharp", RawText = code, Status = ImportStatus.Processed
        };
        db.ImportedFiles.Add(art);
        await db.SaveChangesAsync();
        await new CodeSymbolStore(db, new CodeSymbolExtractorRouter(new[] { new CSharpSymbolExtractor() }))
            .UpsertForArtifactAsync(art.Id);
        await new CodeGraphBuilder(db).RebuildForProjectAsync(projectId);
        return db;
    }

    [Fact]
    public void Extractor_captures_priority1_type_references_and_skips_primitives()
    {
        var refs = new CSharpSymbolExtractor().Extract(Sample).References;

        Assert.Contains(refs, r => r.Kind == CodeReferenceKind.InterfaceImplementation
            && r.FromFullName == "Shop.App.OrderRepository" && r.ReferencedName == "IOrderRepository");
        Assert.Contains(refs, r => r.Kind == CodeReferenceKind.ConstructorParameterType
            && r.FromFullName == "Shop.App.OrderService" && r.ReferencedName == "IOrderRepository");
        Assert.Contains(refs, r => r.Kind == CodeReferenceKind.FieldType
            && r.FromFullName == "Shop.App.OrderService" && r.ReferencedName == "IOrderRepository");
        Assert.Contains(refs, r => r.Kind == CodeReferenceKind.BaseType
            && r.FromFullName == "Shop.App.Order" && r.ReferencedName == "EntityBase");
        Assert.Contains(refs, r => r.Kind == CodeReferenceKind.PropertyType
            && r.FromFullName == "Shop.App.Order" && r.ReferencedName == "IOrderRepository");
        // primitive field type is not emitted
        Assert.DoesNotContain(refs, r => r.ReferencedName == "int");
    }

    [Fact]
    public async Task Graph_resolves_implements_inherits_and_dependson()
    {
        var db = await SeededAsync(Sample);
        var edges = await db.CodeEdges.ToListAsync();
        var sym = await db.CodeSymbols.ToListAsync();
        int Id(string f) => sym.Single(s => s.FullName == f).Id;

        Assert.Contains(edges, e => e.FromSymbolId == Id("Shop.App.OrderRepository")
            && e.ToSymbolId == Id("Shop.Domain.IOrderRepository") && e.RelationType == RelationType.Implements);
        Assert.Contains(edges, e => e.FromSymbolId == Id("Shop.App.Order")
            && e.ToSymbolId == Id("Shop.Domain.EntityBase") && e.RelationType == RelationType.Inherits);
        Assert.Contains(edges, e => e.FromSymbolId == Id("Shop.App.OrderService")
            && e.ToSymbolId == Id("Shop.Domain.IOrderRepository") && e.RelationType == RelationType.DependsOn);
        // deterministic but explicitly not max-confidence (syntax-only simple-name resolution)
        Assert.All(edges, e => Assert.True(e.Confidence >= 0.9 && e.Confidence <= 0.95));
    }

    [Fact]
    public async Task Which_classes_implement_this_interface()
    {
        var db = await SeededAsync(Sample);
        var svc = new StructuralRetrievalService(db);

        var dependents = await svc.DependentsOfAsync(1, "Shop.Domain.IOrderRepository");

        Assert.Contains(dependents, d => d.Symbol.FullName == "Shop.App.OrderRepository"
            && d.RelationType == RelationType.Implements);
    }

    [Fact]
    public async Task What_consumes_this_service()
    {
        var db = await SeededAsync(Sample);
        var svc = new StructuralRetrievalService(db);

        var consumers = await svc.DependentsOfAsync(1, "Shop.Domain.IOrderRepository");
        var names = consumers.Select(c => c.Symbol.FullName).ToHashSet();

        Assert.Contains("Shop.App.OrderService", names); // ctor injection (DependsOn)
        Assert.Contains("Shop.App.Order", names);        // property type (References)
        Assert.Contains("Shop.App.OrderRepository", names); // implementation (Implements)
    }

    [Fact]
    public async Task Ambiguous_simple_name_is_disambiguated_by_namespace()
    {
        const string code = @"
namespace A { public class Customer { } public class UsesA { private A.Customer _c; } }
namespace B { public class Customer { } }";
        var db = await SeededAsync(code);
        var sym = await db.CodeSymbols.ToListAsync();
        var edges = await db.CodeEdges.ToListAsync();

        var aCustomer = sym.Single(s => s.FullName == "A.Customer").Id;
        var usesA = sym.Single(s => s.FullName == "A.UsesA").Id;
        // UsesA's field 'Customer' resolves to A.Customer (same namespace), NOT B.Customer.
        var edge = Assert.Single(edges, e => e.FromSymbolId == usesA);
        Assert.Equal(aCustomer, edge.ToSymbolId);
        Assert.Equal(0.7, edge.Confidence); // medium: disambiguated by namespace
    }

    [Fact]
    public async Task Unresolved_external_types_are_not_fabricated()
    {
        // References a type that is not in the corpus (e.g. a NuGet/BCL type) -> no edge, counted unresolved.
        const string code = "namespace N { public class C { private SomeExternalThing _x; } }";
        var db = await SeededAsync(code);
        var result = await new CodeGraphBuilder(db).RebuildForProjectAsync(1);

        Assert.True(result.Unresolved >= 1);
        Assert.False(await db.CodeSymbols.AnyAsync(s => s.Name == "SomeExternalThing")); // never invented
    }
}
