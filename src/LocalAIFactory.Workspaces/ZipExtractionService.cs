using System.IO.Compression;
using System.Text.RegularExpressions;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Workspaces;

// Safe ZIP extraction: zip-slip protection plus entry-count and size caps.
public sealed class ZipExtractionService : IZipExtractionService
{
    private const int MaxEntries = 20000;
    private const long MaxSingleFileBytes = 50L * 1024 * 1024;     // 50 MB
    private const long MaxTotalBytes = 1024L * 1024 * 1024;        // 1 GB uncompressed

    public async Task<string> ExtractAsync(string archiveFileName, byte[] zipBytes, string destinationRoot, CancellationToken ct = default)
    {
        Directory.CreateDirectory(destinationRoot);
        var safeName = Regex.Replace(Path.GetFileNameWithoutExtension(archiveFileName), "[^A-Za-z0-9_-]", "_");
        if (string.IsNullOrWhiteSpace(safeName)) safeName = "project";
        var stamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var root = Path.Combine(destinationRoot, $"{safeName}-{stamp}");
        Directory.CreateDirectory(root);
        var rootFull = Path.GetFullPath(root) + Path.DirectorySeparatorChar;

        using var ms = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(ms, ZipArchiveMode.Read);

        if (archive.Entries.Count > MaxEntries)
            throw new InvalidOperationException($"Archive has too many entries ({archive.Entries.Count} > {MaxEntries}).");

        long total = 0;
        foreach (var entry in archive.Entries)
        {
            ct.ThrowIfCancellationRequested();
            if (string.IsNullOrEmpty(entry.Name)) continue; // directory entry

            var destPath = Path.GetFullPath(Path.Combine(root, entry.FullName));
            if (!destPath.StartsWith(rootFull, StringComparison.Ordinal))
                continue; // zip-slip attempt: skip

            if (entry.Length > MaxSingleFileBytes) continue;
            total += entry.Length;
            if (total > MaxTotalBytes)
                throw new InvalidOperationException("Archive exceeds the maximum total uncompressed size.");

            Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
            await using var src = entry.Open();
            await using var dst = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await src.CopyToAsync(dst, ct);
        }

        return root;
    }
}
