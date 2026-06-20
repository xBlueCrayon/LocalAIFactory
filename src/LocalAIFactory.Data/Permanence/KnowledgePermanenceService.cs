using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Permanence;

// Phase 2 / KE-002. Pure-EF implementation of the propose-never-overwrite contract. No AI dependency,
// so it works in MSSQL-only mode. Setting an item's Tier to Curated is done directly by the caller
// (the entity is already tracked on the approval/human-create path); this service owns the *proposal*
// side so the rule is enforced uniformly wherever automation would otherwise overwrite a curated item.
public sealed class KnowledgePermanenceService : IPermanenceGuard
{
    private readonly AppDbContext _db;

    public KnowledgePermanenceService(AppDbContext db) => _db = db;

    public bool IsCurated(PermanenceTier tier) => tier == PermanenceTier.Curated;

    public async Task<int> ProposeRevisionAsync(
        string targetType, int targetId, int? originalKnowledgeItemId,
        string? proposedTitle, string proposedContent, string changeReason,
        RevisionSource source, CancellationToken ct = default)
    {
        var content = proposedContent ?? "";

        // H1: do not create a duplicate. If an open (NeedsReview) proposal already exists for this
        // target with identical proposed content, return it instead of inserting another. This keeps
        // repeated re-derivation (e.g. profile regeneration, future consolidation) from flooding the
        // review queue with identical proposals.
        var existingId = await _db.ProposedRevisions
            .Where(r => r.Status == KnowledgeStatus.NeedsReview
                        && r.TargetEntityType == targetType
                        && r.TargetEntityId == targetId
                        && r.ProposedContent == content)
            .Select(r => (int?)r.Id)
            .FirstOrDefaultAsync(ct);
        if (existingId is int id) return id;

        var rev = new ProposedRevision
        {
            TargetEntityType = targetType,
            TargetEntityId = targetId,
            OriginalKnowledgeItemId = originalKnowledgeItemId,
            ProposedTitle = proposedTitle,
            ProposedContent = content,
            ChangeReason = changeReason ?? "",
            Source = source,
            Status = KnowledgeStatus.NeedsReview,
            CreatedUtc = DateTime.UtcNow
        };
        _db.ProposedRevisions.Add(rev);
        await _db.SaveChangesAsync(ct);
        return rev.Id;
    }
}
