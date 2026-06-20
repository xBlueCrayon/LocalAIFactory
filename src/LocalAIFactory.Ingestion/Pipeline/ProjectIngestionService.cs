using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using LocalAIFactory.Core.Options;
using LocalAIFactory.Data;
using LocalAIFactory.Ingestion.Imports;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace LocalAIFactory.Ingestion.Pipeline;

public sealed class ProjectIngestionService : IProjectIngestionService
{
    private static readonly HashSet<string> IgnoredDirs = new(StringComparer.OrdinalIgnoreCase)
    {
        "bin", "obj", ".git", ".vs", ".idea", "packages", "node_modules", "testresults", ".svn", "dist", ".vscode"
    };

    private readonly AppDbContext _db;
    private readonly IZipExtractionService _zip;
    private readonly IFileClassifier _classifier;
    private readonly IChunkingService _chunking;
    private readonly IKnowledgeIndexer _indexer;
    private readonly IProjectProfileService _profile;
    private readonly IKnowledgeGraphService _graph;
    private readonly IModelExecutionService _model;
    private readonly IIdentityResolver _identity;
    private readonly ICodeSymbolStore _symbols;
    private readonly ISchemaSymbolStore _schema;
    private readonly ICodeGraphBuilder _graphBuilder;
    private readonly IImportCoverageService _coverage;
    private readonly WorkspacesOptions _ws;
    private readonly RagOptions _rag;
    private readonly ILogger<ProjectIngestionService> _log;

    public ProjectIngestionService(
        AppDbContext db, IZipExtractionService zip, IFileClassifier classifier, IChunkingService chunking,
        IKnowledgeIndexer indexer, IProjectProfileService profile, IKnowledgeGraphService graph,
        IModelExecutionService model, IIdentityResolver identity, ICodeSymbolStore symbols, ISchemaSymbolStore schema,
        ICodeGraphBuilder graphBuilder, IImportCoverageService coverage, IOptions<WorkspacesOptions> ws, IOptions<RagOptions> rag, ILogger<ProjectIngestionService> log)
    {
        _db = db; _zip = zip; _classifier = classifier; _chunking = chunking; _indexer = indexer;
        _profile = profile; _graph = graph; _model = model; _identity = identity; _symbols = symbols; _schema = schema;
        _graphBuilder = graphBuilder; _coverage = coverage; _ws = ws.Value; _rag = rag.Value; _log = log;
    }

    private string IncomingDir => Path.Combine(_ws.Root, "_incoming");
    private string IncomingZipPath(int jobId) => Path.Combine(IncomingDir, jobId + ".zip");

    public async Task<int> CreateZipJobAsync(int? projectId, string fileName, byte[] zipBytes, CancellationToken ct = default)
    {
        var job = new IngestionJob
        {
            ProjectId = projectId,
            FileName = fileName,
            Status = IngestionJobStatus.Pending,
            Phase = IngestionPhase.Pending
        };
        _db.IngestionJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        Directory.CreateDirectory(IncomingDir);
        await File.WriteAllBytesAsync(IncomingZipPath(job.Id), zipBytes, ct);
        return job.Id;
    }

