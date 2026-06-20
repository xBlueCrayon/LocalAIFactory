using LocalAIFactory.Core.Abstractions;
using LocalAIFactory.Ingestion.Integrations.Sdk;
using Xunit;

namespace LocalAIFactory.Tests;

// R2-ACC-INDUSTRIAL: vendor-SDK integration boundary — mockable, with retry/backoff and stable error mapping,
// and an availability gate so a missing native/COM SDK is handled, not crashed.
public class VendorSdkAdapterTests
{
    [Fact]
    public void Mock_adapter_succeeds_and_reports_version()
    {
        var a = new MockVendorSdkAdapter(version: "2.3.1");
        Assert.Equal("2.3.1", a.Version);
        Assert.True(a.IsAvailable);
        var r = a.Process("doc");
        Assert.True(r.Success);
        Assert.Equal("processed:doc", r.Output);
    }

    [Fact]
    public void Retry_succeeds_after_transient_failures()
    {
        var inner = new MockVendorSdkAdapter(failFirst: 2);                 // fails twice, then succeeds
        var r = new RetryingSdkAdapter(inner, new SdkRetryPolicy(MaxAttempts: 3)).Process("x");
        Assert.True(r.Success);
        Assert.Equal(3, r.Attempts);                                       // succeeded on the 3rd attempt
    }

    [Fact]
    public void Retry_exhaustion_maps_to_stable_error_code()
    {
        var inner = new MockVendorSdkAdapter(failFirst: 99);               // always fails
        var r = new RetryingSdkAdapter(inner, new SdkRetryPolicy(MaxAttempts: 3)).Process("x");
        Assert.False(r.Success);
        Assert.Equal("MAX_RETRIES_EXCEEDED", r.ErrorCode);                 // deterministic mapping for quarantine
        Assert.Equal(3, r.Attempts);
    }

    [Fact]
    public void Unavailable_sdk_is_handled_not_attempted()
    {
        var inner = new MockVendorSdkAdapter(available: false);            // native lib / license missing
        var r = new RetryingSdkAdapter(inner).Process("x");
        Assert.False(r.Success);
        Assert.Equal("SDK_UNAVAILABLE", r.ErrorCode);
        Assert.Equal(0, r.Attempts);                                       // never attempted
    }
}
