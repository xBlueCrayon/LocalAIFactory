using System.Security.Cryptography;
using System.Text;
using LocalAIFactory.Core.Enums;

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

    // KE-008: per-symbol locus, the natural extension of FileKey foreseen in v1. Kind + FullName + a
    // signature hash uniquely identify a symbol within a file regardless of its position, so the key
    // survives edits elsewhere in the file (convergent re-extraction keeps the symbol's Uid). The
    // signature is hashed to keep the canonical string bounded for long generic parameter lists.
    public static string SymbolKey(int? projectId, string? relativePath, CodeSymbolKind kind, string fullName, string? signature)
    {
        var path = (relativePath ?? "").Replace('\\', '/').Trim().ToLowerInvariant();
        var sigHash = string.IsNullOrEmpty(signature)
            ? ""
            : Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(signature)));
        var canonical = $"v1|proj:{projectId ?? 0}|type:symbol|path:{path}|kind:{(int)kind}|sym:{fullName}|sig:{sigHash}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(bytes);
    }

    // KE-009: object-scoped identity for a database object — deliberately path-INDEPENDENT, unlike SymbolKey.
    // A logical object (dbo.Account) must converge to one identity no matter which script file CREATEs or
    // ALTERs it, so the key is built from the canonical database/schema/object[/column] + kind, never the
    // file path. This is what lets KE-010 edges, KE-011 retrieval and Knowledge Packs survive re-import.
    public static string SchemaObjectKey(int? projectId, string? database, string? schema, string objectName, string? column, CodeSymbolKind kind)
    {
        var db = NormalizeSqlIdentifier(database);
        var sch = NormalizeSqlIdentifier(schema);
        if (string.IsNullOrEmpty(sch)) sch = "dbo"; // unqualified objects bind to the default schema
        var obj = NormalizeSqlIdentifier(objectName);
        var col = NormalizeSqlIdentifier(column);
        var canonical = $"v1|proj:{projectId ?? 0}|type:schemaobject|db:{db}|schema:{sch}|obj:{obj}|col:{col}|kind:{(int)kind}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return Convert.ToHexStringLower(bytes);
    }

    // KE-009: kind-agnostic canonical "[db.]schema.object" join key for a referenced object. A name in a
    // FROM/JOIN/FK can resolve to a table or a view, so the reference key omits kind; KE-010 disambiguates
    // when it resolves the edge. Bounded plain text (not hashed) so it equi-joins cheaply and stays legible.
    public static string ReferencedObjectKey(string? database, string? schema, string objectName)
    {
        var db = NormalizeSqlIdentifier(database);
        var sch = NormalizeSqlIdentifier(schema);
        if (string.IsNullOrEmpty(sch)) sch = "dbo";
        var obj = NormalizeSqlIdentifier(objectName);
        return string.IsNullOrEmpty(db) ? $"{sch}.{obj}" : $"{db}.{sch}.{obj}";
    }

    // Canonicalize a SQL identifier: strip [brackets]/"quotes", trim, fold case. Matches the convention used
    // by both SchemaObjectKey and ReferencedObjectKey so a definition and a reference to the same object agree.
    public static string NormalizeSqlIdentifier(string? identifier)
        => (identifier ?? "")
            .Replace("[", "").Replace("]", "").Replace("\"", "").Replace("`", "")
            .Trim()
            .ToLowerInvariant();
}
