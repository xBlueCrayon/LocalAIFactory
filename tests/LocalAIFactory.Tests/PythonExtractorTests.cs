using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Symbols;
using LocalAIFactory.Rag.Retrieval;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-CAP3: deterministic Python structural extractor (pure C#, no Python runtime). Classes, functions,
// async, FastAPI routes, and the Python↔SQL bridge via SQL strings.
public class PythonExtractorTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private const string Py = @"
import os
from fastapi import FastAPI

app = FastAPI()

class OrderService:
    def __init__(self, db):
        self.db = db

    async def get_orders(self):
        rows = self.db.execute(""SELECT id, total FROM dbo.Orders WHERE id > 0"")
        return rows

@app.get(""/health"")
def health():
    return {""status"": ""ok""}

def run_report():
    sql = ""EXEC dbo.usp_GetCustomerOrders""
    return sql
";

    // ---- 1. extracts classes, methods, functions, async ----
    [Fact]
    public void Extracts_classes_functions_and_async()
    {
        var syms = new PythonSymbolExtractor().Extract(Py).Symbols;
        Assert.Contains(syms, s => s.Kind == CodeSymbolKind.Class && s.FullName == "OrderService");
        Assert.Contains(syms, s => s.FullName == "OrderService.__init__");
        var getOrders = Assert.Single(syms, s => s.FullName == "OrderService.get_orders");
        Assert.StartsWith("async", getOrders.Signature);                 // async captured
        Assert.Equal("OrderService", getOrders.ParentFullName);          // method of the class
        Assert.Contains(syms, s => s.FullName == "run_report");          // top-level function
    }

    // ---- 2. FastAPI route decorator captured into the signature ----
    [Fact]
    public void Captures_fastapi_route()
    {
        var syms = new PythonSymbolExtractor().Extract(Py).Symbols;
        var health = Assert.Single(syms, s => s.FullName == "health");
        Assert.Contains("GET /health", health.Signature);
    }

    // ---- 3. SQL string hints -> SqlObjectAccess references (Python↔SQL bridge) ----
    [Fact]
    public void Detects_sql_in_python_strings()
    {
        var refs = new PythonSymbolExtractor().Extract(Py).References;
        Assert.Contains(refs, r => r.Kind == CodeReferenceKind.SqlObjectAccess
            && r.FromFullName == "OrderService.get_orders" && r.ReferencedName == "dbo.orders");
        Assert.Contains(refs, r => r.Kind == CodeReferenceKind.SqlObjectAccess
            && r.FromFullName == "run_report" && r.ReferencedName == "dbo.usp_getcustomerorders");
    }

    // ---- 4. end-to-end Python↔SQL bridge: edge + reverse blast radius ----
    [Fact]
    public async Task Bridge_links_python_to_sql()
    {
        var db = NewDb();
        var sql = new ImportedFile { ProjectId = 1, RelativePath = "db/schema.sql", FileName = "schema.sql", DetectedLanguage = "sql", RawText = "CREATE TABLE dbo.Orders (Id INT, Total DECIMAL(18,2));", Status = ImportStatus.Processed };
        var py = new ImportedFile { ProjectId = 1, RelativePath = "svc/orders.py", FileName = "orders.py", DetectedLanguage = "python", RawText = Py, Status = ImportStatus.Processed };
        db.ImportedFiles.AddRange(sql, py);
        await db.SaveChangesAsync();

        await new SchemaSymbolStore(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() })).UpsertForArtifactAsync(sql.Id);
        await new CodeSymbolStore(db, new CodeSymbolExtractorRouter(new ICodeSymbolExtractor[] { new CSharpSymbolExtractor(), new PythonSymbolExtractor() })).UpsertForArtifactAsync(py.Id);
        await new CodeGraphBuilder(db).RebuildForProjectAsync(1);

        var sym = await db.CodeSymbols.ToListAsync();
        int Id(string f) => sym.Single(s => s.FullName == f).Id;
        var edges = await db.CodeEdges.ToListAsync();
        Assert.Contains(edges, e => e.RelationType == RelationType.AccessesSql
            && e.FromSymbolId == Id("OrderService.get_orders") && e.ToSymbolId == Id("dbo.Orders"));

        var dependents = await new StructuralRetrievalService(db).DependentsOfAsync(1, "dbo.Orders");
        Assert.Contains(dependents, d => d.Symbol.FullName == "OrderService.get_orders" && d.RelationType == RelationType.AccessesSql);
    }

    // ---- 5. imports and malformed lines never break extraction ----
    [Fact]
    public void Imports_and_broken_lines_do_not_break_extraction()
    {
        const string code = "import sys\nfrom a.b import c\n\nclass Keep:\n    def ok(self):\n        return 1\n    def broken(self  # missing paren\n";
        var ex = Record.Exception(() => new PythonSymbolExtractor().Extract(code));
        Assert.Null(ex);
        var syms = new PythonSymbolExtractor().Extract(code).Symbols;
        Assert.Contains(syms, s => s.FullName == "Keep");
        Assert.Contains(syms, s => s.FullName == "Keep.ok");
    }

    // ---- 6. empty/whitespace returns empty, not a crash ----
    [Fact]
    public void Empty_returns_empty()
    {
        Assert.Empty(new PythonSymbolExtractor().Extract("   \n  \n").Symbols);
    }
}
