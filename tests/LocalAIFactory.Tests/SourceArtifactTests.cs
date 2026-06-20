using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Data;
using LocalAIFactory.Data.Backbone;
using LocalAIFactory.Data.Identity;
using LocalAIFactory.Data.Permanence;
using LocalAIFactory.Data.Quality;
using LocalAIFactory.Ingestion.Classification;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LocalAIFactory.Tests;

public class SourceArtifactTests
{
    private static AppDbContext NewDb() =>
        new(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public void Artifact_batch_and_item_are_portable_with_unique_nonempty_uids()
    {
        IPortableEntity artifact = new ImportedFile();
        IPortableEntity batch = new IngestionJob();
        IPortableEntity item = new KnowledgeItem();
        Assert.NotEqual(Guid.Empty, artifact.Uid);
        Assert.NotEqual(Guid.Empty, batch.Uid);
        Assert.NotEqual(Guid.Empty, item.Uid);
        Assert.NotEqual(artifact.Uid, batch.Uid);
    }

    [Theory]
    [InlineData(".cs", "csharp")]
    [InlineData(".sql", "sql")]
    [InlineData(".md", "markdown")]
    [InlineData(".ps1", "powershell")]
    [InlineData(".sln", "solution")]
    [InlineData(".csproj", "xml")]
    public void DetectLanguage_maps_known_extensions(string ext, string lang)
        => Assert.Equal(lang, new FileClassifier().DetectLanguage(ext));

    [Fact]
    public void DetectLanguage_returns_null_for_unknown_extension()
        => Assert.Null(new FileClassifier().DetectLanguage(".xyz"));

    [Fact]
    public async Task ResolveFile_links_derived_provenance_back_to_its_source_artifact()
    {
        var db = NewDb();
        var backbone = new KnowledgeBackboneService(db, new ContentHasher(), new InstanceContext(db));
        var resolver = new IdentityResolver(db, backbone, new KnowledgePermanenceService(db), new ContentHasher(),
            new QualityService(db, new QualityEvaluator()));

        var artifact = new ImportedFile { ProjectId = 1, RelativePath = "src/A.cs", FileName = "A.cs", DetectedLanguage = "csharp" };
        db.ImportedFiles.Add(artifact);
        await db.SaveChangesAsync();

        var res = await resolver.ResolveFileAsync(1, "src/A.cs", "A", "content", SourceType.SourceCode,
            sourceArtifactId: artifact.Id);

        var prov = await db.ProvenanceEvents.SingleAsync(p => p.KnowledgeItemId == res.KnowledgeItemId);
        Assert.Equal(artifact.Id, prov.SourceArtifactId); // every derived item links back to its artifact
    }
}
