using LocalAIFactory.Core.Entities;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-003: provenance + version history for the knowledge backbone. Pure EF, MSSQL-only, no AI.
// The item must already be saved (have an Id and Uid) before these are called.
public interface IKnowledgeBackboneService
{
    // First version (v1) + initial provenance for a newly created knowledge item.
    Task RecordInitialAsync(KnowledgeItem item, ProvenanceMethod method, string actor, string reason,
        int? sourceArtifactId = null, string? extractorOrModelId = null, CancellationToken ct = default);

    // Hash-guarded edit: writes a new version + bumps VersionNumber ONLY when content changed;
    // always appends a provenance event.
    Task RecordEditAsync(KnowledgeItem item, string reason, ProvenanceMethod method, string actor,
        int? sourceArtifactId = null, CancellationToken ct = default);

    // Append a provenance event without a version (approve, tier change, import, etc.).
    Task RecordProvenanceAsync(KnowledgeItem item, ProvenanceMethod method, string actor, string reason,
        Guid? originPackUid = null, CancellationToken ct = default);
}
