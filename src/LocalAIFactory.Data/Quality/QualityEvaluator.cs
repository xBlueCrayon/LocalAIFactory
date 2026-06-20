using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Data.Quality;

// KE-006: deterministic floor-and-adjust quality band. Pure function — no I/O, no model, no vectors —
// so it computes identically in MSSQL-only / Minimal mode. Provenance tier sets the floor; corroboration,
// approval, scope, and currency adjust; a contradiction or failed outcome floors trust (anti-drift).
public sealed class QualityEvaluator : IQualityEvaluator
{
    // Conservative starting threshold; non-curated knowledge older than this is gently de-rated.
    private const double StalenessDays = 365.0;

    public QualityBand ComputeBand(QualityContext c)
    {
        // Anti-drift override: a contradiction or failed outcome floors trust regardless of other signals.
        if (c.HasUnresolvedContradiction || c.HasFailedOutcome) return QualityBand.Provisional;

        // Floor from provenance tier: human-curated outranks single-source machine extraction.
        var band = c.Tier == PermanenceTier.Curated ? QualityBand.Trusted : QualityBand.Provisional;

        // Corroboration (independent confirmation) raises derived knowledge.
        var corroborated = c.CorroborationCount >= 1 || c.DistinctProvenanceSources >= 2;
        if (corroborated && band < QualityBand.Corroborated) band = QualityBand.Corroborated;

        // Human approval promotes to Trusted.
        if (c.Status == KnowledgeStatus.Approved && band < QualityBand.Trusted) band = QualityBand.Trusted;

        // Approved binding-constraint scopes reach Authoritative (regulatory/standards).
        if (c.Status == KnowledgeStatus.Approved &&
            (c.Scope == KnowledgeScope.Regulatory || c.Scope == KnowledgeScope.Standards))
            band = QualityBand.Authoritative;

        // Currency: stale, non-curated knowledge is gently demoted one step (floored at Provisional).
        var ageDays = (c.NowUtc - c.UpdatedUtc).TotalDays;
        if (c.Tier != PermanenceTier.Curated && ageDays > StalenessDays && band > QualityBand.Provisional)
            band = (QualityBand)((int)band - 1);

        return band;
    }
}
