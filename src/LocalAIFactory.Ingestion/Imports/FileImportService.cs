using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Ingestion.Imports;

// Imports a single uploaded file as a knowledge item (chunked, embedded, NeedsReview).
public sealed class FileImportService : IFileImportService
{
    private readonly AppDbContext _db;
    private readonly IFileClassifier _classifier;
    private readonly IChunkingService _chunking;
    private readonly IKnowledgeIndexer _indexer;
    private readonly RagOptions _rag;

    public FileImportService(
        AppDbContext db, IFileClassifier classifier, IChunkingService chunking,
        IKnowledgeIndexer indexer, IOptions<RagOptions> rag)
    {
        _db = db; _classifier = classifier; _chunking = chunking; _indexer = indexer; _rag = rag.Value;
    }

    public async Task<ImportedFile> ImportAsync(int? projectId, string fileName, byte[] content, CancellationToken ct = default)
    {
        var text = Encoding.UTF8.GetString(content);
        var fileClass = _classifier.Classify(fileName);
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var textual = _classifier.IsTextual(fileClass, ext);

        var imported = new ImportedFile
        {
            ProjectId = projectId,
            FileName = Path.GetFileName(fileName),
            RelativePath = fileName,
            Extension = string.IsNullOrEmpty(ext) ? "" : ext,
            FileClass = fileClass,
            SizeBytes = content.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(content)),
            Status = ImportStatus.Processed
        };

        if (!textual)
        {
            imported.Skipped = true;
            imported.SkipReason = "binary";
            _db.ImportedFiles.Add(imported);
            await _db.SaveChangesAsync(ct);
            return imported;
        }

        imported.RawText = text;
        var ki = new KnowledgeItem
        {
            ProjectId = projectId,
            Title = Path.GetFileName(fileName),
            Content = text,
            SourceType = _classifier.ToSourceType(fileClass),
            Status = KnowledgeStatus.NeedsReview,
            Confidence = 0.5,
            Tier = PermanenceTier.Derived // machine-extracted, regenerable.
        };
        _db.KnowledgeItems.Add(ki);
        await _db.SaveChangesAsync(ct);
        imported.KnowledgeItemId = ki.Id;
        _db.ImportedFiles.Add(imported);
        await _db.SaveChangesAsync(ct);

        int idx = 0;
        foreach (var chunk in _chunking.Chunk(text, _rag.MaxChunkChars, _rag.ChunkOverlap))
        {
            _db.KnowledgeChunks.Add(new KnowledgeChunk
            {
                KnowledgeItemId = ki.Id, ChunkIndex = idx++, Content = chunk, TokenCount = _chunking.EstimateTokens(chunk)
            });
        }
        await _db.SaveChangesAsync(ct);

        try { await _indexer.IndexKnowledgeItemAsync(ki.Id, ct); } catch { /* keyword fallback remains */ }
        return imported;
    }
}
