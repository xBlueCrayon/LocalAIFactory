// R2-ACC-INDUSTRIAL: SAMPLE — the vendor-SDK boundary the platform actually uses. The canonical, TESTED version
// lives at src/LocalAIFactory.Core/Abstractions/ISdkIntegration.cs. The platform depends ONLY on this interface,
// never on a native/COM vendor SDK directly, so the vendor is swappable and mockable.
namespace LocalAIFactory.Core.Abstractions
{
    public sealed record SdkResult(bool Success, string? Output, string? ErrorCode, int Attempts);

    public interface IVendorSdkAdapter
    {
        string Name { get; }
        string Version { get; }   // SDK version detection (native/COM version of the loaded engine)
        bool IsAvailable { get; } // health/availability (native lib present, license valid)
        SdkResult Process(string input);
    }

    public sealed record SdkRetryPolicy(int MaxAttempts = 3, int BaseDelayMs = 0);
}
