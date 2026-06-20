using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;
using Microsoft.EntityFrameworkCore;

namespace LocalAIFactory.Data.Backbone;

// KE-003: writes the provenance chain and version history for knowledge items. Pure EF; no AI; works
// in MSSQL-only mode. Honors KE-002 — callers route automated changes to curated items through the
// permanence guard (ProposedRevision); this service is for changes that are actually being applied.
public sealed class KnowledgeBackboneService : IKnowledgeBackboneService
{
    private readonly AppDbContext _db;
    private readonly IContentHasher _hasher;
    private readonly IInstanceContext _instance;

    public KnowledgeBackboneService(AppDbContext db, IContentHasher hasher, IInstanceContext instance)
    {
        _db = db; _hasher = hasher; _instance = instance;
    }

    public async Task RecordInitialAsync(KnowledgeItem item, ProvenanceMethod method, string actor, string reason,
        int? sourceArtifactId = null, string? extractorOrModelId = null, CancellationToken ct = default)
    {
        item.ContentHash = _hasher.Compute(item.Content);
        item.VersionNumber = 1;

        _db.KnowledgeVersions.Add(new KnowledgeVersion
        {
            KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, VersionNumber = 1,
            ContentSnapshot = item.Content, ContentHash = item.ContentHash, Title = item.Title,
            Summary = item.Summary, ChangeReason = reason, Method = method, Actor = actor,
            TierAtVersion = item.Tier, StatusAtVersion = item.Status
        });
        _db.ProvenanceEvents.Add(new ProvenanceEvent
        {
            KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, SourceArtifactId = sourceArtifactId,
            Method = method, ExtractorOrModelId = extractorOrModelId, Actor = actor, Reason = reason,
            OriginInstanceId = await _instance.GetInstanceIdAsync(ct)
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordEditAsync(KnowledgeItem item, string reason, ProvenanceMethod method, string actor,
        CancellationToken ct = default)
    {
        var instanceId = await _instance.GetInstanceIdAsync(ct);
        var newHash = _hasher.Compute(item.Content);

        // Hash-guard: only create a new version when the content actually changed (convergence).
        if (newHash != item.ContentHash)
        {
            var prevUid = await _db.KnowledgeVersions
                .Where(v => v.KnowledgeItemId == item.Id)
                .OrderByDescending(v => v.VersionNumber)
                .Select(v => (Guid?)v.Uid)
                .FirstOrDefaultAsync(ct);

            item.ContentHash = newHash;
            item.VersionNumber += 1;
            _db.KnowledgeVersions.Add(new KnowledgeVersion
            {
                KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, VersionNumber = item.VersionNumber,
                ContentSnapshot = item.Content, ContentHash = newHash, Title = item.Title, Summary = item.Summary,
                ChangeReason = reason, Method = method, Actor = actor, TierAtVersion = item.Tier,
                StatusAtVersion = item.Status, PreviousVersionUid = prevUid
            });
        }

        _db.ProvenanceEvents.Add(new ProvenanceEvent
        {
            KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, Method = method, Actor = actor,
            Reason = reason, OriginInstanceId = instanceId
        });
        await _db.SaveChangesAsync(ct);
    }

    public async Task RecordProvenanceAsync(KnowledgeItem item, ProvenanceMethod method, string actor, string reason,
        Guid? originPackUid = null, CancellationToken ct = default)
    {
        _db.ProvenanceEvents.Add(new ProvenanceEvent
        {
            KnowledgeItemId = item.Id, KnowledgeItemUid = item.Uid, Method = method, Actor = actor,
            Reason = reason, OriginInstanceId = await _instance.GetInstanceIdAsync(ct), OriginPackUid = originPackUid
        });
        await _db.SaveChangesAsync(ct);
    }
}
