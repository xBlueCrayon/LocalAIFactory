using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-006: the inputs to the quality-band computation. All fields are MSSQL-derivable with NO model and
// NO vector store. The record is designed so future signals (e.g. validated outcomes from KE-028,
// contradictions from KE-025) are already represented as flags — adding their *sources* needs no redesign.
public sealed record QualityContext(
    PermanenceTier Tier,
    ProvenanceMethod InitialMethod,
    int CorroborationCount,          // primary: exact KnowledgeDuplicate links
    int DistinctProvenanceSources,   // secondary: distinct source artifacts in the provenance chain
    KnowledgeStatus Status,
    KnowledgeScope Scope,
    DateTime UpdatedUtc,
    DateTime NowUtc,
    bool HasUnresolvedContradiction, // KE-025 signal (false until that source exists)
    bool HasFailedOutcome);          // KE-028 signal (false until that source exists)

// Pure, deterministic floor-and-adjust quality band. No I/O, no model — trivially degradation-safe.
public interface IQualityEvaluator
{
    QualityBand ComputeBand(QualityContext context);
}
