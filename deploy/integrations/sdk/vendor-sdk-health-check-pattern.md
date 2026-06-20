# Vendor-SDK Health-Check Pattern

A vendor SDK is an **optional, possibly-absent** dependency: the native library may not be
installed, the license may be expired, or the engine may fail to initialize. The platform
must detect this and **degrade gracefully**, never crash. This is the role of `IsAvailable`
and `Version` on `IVendorSdkAdapter`.

## The probe: `IsAvailable` + `Version`

```csharp
public interface IVendorSdkAdapter
{
    string Name { get; }
    string Version { get; }    // loaded engine version (or a sentinel when absent)
    bool IsAvailable { get; }  // native lib present, license valid, engine init OK
    SdkResult Process(string input);
}
```

A real adapter's `IsAvailable` should reflect the things that actually break an SDK:

- the native DLL / COM server is present, loadable, and the **right bitness**;
- the **license** is valid and not expired;
- the engine **initializes** without error;
- (optionally) the loaded **version** meets the minimum the adapter supports.

`Version` reports the actually-loaded engine version (from the SDK's own version API), so
logs and operators can see what is running — not a hard-coded guess.

## Fail closed

The availability gate lives in `RetryingSdkAdapter`:

```csharp
if (!_inner.IsAvailable)
    return new SdkResult(false, null, "SDK_UNAVAILABLE", 0);  // never attempted
```

- If the SDK is unavailable, the call is **not attempted** — it returns `SDK_UNAVAILABLE` with
  `Attempts == 0`. This is **fail-closed**: an absent/unlicensed engine produces a clear,
  handled outcome, not a partial result or a crash.
- The tested behavior is asserted in `VendorSdkAdapterTests`
  (`Unavailable_sdk_is_handled_not_attempted`).

## Mock for CI

In tests and dev, `MockVendorSdkAdapter` lets you force availability and version:

```csharp
new MockVendorSdkAdapter(available: false);               // simulate missing SDK
new MockVendorSdkAdapter(version: "2.3.1");               // simulate a specific engine
```

So CI validates both the available and unavailable paths **with no native dependency** on the
build agents.

## Degrade gracefully

When `IsAvailable` is false, the surrounding feature should:

- continue to start and run (the app must never fail to boot because an optional SDK is
  missing — consistent with MSSQL-only mode);
- surface a clear status ("OCR engine unavailable") rather than a stack trace;
- route affected work to quarantine/retry-later instead of dropping it;
- log the condition (and the `Version`/probe detail) for operators.

## Probing cost & caching

- Probing can be expensive (loading a DLL, contacting a license server). **Cache** the
  availability result and refresh it on a timer or on first failure — do not probe on every
  `Process` call.
- This mirrors the platform's existing approach of reading health from a cached snapshot
  rather than calling external services on the request path.

## Summary

| Signal | Meaning | Outcome |
|--------|---------|---------|
| `IsAvailable == true` | engine present + licensed + init OK | `Process` runs (with retry) |
| `IsAvailable == false` | missing / unlicensed / wrong version | `SDK_UNAVAILABLE`, not attempted |
| `Version` | loaded engine version | logged; min-version gate |
