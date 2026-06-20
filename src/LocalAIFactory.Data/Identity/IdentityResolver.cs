using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Identity;

// KE-004: convergent identity for machine-extracted knowledge. Pure EF, MSSQL-only — needs no vector
// store or model. Re-extraction resolves by source locus and either updates a Derived item in place
// (with a KE-003 version) or proposes a revision to a Curated item (KE-002), never overwriting it.
public sealed class IdentityResolver : IIdentityResolver
{
    private readonly AppDbContext _db;
    private readonly IKnowledgeBackboneService _backbone;
    private readonly IPermanenceGuard _permanence;
    private readonly IContentHasher _hasher;

    public IdentityResolver(AppDbContext db, IKnowledgeBackboneService backbone, IPermanenceGuard permanence, IContentHasher hasher)
    {
        _db = db; _backbone = backbone; _permanence = permanence; _hasher = hasher;
    }

    public string ComputeFileLocusKey(int? projectId, string relativePath) => SourceLocus.FileKey(projectId, relativePath);

    public async Task<LocusResolution> ResolveFileAsync(int? projectId, string relativePath, string title, string content,
        SourceType sourceType, CancellationToken ct = default)
    {
        var locus = SourceLocus.FileKey(projectId, relativePath);
        var existing = await _db.KnowledgeItems.FirstOrDefaultAsync(k => k.SourceLocusKey == locus, ct);

        if (existing is null)
        {
            var ki = new KnowledgeItem
            {
                ProjectId = projectId,
                Title = title,
                Content = content,
                SourceType = sourceType,
                Status = KnowledgeStatus.NeedsReview,
                Confidence = 0.5,
                Tier = PermanenceTier.Derived,
                SourceLocusKey = locus
            };
            _db.KnowledgeItems.Add(ki);
            await _db.SaveChangesAsync(ct);
            await _backbone.RecordInitialAsync(ki, ProvenanceMethod.Deterministic, "system:extraction",
                $"Extracted from {relativePath}", ct: ct);
            return new LocusResolution(ki.Id, LocusOutcome.Created);
        }

        // Convergence: same logical item already exists.
        var newHash = _hasher.Compute(content);
        if (newHash == existing.ContentHash)
            return new LocusResolution(existing.Id, LocusOutcome.Unchanged); // idempotent re-extraction

        if (_permanence.IsCurated(existing.Tier))
        {
            // KE-002: never overwrite curated knowledge — route the change to review.
            await _permanence.ProposeRevisionAsync("KnowledgeItem", existing.Id, existing.Id, title, content,
                "Re-extraction produced new content for a curated item.", RevisionSource.Extraction, ct);
            return new LocusResolution(existing.Id, LocusOutcome.ProposedRevision);
        }

        // Derived: update in place and record a new version (KE-003).
        existing.Title = title;
        existing.Content = content;
        existing.UpdatedUtc = DateTime.UtcNow;
        await _backbone.RecordEditAsync(existing, $"Re-extracted from {relativePath}",
            ProvenanceMethod.Deterministic, "system:extraction", ct);
        return new LocusResolution(existing.Id, LocusOutcome.Updated);
    }

    public async Task<int> DetectExactDuplicatesAsync(int? projectId, CancellationToken ct = default)
    {
        // Exact-content-hash duplicates within the project. The earliest item (lowest Id) is treated as
        // canonical; later identical items are recorded as duplicates of it. Capture only.
        var rows = await _db.KnowledgeItems
            .Where(k => k.ProjectId == projectId && k.ContentHash != "")
            .Select(k => new { k.Id, k.Uid, k.ContentHash })
            .ToListAsync(ct);

        int added = 0;
        foreach (var group in rows.GroupBy(r => r.ContentHash).Where(g => g.Count() > 1))
        {
            var ordered = group.OrderBy(r => r.Id).ToList();
            var canonical = ordered[0];
            foreach (var dup in ordered.Skip(1))
            {
                var exists = await _db.KnowledgeDuplicates.AnyAsync(
                    d => d.KnowledgeItemId == dup.Id && d.DuplicateOfKnowledgeItemId == canonical.Id, ct);
                if (exists) continue;
                _db.KnowledgeDuplicates.Add(new KnowledgeDuplicate
                {
                    KnowledgeItemId = dup.Id, KnowledgeItemUid = dup.Uid,
                    DuplicateOfKnowledgeItemId = canonical.Id, DuplicateOfUid = canonical.Uid,
                    MatchKind = DuplicateMatchKind.Exact, Status = DuplicateStatus.Candidate, Confidence = 1.0
                });
                added++;
            }
        }
        if (added > 0) await _db.SaveChangesAsync(ct);
        return added;
    }
}