    public async Task<IngestionProgress?> GetProgressAsync(int ingestionJobId, CancellationToken ct = default)
    {
        var j = await _db.IngestionJobs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ingestionJobId, ct);
        if (j is null) return null;
        return new IngestionProgress
        {
            JobId = j.Id, Status = j.Status, Phase = j.Phase,
            TotalFiles = j.TotalFiles, ProcessedFiles = j.ProcessedFiles, SkippedFiles = j.SkippedFiles,
            ChunkCount = j.ChunkCount, EmbeddedCount = j.EmbeddedCount, Error = j.Error
        };
    }

    public async Task ProcessJobAsync(int ingestionJobId, CancellationToken ct = default)
    {
        var job = await _db.IngestionJobs.FirstOrDefaultAsync(x => x.Id == ingestionJobId, ct);
        if (job is null) return;

        job.Status = IngestionJobStatus.Running;
        job.StartedUtc = DateTime.UtcNow;
        job.Phase = IngestionPhase.Extracting;
        await _db.SaveChangesAsync(ct);

        var created = new List<(int Id, string Content, FileClass Class)>();
        try
        {
            var zipPath = IncomingZipPath(job.Id);
            if (!File.Exists(zipPath)) throw new FileNotFoundException("Uploaded archive was not found for this job.");
            var bytes = await File.ReadAllBytesAsync(zipPath, ct);

            var root = await _zip.ExtractAsync(job.FileName, bytes, _ws.Root, ct);
            job.ExtractedRoot = root;
            job.Phase = IngestionPhase.Scanning;
            await _db.SaveChangesAsync(ct);

            var files = EnumerateFiles(root).ToList();
            job.TotalFiles = files.Count;
            job.Phase = IngestionPhase.Classifying;
            await _db.SaveChangesAsync(ct);

            job.Phase = IngestionPhase.Storing;
            await _db.SaveChangesAsync(ct);

            int processed = 0;
            foreach (var full in files)
            {
                ct.ThrowIfCancellationRequested();
                processed++;
                var rel = full;
                // R2-P0C: one unprocessable file must NEVER abort the whole import. The entire per-file body is
                // guarded; cancellation propagates, anything else is recorded honestly and we move on.
                try
                {
                    rel = Path.GetRelativePath(root, full);
                    var fileClass = _classifier.Classify(rel);
                    var ext = Path.GetExtension(rel).ToLowerInvariant();
                    var info = new FileInfo(full);

                    var imported = new ImportedFile
                    {
                        ProjectId = job.ProjectId,
                        IngestionJobId = job.Id,
                        FileName = Path.GetFileName(rel),
                        RelativePath = rel,
                        Extension = string.IsNullOrEmpty(ext) ? "" : ext,
                        FileClass = fileClass,
                        SizeBytes = info.Exists ? info.Length : 0,
                        Status = ImportStatus.Processed
                    };

                    bool textual = _classifier.IsTextual(fileClass, ext);
                    if (!textual)
                    {
                        imported.Skipped = true;
                        imported.SkipReason = "binary";
                        job.SkippedFiles++;
                        _db.ImportedFiles.Add(imported);
                    }
                    else if (info.Exists && info.Length > _ws.MaxTextFileBytes)
                    {
                        imported.Skipped = true;
                        imported.SkipReason = "too large";
                        job.SkippedFiles++;
                        _db.ImportedFiles.Add(imported);
                    }
                    else
                    {
                        byte[] raw;
                        try { raw = await File.ReadAllBytesAsync(full, ct); }
                        catch (OperationCanceledException) { throw; }
                        catch { imported.Skipped = true; imported.SkipReason = "unreadable"; job.SkippedFiles++; _db.ImportedFiles.Add(imported); continue; }

                        // R2-P0C: content-based binary detection catches binaries mislabeled with a text extension.
                        if (RobustText.IsBinary(raw))
                        {
                            imported.Skipped = true; imported.SkipReason = "binary (content)"; job.SkippedFiles++;
                            _db.ImportedFiles.Add(imported); continue;
                        }

                        // R2-P0C: BOM/UTF-8/Latin-1 decode; a non-UTF-8 fallback is recorded, never silent.
                        var text = RobustText.Decode(raw, out var encNote);
                        if (encNote != null) imported.ExtractionNote = encNote;

                        var sha = Sha256(text);
                        imported.Sha256 = sha;

                        // KE-004: raw artifact identity by path. The latest non-skipped artifact at this path,
                        // if any, is the prior version of the same logical file.
                        var samePath = await _db.ImportedFiles
                            .Where(f => f.ProjectId == job.ProjectId && f.RelativePath == rel && !f.Skipped && f.KnowledgeItemId != null)
                            .OrderByDescending(f => f.Id)
                            .FirstOrDefaultAsync(ct);

                        if (samePath != null && samePath.Sha256 == sha)
                        {
                            imported.Skipped = true;
                            imported.SkipReason = "duplicate";
                            imported.KnowledgeItemId = samePath.KnowledgeItemId;
                            job.SkippedFiles++;
                            _db.ImportedFiles.Add(imported);
                        }
                        else
                        {
                            imported.RawText = text;
                            imported.DetectedLanguage = _classifier.DetectLanguage(ext); // KE-007
                            if (samePath != null) imported.SupersedesImportedFileId = samePath.Id; // changed content
                            _db.ImportedFiles.Add(imported);
                            await _db.SaveChangesAsync(ct);
                            var res = await _identity.ResolveFileAsync(job.ProjectId, rel, rel, text,
                                _classifier.ToSourceType(fileClass), sourceArtifactId: imported.Id, ct);
                            imported.KnowledgeItemId = res.KnowledgeItemId;
                            if (res.Outcome is LocusOutcome.Created or LocusOutcome.Updated)
                                created.Add((res.KnowledgeItemId, text, fileClass));
                        }
                    }
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    _log.LogWarning(ex, "Skipped unprocessable file {File}", full);
                    try
                    {
                        string safeName; try { safeName = Path.GetFileName(rel); } catch { safeName = "unknown"; }
                        _db.ImportedFiles.Add(new ImportedFile
                        {
                            ProjectId = job.ProjectId, IngestionJobId = job.Id,
                            FileName = safeName, RelativePath = rel, Skipped = true, SkipReason = "error",
                            ExtractionNote = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message,
                            Status = ImportStatus.Processed
                        });
                        await _db.SaveChangesAsync(ct);
                        job.SkippedFiles++;
                    }
                    catch { /* even the error bookkeeping is best-effort — never let it abort the import */ }
                }

                if (processed % 25 == 0)
                {
                    job.ProcessedFiles = processed;
                    try { await _db.SaveChangesAsync(ct); } catch (OperationCanceledException) { throw; } catch { }
                }
            }
            job.ProcessedFiles = processed;
            await _db.SaveChangesAsync(ct);

            // Chunking
            job.Phase = IngestionPhase.Chunking;
            await _db.SaveChangesAsync(ct);
            int chunkCount = 0;
            foreach (var item in created)
            {
                // KE-004: clear prior chunks so re-extracted/updated items don't accumulate stale chunks.
                var oldChunks = await _db.KnowledgeChunks.Where(c => c.KnowledgeItemId == item.Id).ToListAsync(ct);
                if (oldChunks.Count > 0) _db.KnowledgeChunks.RemoveRange(oldChunks);
                var chunks = _chunking.Chunk(item.Content, _rag.MaxChunkChars, _rag.ChunkOverlap);
                int idx = 0;
                foreach (var chunk in chunks)
                {
                    _db.KnowledgeChunks.Add(new KnowledgeChunk
                    {
                        KnowledgeItemId = item.Id,
                        ChunkIndex = idx++,
                        Content = chunk,
                        TokenCount = _chunking.EstimateTokens(chunk)
                    });
                    chunkCount++;
                }
                await _db.SaveChangesAsync(ct);
            }
            job.ChunkCount = chunkCount;
            await _db.SaveChangesAsync(ct);

            // Embedding (safe no-op if vector store / embeddings unavailable)
            job.Phase = IngestionPhase.Embedding;
            await _db.SaveChangesAsync(ct);
            foreach (var item in created)
            {
                try { await _indexer.IndexKnowledgeItemAsync(item.Id, ct); }
                catch (Exception ex) { _log.LogWarning(ex, "Embedding failed for knowledge item {Id}", item.Id); }
            }
            var createdIds = created.Select(c => c.Id).ToList();
            job.EmbeddedCount = await _db.KnowledgeChunks.CountAsync(c => createdIds.Contains(c.KnowledgeItemId) && c.VectorId != null, ct);
            await _db.SaveChangesAsync(ct);

            // Profiling (optional)
            job.Phase = IngestionPhase.Profiling;
            await _db.SaveChangesAsync(ct);
            if (job.ProjectId is int pid)
            {
                try { await _profile.GenerateAsync(pid, job.Id, ct); }
                catch (Exception ex) { _log.LogWarning(ex, "Profile generation failed for project {Pid}", pid); }
            }

            // Graph extraction (optional, bounded)
            job.Phase = IngestionPhase.GraphExtraction;
            await _db.SaveChangesAsync(ct);
            foreach (var item in created.Where(c => c.Class is FileClass.SourceCode or FileClass.SqlScript or FileClass.RazorView or FileClass.Documentation or FileClass.DeploymentNote).Take(40))
            {
                try { await _graph.ExtractAsync(job.ProjectId, item.Id, await TitleOf(item.Id, ct), item.Content, ct); }
                catch (Exception ex) { _log.LogWarning(ex, "Graph extraction failed for item {Id}", item.Id); }
            }

            // Candidate extraction (optional)
            job.Phase = IngestionPhase.CandidateExtraction;
            await _db.SaveChangesAsync(ct);
            try { await ExtractCandidateRulesAsync(job.ProjectId, created, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "Candidate extraction failed for job {Id}", job.Id); }

            // KE-004: record exact-content duplicate candidates (capture only; auto-merge is KE-030).
            try { await _identity.DetectExactDuplicatesAsync(job.ProjectId, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "Duplicate detection failed for job {Id}", job.Id); }

            // KE-008/KE-009: deterministic structural extraction (C# symbols + T-SQL schema). Incremental —
            // only this job's changed/new artifacts (dedup already skips unchanged files). Best-effort.
            job.Phase = IngestionPhase.SymbolExtraction;
            await _db.SaveChangesAsync(ct);
            try { await ExtractSymbolsAsync(job.Id, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "Symbol extraction failed for job {Id}", job.Id); }

            // KE-010: resolve references into the structural graph (convergent, project-scoped). Best-effort.
            job.Phase = IngestionPhase.StructuralGraph;
            await _db.SaveChangesAsync(ct);
            try
            {
                var g = await _graphBuilder.RebuildForProjectAsync(job.ProjectId, ct);
                _log.LogInformation("Structural graph for project {Pid}: {Edges} edges, {Unresolved} unresolved references",
                    job.ProjectId, g.Edges, g.Unresolved);
            }
            catch (Exception ex) { _log.LogWarning(ex, "Structural graph build failed for job {Id}", job.Id); }

            // R2-P0A: compute the honest coverage / gap report for this import. Best-effort.
            try { await _coverage.ComputeAsync(job.ProjectId, job.Id, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "Coverage report failed for job {Id}", job.Id); }

            job.Phase = IngestionPhase.Completed;
            job.Status = IngestionJobStatus.Completed;
            job.CompletedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { /* best effort */ }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Ingestion job {Id} failed", job.Id);
            job.Status = IngestionJobStatus.Failed;
            job.Phase = IngestionPhase.Failed;
            job.Error = ex.Message;
            job.CompletedUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }
    }

    private async Task<string> TitleOf(int knowledgeItemId, CancellationToken ct)
        => (await _db.KnowledgeItems.Where(k => k.Id == knowledgeItemId).Select(k => k.Title).FirstOrDefaultAsync(ct)) ?? "file";

    // KE-008/KE-009: extract structure from this job's changed/new C# and SQL artifacts only. Loads ids first
    // (not RawText) so memory stays bounded on large repos; the stores stream one artifact at a time.
    // Per-artifact failures are isolated so one malformed file can't abort the others.
    private async Task ExtractSymbolsAsync(int jobId, CancellationToken ct)
    {
        // R2-ACC-CAP3: C# and Python both route through the code-symbol store (the router dispatches by language).
        var codeIds = await _db.ImportedFiles
            .Where(f => f.IngestionJobId == jobId && !f.Skipped && (f.DetectedLanguage == "csharp" || f.DetectedLanguage == "python") && f.RawText != null)
            .Select(f => f.Id)
            .ToListAsync(ct);
        foreach (var id in codeIds)
        {
            ct.ThrowIfCancellationRequested();
            try { await _symbols.UpsertForArtifactAsync(id, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "Code symbol extraction failed for artifact {Id}", id); await MarkParseErrorAsync(id, ex, ct); }
        }

        var sqlIds = await _db.ImportedFiles
            .Where(f => f.IngestionJobId == jobId && !f.Skipped && f.DetectedLanguage == "sql" && f.RawText != null)
            .Select(f => f.Id)
            .ToListAsync(ct);
        foreach (var id in sqlIds)
        {
            ct.ThrowIfCancellationRequested();
            try { await _schema.UpsertForArtifactAsync(id, ct); }
            catch (Exception ex) { _log.LogWarning(ex, "T-SQL schema extraction failed for artifact {Id}", id); await MarkParseErrorAsync(id, ex, ct); }
        }
    }

    // R2-P0A: record a parse failure on the artifact so the gap report can surface it — never a silent loss.
    private async Task MarkParseErrorAsync(int artifactId, Exception ex, CancellationToken ct)
    {
        try
        {
            var art = await _db.ImportedFiles.FirstOrDefaultAsync(f => f.Id == artifactId, ct);
            if (art is null) return;
            art.ExtractionStatus = ExtractionStatus.ParseError;
            art.ExtractionNote = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
            await _db.SaveChangesAsync(ct);
        }
        catch { /* never let gap-bookkeeping break ingestion */ }
    }

    private async Task ExtractCandidateRulesAsync(int? projectId, List<(int Id, string Content, FileClass Class)> created, CancellationToken ct)
    {
        // Heuristic: rule-like sentences in docs / readme / deployment notes.
        var docs = created.Where(c => c.Class is FileClass.Documentation or FileClass.Readme or FileClass.DeploymentNote).ToList();
        var keywords = new[] { "must", "should", "shall", "required", "require", "only if", "never", "always", "do not", "ensure" };
        int added = 0;
        foreach (var d in docs)
        {
            foreach (var line in SplitSentences(d.Content))
            {
                if (added >= 30) break;
                var lower = line.ToLowerInvariant();
                if (line.Length is < 25 or > 400) continue;
                if (keywords.Any(k => lower.Contains(k)))
                {
                    _db.BusinessRules.Add(new BusinessRule
                    {
                        ProjectId = projectId,
                        Title = Truncate(line, 120),
                        Content = line.Trim(),
                        Status = BusinessRuleStatus.NeedsReview,
                        IsApproved = false,
                        Tier = PermanenceTier.Derived
                    });
                    added++;
                }
            }
            if (added >= 30) break;
        }
        if (added > 0) await _db.SaveChangesAsync(ct);

        // Model-assisted (single bounded call) when a model is available.
        var digest = BuildDigest(docs.Select(d => d.Content), 6000);
        if (!string.IsNullOrWhiteSpace(digest))
        {
            var sys = "Extract concrete, testable business or engineering rules from the provided project text. "
                    + "Return one rule per line, no numbering, max 15 rules. If none, return nothing.";
            string answer;
            try { answer = await _model.CompleteSimpleAsync(TaskType.BusinessRuleExtraction, sys, digest, ct); }
            catch { answer = ""; }
            if (!string.IsNullOrWhiteSpace(answer))
            {
                int mAdded = 0;
                foreach (var raw in answer.Split('\n'))
                {
                    var line = raw.TrimStart('-', '*', '•', ' ', '\t').Trim();
                    if (line.Length is < 15 or > 400) continue;
                    if (mAdded >= 15) break;
                    _db.BusinessRules.Add(new BusinessRule
                    {
                        ProjectId = projectId,
                        Title = Truncate(line, 120),
                        Content = line,
                        Status = BusinessRuleStatus.NeedsReview,
                        IsApproved = false,
                        Tier = PermanenceTier.Derived
                    });
                    mAdded++;
                }
                if (mAdded > 0) await _db.SaveChangesAsync(ct);
            }
        }
    }

    private static IEnumerable<string> EnumerateFiles(string root)
    {
        // R2-P0C: tolerate inaccessible/looping directories — a single bad subtree must not abort enumeration.
        // IgnoreInaccessible skips permission failures; reparse points (symlinks/junctions) are not recursed,
        // preventing escapes outside the extracted root and infinite loops.
        var opts = new EnumerationOptions
        {
            RecurseSubdirectories = true,
            IgnoreInaccessible = true,
            AttributesToSkip = FileAttributes.ReparsePoint | FileAttributes.System
        };
        var it = SafeEnumerator(root, opts);
        foreach (var f in it)
        {
            string rel;
            try { rel = Path.GetRelativePath(root, f); } catch { continue; }
            var parts = rel.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (parts.Any(p => IgnoredDirs.Contains(p))) continue;
            yield return f;
        }
    }

    // Wraps lazy enumeration so an exception thrown DURING iteration (not just at the start) ends enumeration
    // cleanly instead of propagating and failing the whole import.
    private static IEnumerable<string> SafeEnumerator(string root, EnumerationOptions opts)
    {
        IEnumerator<string> e;
        try { e = Directory.EnumerateFiles(root, "*", opts).GetEnumerator(); }
        catch { yield break; }
        while (true)
        {
            string current;
            try { if (!e.MoveNext()) break; current = e.Current; }
            catch { break; } // a bad directory mid-walk ends enumeration of the rest of that subtree, not the job
            yield return current;
        }
    }

    private static IEnumerable<string> SplitSentences(string text)
        => (text ?? "").Replace("\r", " ").Split(new[] { '\n', '.', '!', '?', '•' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim()).Where(s => s.Length > 0);

    private static string BuildDigest(IEnumerable<string> contents, int maxChars)
    {
        var sb = new StringBuilder();
        foreach (var c in contents)
        {
            if (sb.Length >= maxChars) break;
            sb.AppendLine(c.Length > 1500 ? c.Substring(0, 1500) : c);
            sb.AppendLine("----");
        }
        var s = sb.ToString();
        return s.Length > maxChars ? s.Substring(0, maxChars) : s;
    }

    private static string Truncate(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "…";

    private static string Sha256(string text)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(bytes);
    }
}
