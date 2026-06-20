using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Data.Backbone;

// KE-003: SHA-256 over normalized content. Line endings are unified and the text trimmed so that
// content which is equivalent across platforms / re-imports hashes identically (stable reconciliation).
public sealed class ContentHasher : IContentHasher
{
    public string Compute(string? content)
    {
        var normalized = (content ?? "").Replace("\r\n", "\n").Replace("\r", "\n").Trim();
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(normalized));
        return Convert.ToHexStringLower(bytes); // 64 lowercase hex chars
    }
}
