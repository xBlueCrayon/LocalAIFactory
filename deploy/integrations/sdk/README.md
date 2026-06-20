# Vendor-SDK Integration (deploy/integrations/sdk)

Some banking workloads need a **native or COM vendor SDK** — for example a document/OCR
engine. LocalAIFactory integrates such SDKs **behind a clean adapter boundary** so the
platform never hard-depends on the vendor, can be tested without the native library, and
handles availability, retry, and error mapping uniformly.

The platform depends **only** on the interface `IVendorSdkAdapter`. No vendor SDK is bundled;
this folder documents the seam an adapter implements.

## The boundary: `IVendorSdkAdapter`

```csharp
public sealed record SdkResult(bool Success, string? Output, string? ErrorCode, int Attempts);

public interface IVendorSdkAdapter
{
    string Name { get; }
    string Version { get; }    // reports the loaded native/COM engine version
    bool IsAvailable { get; }  // native lib present, license valid, etc.
    SdkResult Process(string input);
}

public sealed record SdkRetryPolicy(int MaxAttempts = 3, int BaseDelayMs = 0);
```

Two collaborators sit behind it:

- **A real adapter** implements `IVendorSdkAdapter` over the vendor's native/COM API.
- **`RetryingSdkAdapter`** wraps *any* adapter to add the availability gate, retry/backoff,
  and deterministic error mapping — and **never throws** on a vendor failure; it returns a
  mapped `SdkResult`.

## Canonical, tested code (depend on this, not the samples)

| Artifact | Path |
|----------|------|
| Interface + records | `src/LocalAIFactory.Core/Abstractions/ISdkIntegration.cs` |
| Mock + retry wrapper | `src/LocalAIFactory.Ingestion/Integrations/Sdk/VendorSdkAdapters.cs` |
| Tests | `tests/LocalAIFactory.Tests/VendorSdkAdapterTests.cs` |

The sample files in this folder (`sample-adapter-interface.cs`, `sample-mock-adapter.cs`) are
**illustrative copies**. The versions above are the ones under test and the ones to depend on.

## What the tests prove

`VendorSdkAdapterTests` asserts the integration contract:

- **Mock success** — a working adapter returns `Success` and reports its `Version`.
- **Retry after transient failures** — fails twice then succeeds on attempt 3 (`Attempts == 3`).
- **Retry exhaustion** — always-failing adapter maps to `MAX_RETRIES_EXCEEDED`.
- **Unavailable SDK** — `IsAvailable == false` returns `SDK_UNAVAILABLE` with `Attempts == 0`
  (never attempted).

## Why an adapter (and not a direct dependency)

- **Swappable** — replace the vendor without touching callers.
- **Testable** — the mock exercises retry/error/availability logic with no native dependency,
  so CI runs everywhere.
- **No hard native dependency** — if the SDK is absent, the app degrades gracefully
  (`SDK_UNAVAILABLE`) instead of failing to start. This keeps MSSQL-only mode intact.

## OCR-engine integration note (no vendor lock-in)

A native OCR/document SDK (conceptually, a Parascript-style engine) plugs in by implementing
`IVendorSdkAdapter`: `Process(input)` runs the OCR call, `IsAvailable` reflects the native
library + license, and `Version` reports the loaded engine version. This is a **conceptual**
placement of such an engine behind the adapter — there is **no compatibility claim**, no
bundled SDK, and no coupling to any product's API surface. Any compliant OCR/document engine
can sit behind the same seam.

## Further reading

- `sdk-wrapper-pattern.md` — the adapter/wrapper pattern and rationale.
- `native-sdk-boundary.md` — P/Invoke boundaries (marshalling, lifetime, crash isolation).
- `com-sdk-boundary.md` — COM boundaries (apartment threading, release, versioning).
- `vendor-sdk-health-check-pattern.md` — availability/version probing, fail-closed.
- `retry-error-mapping.md` — retry/backoff and the stable error codes.
