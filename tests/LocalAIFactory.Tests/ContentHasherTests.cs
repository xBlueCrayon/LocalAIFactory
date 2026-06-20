using LocalAIFactory.Data.Backbone;
using Xunit;

namespace LocalAIFactory.Tests;

public class ContentHasherTests
{
    private readonly ContentHasher _h = new();

    [Fact]
    public void Compute_is_deterministic_and_64_lowercase_hex()
    {
        var a = _h.Compute("hello world");
        var b = _h.Compute("hello world");
        Assert.Equal(a, b);
        Assert.Equal(64, a.Length);
        Assert.Matches("^[0-9a-f]{64}$", a);
    }

    [Fact]
    public void Compute_normalizes_line_endings_and_trims()
    {
        Assert.Equal(_h.Compute("line1\nline2"), _h.Compute("  line1\r\nline2  "));
    }

    [Fact]
    public void Different_content_produces_different_hash()
    {
        Assert.NotEqual(_h.Compute("a"), _h.Compute("b"));
    }

    [Fact]
    public void Null_and_empty_hash_equally_but_are_not_the_empty_string()
    {
        Assert.Equal(_h.Compute(null), _h.Compute(""));
        Assert.NotEqual("", _h.Compute(null));
    }
}
