using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-004: convergent identity for machine-extracted knowledge. Resolves a file to its stable source
// locus so re-extraction updates the same logical item (KE-003 versioning) or — when the item is
// curated — proposes a revision (KE-002), instead of multiplying duplicates. Pure EF, MSSQL-only.
public sealed record LocusResolution(int KnowledgeItemId, LocusOutcome Outcome);

public interface IIdentityResolver
{
    // The stable per-file locus key (instance-local, derived, regenerable — Uid remains the portable id).
    string ComputeFileLocusKey(int? projectId, string relativePath);

    // Resolve-or-create the machine-extracted knowledge item for a file. Honors KE-002 (curated =>
    // propose) and KE-003 (Derived change => new version; unchanged => no version).
    Task<LocusResolution> ResolveFileAsync(int? projectId, string relativePath, string title, string content,
        SourceType sourceType, int? sourceArtifactId = null, CancellationToken ct = default);

    // Exact-content-hash duplicate detection within a project; records KnowledgeDuplicate candidates
    // (capture only; near-duplicate and auto-merge are KE-030). Returns the number of new candidates.
    Task<int> DetectExactDuplicatesAsync(int? projectId, CancellationToken ct = default);
}
