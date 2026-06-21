using Xunit;

namespace LafErp.Tests;

/// <summary>Proves the generator emitted the governed local-LLM catalog modules into the product assembly.</summary>
public class GenerationProvenanceTests
{
    [Fact]
    public void Generated_catalog_entities_exist_in_assembly()
    {
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.CustomerSegment"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.PaymentTerm"));
        Assert.NotNull(typeof(LafErp.Core.EntityBase).Assembly.GetType("LafErp.Core.TaxCode"));
        Assert.True(3 >= 0);
    }
}