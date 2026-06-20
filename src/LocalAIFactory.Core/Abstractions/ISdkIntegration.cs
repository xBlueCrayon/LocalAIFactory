namespace LocalAIFactory.Core.Abstractions;

// R2-ACC-INDUSTRIAL: a vendor-SDK integration boundary (e.g. an OCR/document SDK such as a Parascript-style
// engine) behind a clean, mockable interface — so the platform never hard-depends on a native/COM vendor SDK,
// can be tested with a mock, and wraps the vendor call in retry + deterministic error mapping. No vendor SDK is
// bundled; this is the seam an adapter implements.
public sealed record SdkResult(bool Success, string? Output, string? ErrorCode, int Attempts);

public interface IVendorSdkAdapter
{
    string Name { get; }
    string Version { get; }      // SDK version detection (a real adapter reports the loaded native/COM version)
    bool IsAvailable { get; }    // health/availability detection (native lib present, license valid, etc.)
    SdkResult Process(string input);
}

// Retry/backoff policy. BaseDelayMs = 0 keeps unit tests instant; production uses a real backoff.
public sealed record SdkRetryPolicy(int MaxAttempts = 3, int BaseDelayMs = 0);
