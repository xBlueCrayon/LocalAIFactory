# The Adapter / Wrapper Pattern for Vendor SDKs

LocalAIFactory wraps every vendor SDK behind a small interface and a retry wrapper. This
document explains the pattern and why it is worth the indirection.

## The two layers

```
caller  ->  IVendorSdkAdapter (the seam)
                |
                +-- RealVendorAdapter   (native/COM call; the only place that touches the vendor)
                +-- MockVendorSdkAdapter (deterministic; for tests/dev)
                +-- RetryingSdkAdapter   (decorator: availability gate + retry + error mapping)
```

- **Adapter** — translates the platform's `Process(string input) -> SdkResult` into the
  vendor's native/COM call, and reports `IsAvailable`/`Version`. It is the *only* code that
  knows the vendor exists.
- **Wrapper (decorator)** — `RetryingSdkAdapter` is itself an `IVendorSdkAdapter` that takes
  an inner adapter and adds cross-cutting behavior. Because it implements the same interface,
  callers can't tell whether they hold a raw adapter or a wrapped one.

```csharp
IVendorSdkAdapter ocr = new RetryingSdkAdapter(
    new RealOcrAdapter(/* native handle */),
    new SdkRetryPolicy(MaxAttempts: 3, BaseDelayMs: 200));

SdkResult r = ocr.Process(documentPath);   // availability + retry + mapping, all transparent
```

## Why swappable

The caller depends on `IVendorSdkAdapter`, never on the vendor type. Replacing the engine —
or running a different one per environment — is a one-line wiring change in DI. No business
code changes. This matters in a long-lived banking estate where vendors and licenses change.

## Why testable

`MockVendorSdkAdapter` implements the same interface deterministically (fail `failFirst`
times, then succeed; configurable `IsAvailable`/`Version`). That lets the **integration
logic** — retry counts, error codes, the availability gate — be unit-tested with **no native
dependency**, so the suite runs in CI on any machine. The canonical tests
(`VendorSdkAdapterTests`) do exactly this.

## Why no hard native dependency

- The platform assembly references only `IVendorSdkAdapter`. The real adapter (with its
  P/Invoke or COM references) is loaded only when actually configured.
- If the SDK is absent or unlicensed, `IsAvailable` is `false` and `RetryingSdkAdapter`
  returns `SDK_UNAVAILABLE` instead of crashing. The app still starts and runs (MSSQL-only
  mode is unaffected).
- This honors the project rule that optional integrations degrade gracefully.

## Separation of concerns

| Concern | Lives in |
|---------|----------|
| Vendor-specific call | the real adapter |
| Availability/version reporting | the adapter (`IsAvailable`/`Version`) |
| Retry/backoff | `RetryingSdkAdapter` |
| Error mapping to stable codes | `RetryingSdkAdapter` (see `retry-error-mapping.md`) |
| Test doubles | `MockVendorSdkAdapter` |

Keep each concern in its layer. Do not let vendor types or native error structs leak past the
adapter — callers see only `SdkResult` and stable string error codes.
