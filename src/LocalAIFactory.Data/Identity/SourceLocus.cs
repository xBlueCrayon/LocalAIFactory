using System.Security.Cryptography;
using System.Text;

namespace LocalAIFactory.Data.Identity;

// KE-004: builds the stable per-file source-locus key. The canonical string is versioned ("v1|...") and
// structured ("proj|type|path") so it can naturally extend to per-symbol granularity in M2 (KE-008) by
// adding "sym:..." segments — without changing existing file-level keys. Hashed to a fixed 64-char value
// so it indexes efficiently regardless of path length. The key is instance-local and regenerable; the
// portable identity remains the item's Uid.
public static class SourceLocus
{
    public static string FileKey(int? projectId, string? relativePath)
    {
        var path = (relativePath ?? "").Replace('\\', '/').Trim().ToLowerInvariant();
        var canonical = $"v1|proj:{projectId ?? 0}|type:file|path:{path}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(bytes);
    }
}
