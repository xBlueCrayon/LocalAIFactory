namespace LocalAIFactory.Core.Abstractions;

// R2-ACC-B1: installs a portable Knowledge Pack (manifest.json + category files) into MSSQL. The whole pack
// is validated in memory FIRST; the database is touched only when the pack is fully valid, so a malformed
// pack can never partially corrupt the store. Installs are idempotent (keyed on each item's stable Uid) and
// never silently overwrite a user-edited baseline item (a proposed revision is raised instead).
public interface IKnowledgePackInstaller
{
    Task<KnowledgePackInstallResult> InstallAsync(string packDirectory, string actor, CancellationToken ct = default);
}

// Outcome of an install run. Created/Updated/Unchanged/Proposed are mutually exclusive per item, so they sum
// to the number of valid items processed. Errors is non-empty when validation failed (no DB writes happened).
public sealed record KnowledgePackInstallResult(
    bool Success,
    Guid PackUid,
    string Name,
    string Version,
    int TotalItems,
    int Created,
    int Updated,
    int Unchanged,
    int ProposedRevisions,
    bool AlreadyCurrent,
    IReadOnlyList<string> Errors)
{
    public static KnowledgePackInstallResult Failed(IReadOnlyList<string> errors) =>
        new(false, Guid.Empty, "", "", 0, 0, 0, 0, 0, false, errors);
}
