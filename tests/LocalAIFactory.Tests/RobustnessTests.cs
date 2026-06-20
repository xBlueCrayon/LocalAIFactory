using System.Text;
using LocalAIFactory.Ingestion.Imports;
using LocalAIFactory.Ingestion.Symbols;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-P0C: ingestion robustness. A bad repository must never crash the platform. These regression tests pin
// the real failure modes we hardened: content-binary detection, honest (never-silent) decoding of imperfect
// encodings, and error-tolerant parsers that yield what they can instead of throwing.
public class RobustnessTests
{
    // ---- content-based binary detection (an extension can lie; a NUL byte cannot) ----

    [Fact]
    public void IsBinary_detects_null_byte_in_otherwise_textual_content()
    {
        var bytes = Encoding.ASCII.GetBytes("public class A {}").ToList();
        bytes.Insert(5, 0x00); // a binary mislabeled .cs
        Assert.True(RobustText.IsBinary(bytes.ToArray()));
    }

    [Fact]
    public void IsBinary_false_for_clean_text_and_empty()
    {
        Assert.False(RobustText.IsBinary(Encoding.UTF8.GetBytes("namespace N { class C {} }")));
        Assert.False(RobustText.IsBinary(Array.Empty<byte>()));
    }

    [Fact]
    public void IsBinary_only_scans_first_8kb_a_null_byte_past_the_window_is_not_flagged()
    {
        var bytes = new byte[10_000];
        for (int i = 0; i < bytes.Length; i++) bytes[i] = (byte)'a';
        bytes[9_000] = 0x00; // beyond the 8 KB sniff window
        Assert.False(RobustText.IsBinary(bytes));
    }

    // ---- honest decoding: BOMs, strict UTF-8, and a flagged Latin-1 fallback ----

    [Fact]
    public void Decode_empty_returns_empty_no_note()
    {
        Assert.Equal("", RobustText.Decode(Array.Empty<byte>(), out var note));
        Assert.Null(note);
    }

    [Fact]
    public void Decode_plain_ascii_no_note()
    {
        var text = RobustText.Decode(Encoding.ASCII.GetBytes("SELECT 1;"), out var note);
        Assert.Equal("SELECT 1;", text);
        Assert.Null(note);
    }

    [Fact]
    public void Decode_strips_utf8_bom()
    {
        var bytes = new byte[] { 0xEF, 0xBB, 0xBF }.Concat(Encoding.UTF8.GetBytes("héllo")).ToArray();
        var text = RobustText.Decode(bytes, out var note);
        Assert.Equal("héllo", text);
        Assert.Null(note); // valid UTF-8 with BOM is not a fallback
    }

    [Fact]
    public void Decode_handles_utf16_le_bom()
    {
        var bytes = Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("café")).ToArray();
        Assert.Equal("café", RobustText.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_handles_utf16_be_bom()
    {
        var bytes = Encoding.BigEndianUnicode.GetPreamble().Concat(Encoding.BigEndianUnicode.GetBytes("café")).ToArray();
        Assert.Equal("café", RobustText.Decode(bytes, out _));
    }

    [Fact]
    public void Decode_valid_multibyte_utf8_without_bom_has_no_note()
    {
        var text = RobustText.Decode(Encoding.UTF8.GetBytes("// café — naïve"), out var note);
        Assert.Equal("// café — naïve", text);
        Assert.Null(note);
    }

    [Fact]
    public void Decode_non_utf8_bytes_fall_back_to_latin1_and_record_a_note()
    {
        // 0xE9 is 'é' in Windows-1252/Latin-1 but an invalid lone UTF-8 lead byte here.
        var bytes = new byte[] { (byte)'c', (byte)'a', (byte)'f', 0xE9 };
        var text = RobustText.Decode(bytes, out var note);
        Assert.Equal("café", text);          // losslessly recovered
        Assert.NotNull(note);                // and never silent
        Assert.Contains("Latin-1", note);
    }

    // ---- parsers are error-tolerant: garbage in, no throw, best-effort symbols out ----

    [Fact]
    public void CSharp_extractor_does_not_throw_on_truncated_source()
    {
        var ex = Record.Exception(() => new CSharpSymbolExtractor().Extract("public class Half { public void M( { if (x"));
        Assert.Null(ex); // Roslyn is error-tolerant; a non-compiling file still parses
    }

    [Fact]
    public void CSharp_extractor_does_not_throw_on_arbitrary_latin1_garbage()
    {
        // bytes a binary would produce, decoded the way the pipeline would decode them
        var garbage = Encoding.Latin1.GetString(Enumerable.Range(0, 4096).Select(i => (byte)((i * 37) % 256)).ToArray());
        var ex = Record.Exception(() => new CSharpSymbolExtractor().Extract(garbage));
        Assert.Null(ex);
    }

    [Fact]
    public void CSharp_extractor_still_recovers_symbols_from_partially_broken_file()
    {
        var res = new CSharpSymbolExtractor().Extract(
            "namespace N { public class Good { public int X { get; set; } } public class Broken { void M( ");
        Assert.Contains(res.Symbols, s => s.Name == "Good"); // the well-formed declaration survives
    }

    [Fact]
    public void Sql_extractor_does_not_throw_on_garbage_or_malformed_batches()
    {
        var ex = Record.Exception(() => new TSqlSchemaExtractor().Extract(
            "GO\n)(*&^ not sql at all\nGO\nCREATE TABLE dbo.T (Id int"));
        Assert.Null(ex);
    }

    [Fact]
    public void Sql_extractor_recovers_good_batch_despite_a_malformed_neighbour()
    {
        var res = new TSqlSchemaExtractor().Extract(
            "this is not sql\nGO\nCREATE TABLE dbo.Account (Id INT NOT NULL, Balance DECIMAL(18,2));\nGO\n@@@ broken");
        Assert.Contains(res.Symbols, s => s.Name.Contains("Account", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Both_extractors_return_empty_not_throw_on_empty_and_whitespace()
    {
        Assert.Empty(new CSharpSymbolExtractor().Extract("   ").Symbols);
        Assert.Empty(new TSqlSchemaExtractor().Extract("\n\t  ").Symbols);
    }
}
