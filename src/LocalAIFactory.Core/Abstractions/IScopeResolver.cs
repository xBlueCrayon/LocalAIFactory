using LocalAIFactory.Core.Enums;

namespace LocalAIFactory.Core.Abstractions;

// KE-005: scope precedence + applicability. Pure MSSQL/EF — no vectors, no model. Provides QUERYABLE
// precedence only; wiring it into retrieval ranking (KE-011/019) and the guardrail channel (KE-019/023)
// is deliberately out of scope here.
public sealed record PrecedenceItem(int Id, KnowledgeScope Scope, int? ProjectId, DateTime UpdatedUtc);

public interface IScopeResolver
{
    // Authority rank: higher = more authoritative (Regulatory > Standards > Project > Global > Team).
    int Rank(KnowledgeScope scope);

    // The enforcement strength implied by a scope (constraint scopes are binding/high).
    AuthorityLevel AuthorityForScope(KnowledgeScope scope);

    // Deterministic precedence ordering: scope rank desc -> specificity (project-scoped first) ->
    // recency desc -> id desc (stable tie-break).
    IReadOnlyList<PrecedenceItem> OrderByPrecedence(IEnumerable<PrecedenceItem> items);

    // Standards/Regulatory items that apply to a project: those with no applicability links (global) or
    // an explicit AppliesTo link to the project.
    Task<IReadOnlyList<int>> ResolveApplicableConstraintItemIdsAsync(int projectId, CancellationToken ct = default);
}
