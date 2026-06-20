namespace LocalAIFactory.Core.Entities;

// KE-011: a capture-only learning record — one row per retrieval query (its text, mode, and result count).
// Lean and append-only; it feeds future retrieval-relevance adaptation (design §6) but influences nothing now.
// Writing it is best-effort and must never fail or slow the query that produced it.
public class RetrievalEvent : IPortableEntity
{
    public int Id { get; set; }
    public Guid Uid { get; set; } = Guid.CreateVersion7();
    public int? ProjectId { get; set; }
    public string Query { get; set; } = "";
    public string Mode { get; set; } = "";   // lexical | dependents | dependencies | impact
    public int ResultCount { get; set; }
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
