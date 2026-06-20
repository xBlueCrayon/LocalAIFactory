using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Ingestion.Integrations.Sdk;

// R2-ACC-INDUSTRIAL: a deterministic MOCK vendor-SDK adapter for tests/dev, plus a retry wrapper that adds
// retry/backoff and stable error mapping around ANY adapter. This is the boundary a real native/COM SDK
// (e.g. an OCR engine) sits behind — the platform depends only on IVendorSdkAdapter, never the vendor directly.

// Deterministic mock: fails (transiently) for the first `failFirst` calls, then succeeds. Used to exercise the
// retry wrapper without a native dependency.
public sealed class MockVendorSdkAdapter : IVendorSdkAdapter
{
    private readonly int _failFirst;
    private int _calls;
    public MockVendorSdkAdapter(int failFirst = 0, string version = "1.0.0", bool available = true)
    { _failFirst = failFirst; Version = version; IsAvailable = available; }

    public string Name => "MockVendorSdk";
    public string Version { get; }
    public bool IsAvailable { get; }

    public SdkResult Process(string input)
    {
        _calls++;
        if (_calls <= _failFirst) return new SdkResult(false, null, "TRANSIENT", _calls);
        return new SdkResult(true, $"processed:{input}", null, _calls);
    }
}

// Wraps an adapter with availability detection, retry/backoff, and deterministic error mapping. Never throws on
// a vendor failure — it returns a mapped SdkResult so callers handle integration failures uniformly.
public sealed class RetryingSdkAdapter : IVendorSdkAdapter
{
    private readonly IVendorSdkAdapter _inner;
    private readonly SdkRetryPolicy _policy;
    public RetryingSdkAdapter(IVendorSdkAdapter inner, SdkRetryPolicy? policy = null)
    { _inner = inner; _policy = policy ?? new SdkRetryPolicy(); }

    public string Name => _inner.Name;
    public string Version => _inner.Version;
    public bool IsAvailable => _inner.IsAvailable;

    public SdkResult Process(string input)
    {
        // Availability boundary: do not even attempt if the SDK is not available (license/native lib missing).
        if (!_inner.IsAvailable) return new SdkResult(false, null, "SDK_UNAVAILABLE", 0);

        SdkResult last = new(false, null, "NOT_RUN", 0);
        for (int attempt = 1; attempt <= _policy.MaxAttempts; attempt++)
        {
            try { last = _inner.Process(input); }
            catch (Exception ex) { last = new SdkResult(false, null, "SDK_EXCEPTION:" + ex.GetType().Name, attempt); }
            if (last.Success) return last with { Attempts = attempt };
            if (_policy.BaseDelayMs > 0 && attempt < _policy.MaxAttempts)
                Thread.Sleep(_policy.BaseDelayMs * attempt); // linear backoff (exponential is a config option)
        }
        // Exhausted retries: map to a stable terminal error code for the caller's quarantine/audit path.
        return new SdkResult(false, null, MapTerminal(last.ErrorCode), _policy.MaxAttempts);
    }

    private static string MapTerminal(string? code) => code switch
    {
        null or "" => "MAX_RETRIES_EXCEEDED",
        "TRANSIENT" => "MAX_RETRIES_EXCEEDED",
        _ => code
    };
}
