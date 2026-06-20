namespace LocalAIFactory.Core.Abstractions;

// KE-003: deterministic, normalized content fingerprint (SHA-256, lowercase hex). Used for the
// version hash-guard and for cross-instance / Knowledge Pack reconciliation. App-side and MSSQL-only.
public interface IContentHasher
{
    string Compute(string? content);
}
