using System.Text;

namespace LocalAIFactory.Ingestion.Imports;

// R2-P0C: robust, honest decoding of imperfect real-world files. A bad file must never crash ingestion, and a
// non-UTF-8 file must never be silently mojibake'd — when we fall back, we say so (surfaced in the gap report).
public static class RobustText
{
    // Content-based binary detection (independent of file extension): a NUL byte in the first 8 KB is the
    // classic, reliable "this is not text" signal. Catches binaries mislabeled with a source extension.
    public static bool IsBinary(ReadOnlySpan<byte> bytes)
    {
        var n = Math.Min(bytes.Length, 8192);
        for (int i = 0; i < n; i++)
            if (bytes[i] == 0) return true;
        return false;
    }

    // Decode bytes to text with BOM detection, then strict UTF-8, then a lossless Windows-1252/Latin-1
    // fallback. `note` is non-null when a fallback was used so the import can record it honestly (never silent).
    public static string Decode(byte[] bytes, out string? note)
    {
        note = null;
        if (bytes.Length == 0) return "";

        // Byte-order marks.
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        if (bytes.Length >= 4 && bytes[0] == 0xFF && bytes[1] == 0xFE && bytes[2] == 0 && bytes[3] == 0)
            return Encoding.UTF32.GetString(bytes, 4, bytes.Length - 4);
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);       // UTF-16 LE
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2); // UTF-16 BE

        // No BOM: try strict UTF-8 (the overwhelming common case for modern source).
        try { return new UTF8Encoding(false, throwOnInvalidBytes: true).GetString(bytes); }
        catch
        {
            // Not valid UTF-8 (legacy Windows-1252/Latin-1, etc.). Latin-1 is a lossless 1:1 byte map and
            // never throws — decode best-effort and flag it so the file is not silently misread.
            note = "non-UTF-8 content decoded as Latin-1 (best effort)";
            return Encoding.Latin1.GetString(bytes);
        }
    }
}
