// R2-ACC-INDUSTRIAL: SAMPLE — mock adapter + retry wrapper. The canonical, TESTED version lives at
// src/LocalAIFactory.Ingestion/Integrations/Sdk/VendorSdkAdapters.cs (see VendorSdkAdapterTests). A real
// adapter implements IVendorSdkAdapter over the vendor's native/COM API; tests use the mock so no native
// dependency is required to validate the integration logic (retry/backoff, error mapping, availability gate).
using LocalAIFactory.Core.Abstractions;

namespace LocalAIFactory.Samples.Sdk
{
    // Deterministic mock: fails transiently `failFirst` times, then succeeds.
    public sealed class MockVendorSdkAdapter : IVendorSdkAdapter
    {
        private readonly int _failFirst; private int _calls;
        public MockVendorSdkAdapter(int failFirst = 0, string version = "1.0.0", bool available = true)
        { _failFirst = failFirst; Version = version; IsAvailable = available; }
        public string Name => "MockVendorSdk";
        public string Version { get; }
        public bool IsAvailable { get; }
        public SdkResult Process(string input)
        {
            _calls++;
            return _calls <= _failFirst
                ? new SdkResult(false, null, "TRANSIENT", _calls)
                : new SdkResult(true, $"processed:{input}", null, _calls);
        }
    }

    // Retry/backoff + stable error mapping + availability gate around ANY adapter. Never throws on vendor failure.
    public sealed class RetryingSdkAdapter : IVendorSdkAdapter
    {
        private readonly IVendorSdkAdapter _inner; private readonly SdkRetryPolicy _policy;
        public RetryingSdkAdapter(IVendorSdkAdapter inner, SdkRetryPolicy? policy = null)
        { _inner = inner; _policy = policy ?? new SdkRetryPolicy(); }
        public string Name => _inner.Name;
        public string Version => _inner.Version;
        public bool IsAvailable => _inner.IsAvailable;
        public SdkResult Process(string input)
        {
            if (!_inner.IsAvailable) return new SdkResult(false, null, "SDK_UNAVAILABLE", 0);
            SdkResult last = new(false, null, "NOT_RUN", 0);
            for (int attempt = 1; attempt <= _policy.MaxAttempts; attempt++)
            {
                last = _inner.Process(input);
                if (last.Success) return last with { Attempts = attempt };
            }
            return new SdkResult(false, null, "MAX_RETRIES_EXCEEDED", _policy.MaxAttempts);
        }
    }
}
