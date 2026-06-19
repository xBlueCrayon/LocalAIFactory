using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Workspaces;

// Read-only view over extracted workspaces. No writes in Phase 1.
public sealed class WorkspaceService : IWorkspaceService
{
    public bool IsReadOnly => true;

    public IReadOnlyList<string> ListFiles(string root, string searchPattern = "*.*")
    {
        if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root)) return Array.Empty<string>();
        try
        {
            return Directory.EnumerateFiles(root, searchPattern, SearchOption.AllDirectories).Take(50000).ToList();
        }
        catch { return Array.Empty<string>(); }
    }

    public string? ReadText(string path)
    {
        try { return File.Exists(path) ? File.ReadAllText(path) : null; }
        catch { return null; }
    }
}
