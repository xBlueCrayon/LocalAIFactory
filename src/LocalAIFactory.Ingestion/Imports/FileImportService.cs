using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Ingestion.Imports;

// Imports a single uploaded file as a knowledge item (chunked, embedded, NeedsReview).
public sealed class FileImportService : IFileImportService
{
    private readonly AppDbContext _db;
    private readonly IFileClassifier _classifier;
    private readonly IChunkingService _chunking;
    private readonly IKnowledgeIndexer _indexer;
    private readonly IIdentityResolver _identity;
    private readonly ICodeSymbolStore _symbols;
    private readonly ISchemaSymbolStore _schema;
    private readonly ICodeGraphBuilder _graphBuilder;
    private readonly RagOptions _rag;

    public FileImportService(
        AppDbContext db, IFileClassifier classifier, IChunkingService chunking,
        IKnowledgeIndexer indexer, IIdentityResolver identity, ICodeSymbolStore symbols,
        ISchemaSymbolStore schema, ICodeGraphBuilder graphBuilder, IOptions<RagOptions> rag)
    {
        _db = db; _classifier = classifier; _chunking = chunking; _indexer = indexer; _identity = identity;
        _symbols = symbols; _schema = schema; _graphBuilder = graphBuilder; _rag = rag.Value;
    }

    public async Task<ImportedFile> ImportAsync(int? projectId, string fileName, byte[] content, CancellationToken ct = default)
    {
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

        // R2-P0C: skip binary by extension OR by content (a binary mislabeled with a text extension).
        if (!textual || RobustText.IsBinary(content))
        {
            imported.Skipped = true;
            imported.SkipReason = textual ? "binary (content)" : "binary";
            _db.ImportedFiles.Add(imported);
            await _db.SaveChangesAsync(ct);
            return imported;
        }

        // R2-P0C: robust decode (BOM/UTF-8/Latin-1); a non-UTF-8 fallback is recorded, never silent.
        var text = RobustText.Decode(content, out var encNote);
        if (encNote != null) imported.ExtractionNote = encNote;
        imported.RawText = text;
        imported.DetectedLanguage = _classifier.DetectLanguage(ext); // KE-007
        // KE-007: persist the artifact first so the derived knowledge links back to it.
        _db.ImportedFiles.Add(imported);
        await _db.SaveChangesAsync(ct);
        // KE-004/007: converge by source locus and link the derived item to its artifact.
        var res = await _identity.ResolveFileAsync(projectId, fileName, Path.GetFileName(fileName), text,
            _classifier.ToSourceType(fileClass), sourceArtifactId: imported.Id, ct);
        imported.KnowledgeItemId = res.KnowledgeItemId;
        await _db.SaveChangesAsync(ct);

        // (Re)chunk + index only when content was created or updated; curated/unchanged keep their chunks.
        if (res.Outcome is LocusOutcome.Created or LocusOutcome.Updated)
        {
            var oldChunks = await _db.KnowledgeChunks.Where(c => c.KnowledgeItemId == res.KnowledgeItemId).ToListAsync(ct);
            if (oldChunks.Count > 0) _db.KnowledgeChunks.RemoveRange(oldChunks);
            int idx = 0;
            foreach (var chunk in _chunking.Chunk(text, _rag.MaxChunkChars, _rag.ChunkOverlap))
                _db.KnowledgeChunks.Add(new KnowledgeChunk
                {
                    KnowledgeItemId = res.KnowledgeItemId, ChunkIndex = idx++, Content = chunk, TokenCount = _chunking.EstimateTokens(chunk)
                });
            await _db.SaveChangesAsync(ct);
            try { await _indexer.IndexKnowledgeItemAsync(res.KnowledgeItemId, ct); } catch { /* keyword fallback remains */ }
        }

        // KE-008/KE-009: deterministic structural extraction (best-effort; never fails the import).
        // KE-010: resolve the artifact's references into the structural graph after extraction.
        if (string.Equals(imported.DetectedLanguage, "csharp", StringComparison.OrdinalIgnoreCase))
            try { await _symbols.UpsertForArtifactAsync(imported.Id, ct); } catch { /* symbols are regenerable */ }
        else if (string.Equals(imported.DetectedLanguage, "sql", StringComparison.OrdinalIgnoreCase))
            try { await _schema.UpsertForArtifactAsync(imported.Id, ct); } catch { /* symbols are regenerable */ }

        // KE-008.x/KE-010: resolve the artifact's references into the structural graph (best-effort).
        if (string.Equals(imported.DetectedLanguage, "csharp", StringComparison.OrdinalIgnoreCase)
            || string.Equals(imported.DetectedLanguage, "sql", StringComparison.OrdinalIgnoreCase))
            try { await _graphBuilder.RebuildForArtifactAsync(imported.Id, ct); } catch { /* edges are regenerable */ }

        return imported;
    }
}
