using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Core.Dtos;

namespace LocalAIFactory.Workspaces.Management;

// Simple line-based diff (LCS) sufficient for previewing proposed changes in Phase 1.
public sealed class DiffService : IDiffService
{
    public DiffResult Diff(string? previous, string? current)
    {
        var a = Split(previous);
        var b = Split(current);
        var result = new DiffResult();

        // LCS table
        int n = a.Length, m = b.Length;
        var lcs = new int[n + 1, m + 1];
        for (int i = n - 1; i >= 0; i--)
            for (int j = m - 1; j >= 0; j--)
                lcs[i, j] = a[i] == b[j] ? lcs[i + 1, j + 1] + 1 : Math.Max(lcs[i + 1, j], lcs[i, j + 1]);

        int x = 0, y = 0;
        while (x < n && y < m)
        {
            if (a[x] == b[y]) { result.Lines.Add(new DiffLine(DiffOp.Unchanged, a[x])); x++; y++; }
            else if (lcs[x + 1, y] >= lcs[x, y + 1]) { result.Lines.Add(new DiffLine(DiffOp.Removed, a[x])); result.Removed++; x++; }
            else { result.Lines.Add(new DiffLine(DiffOp.Added, b[y])); result.Added++; y++; }
        }
        while (x < n) { result.Lines.Add(new DiffLine(DiffOp.Removed, a[x])); result.Removed++; x++; }
        while (y < m) { result.Lines.Add(new DiffLine(DiffOp.Added, b[y])); result.Added++; y++; }
        return result;
    }

    private static string[] Split(string? s)
        => string.IsNullOrEmpty(s) ? Array.Empty<string>() : s.Replace("\r\n", "\n").Split('\n');
}
