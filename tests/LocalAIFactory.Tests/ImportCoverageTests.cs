using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Coverage;
using LocalAIFactory.Ingestion.Graph;
using LocalAIFactory.Ingestion.Symbols;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class ImportCoverageTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static ImportCoverageService NewService(AppDbContext db) => new(db, new CodeGraphBuilder(db));

    private static ImportedFile Art(string path, string lang, string? text, bool skipped = false, string? reason = null)
        => new() { ProjectId = 1, RelativePath = path, FileName = System.IO.Path.GetFileName(path),
                   DetectedLanguage = lang, RawText = text, Skipped = skipped, SkipReason = reason, Status = ImportStatus.Processed };

    [Fact]
    public async Task Report_classifies_every_file_into_a_visible_bucket()
    {
        var db = NewDb();
        // 1 supported-with-symbols (C#), 1 supported-no-symbols (C# global usings), 1 supported Python (R2-ACC-CAP3),
        // 1 unsupported (JS), 1 non-code (json), 1 skipped (binary), 1 SQL with symbols.
        db.ImportedFiles.AddRange(
            Art("src/Account.cs", "csharp", "namespace N { public class Account { } }"),
            Art("src/GlobalUsings.cs", "csharp", "global using System;"),
            Art("svc/app.py", "python", "def main():\n    return 1"),
            Art("web/app.js", "javascript", "function f(){ return 1; }"),
            Art("app.json", "json", "{}"),
            Art("logo.png", "binary", null, skipped: true, reason: "binary"),
            Art("db/schema.sql", "sql", "CREATE TABLE dbo.X (Id INT);"));
        await db.SaveChangesAsync();

        // Extract the supported files (C# + Python now) so symbol-presence reflects reality.
        var ids = await db.ImportedFiles.Where(f => f.DetectedLanguage == "csharp" || f.DetectedLanguage == "python").Select(f => f.Id).ToListAsync();
        var cs = new CodeSymbolStore(db, new CodeSymbolExtractorRouter(new ICodeSymbolExtractor[] { new CSharpSymbolExtractor(), new PythonSymbolExtractor() }));
        foreach (var id in ids) await cs.UpsertForArtifactAsync(id);
        var sqlId = await db.ImportedFiles.Where(f => f.DetectedLanguage == "sql").Select(f => f.Id).FirstAsync();
        await new SchemaSymbolStore(db, new SqlSchemaExtractorRouter(new[] { new TSqlSchemaExtractor() })).UpsertForArtifactAsync(sqlId);

        var r = await NewService(db).ComputeAsync(1);

        Assert.Equal(7, r.FilesDiscovered);
        Assert.Equal(6, r.FilesImported);          // binary skipped
        Assert.Equal(3, r.FilesExtracted);         // Account.cs + schema.sql + app.py (Python now supported)
        Assert.Equal(1, r.FilesNoSymbols);         // GlobalUsings.cs
        Assert.Equal(1, r.FilesUnsupported);       // app.js (still unsupported)
        Assert.Equal(1, r.FilesNonCode);           // app.json
        Assert.Equal(1, r.FilesSkipped);           // logo.png
        Assert.Contains("javascript", r.UnsupportedLanguagesJson);
        Assert.DoesNotContain("\"python\"", r.UnsupportedLanguagesJson); // python is supported now
    }

    [Fact]
    public async Task Unsupported_languages_are_never_hidden_as_empty()
    {
        var db = NewDb();
        db.ImportedFiles.Add(Art("web/handler.js", "javascript", "function f(){}")); // still-unsupported language
        await db.SaveChangesAsync();

        var r = await NewService(db).ComputeAsync(1);

        Assert.True(r.HasUnsupported);
        Assert.Equal(1, r.FilesUnsupported);
        Assert.Equal(0, r.FilesNoSymbols); // NOT silently counted as "empty"
    }

    [Fact]
    public async Task Parse_errors_are_surfaced_not_swallowed()
    {
        var db = NewDb();
        var art = Art("src/Broken.cs", "csharp", "namespace N {");
        art.ExtractionStatus = ExtractionStatus.ParseError;
        art.ExtractionNote = "boom";
        db.ImportedFiles.Add(art);
        await db.SaveChangesAsync();

        var r = await NewService(db).ComputeAsync(1);

        Assert.True(r.HasParseErrors);
        Assert.Equal(1, r.FilesParseError);
        Assert.Contains("boom", r.ParseErrorsJson);
    }

    [Fact]
    public async Task Report_is_appended_each_compute_and_latest_is_returned()
    {
        var db = NewDb();
        db.ImportedFiles.Add(Art("src/A.cs", "csharp", "namespace N { class A {} }"));
        await db.SaveChangesAsync();
        var svc = NewService(db);

        var first = await svc.ComputeAsync(1);
        var second = await svc.ComputeAsync(1);

        Assert.Equal(2, await db.ImportCoverageReports.CountAsync()); // append-only
        var latest = await svc.LatestForProjectAsync(1);
        Assert.Equal(second.Id, latest!.Id);
    }

    [Fact]
    public async Task Report_flags_project_scoped_limitation()
    {
        var db = NewDb();
        db.ImportedFiles.Add(Art("src/A.cs", "csharp", "namespace N { class A {} }"));
        await db.SaveChangesAsync();
        var r = await NewService(db).ComputeAsync(1);
        Assert.True(r.ProjectScopedOnly); // honesty: cross-repo/DB not resolved
    }
}
