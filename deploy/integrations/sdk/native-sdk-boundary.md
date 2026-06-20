# Native (P/Invoke) SDK Boundary

When the real adapter calls a **native** library (C/C++ DLL via P/Invoke), the adapter is the
containment boundary for everything that can go wrong at the managed/native edge. This
document lists what that adapter must handle. The platform above it still sees only
`IVendorSdkAdapter` / `SdkResult`.

## Marshalling

- Define `[DllImport]` (or `LibraryImport`) signatures that exactly match the native ABI —
  calling convention, struct layout (`[StructLayout]`), and string encoding (ANSI vs UTF-16).
- Marshal strings explicitly; do not assume the default. Free any native-allocated buffers
  the API hands back, using the vendor's documented free function.
- Keep marshalling **inside the adapter**. Native handles, pointers, and structs must never
  escape past `IVendorSdkAdapter` — convert to managed types and return `SdkResult`.
- Validate inputs before crossing the boundary (length, null, encoding) — a bad pointer or
  oversized buffer can corrupt the process, not just throw.

## Lifetime & resources

- Wrap native handles in a `SafeHandle` so they are released deterministically even on
  exceptions, and implement `IDisposable` on the adapter.
- Initialize the engine once and reuse it where the SDK supports it; pair every `init`/`open`
  with a `shutdown`/`close`. Track init state so `IsAvailable` reflects reality.
- Be explicit about thread-safety: if the native engine is not thread-safe, serialize calls
  (a lock or a single-threaded worker) — do not call it concurrently and hope.

## Crash isolation

A native crash (access violation, stack corruption) can take down the **whole process** —
managed `try/catch` does **not** catch a true native crash. Mitigations, in order of strength:

1. **Out-of-process host (strongest).** Run the SDK in a separate worker process; the adapter
   talks to it over IPC (pipe/socket/queue). A worker crash kills only the worker — the main
   app survives, marks the call failed (`SDK_EXCEPTION`/`SDK_UNAVAILABLE`), restarts the
   worker, and continues. Recommended for fragile or untrusted native engines.
2. **Input hardening.** Validate and bound every input; reject pathological documents before
   they reach the engine.
3. **Managed-exception mapping.** For failures that *do* surface as managed exceptions, the
   `RetryingSdkAdapter` already maps them to `SDK_EXCEPTION:<type>` and never rethrows.

The out-of-process option also gives you a clean place to enforce timeouts and memory caps.

## Version detection

- Report `Version` from the SDK's own version API (a `GetVersion`-style export), not a
  hard-coded constant — so logs and the health probe show the actually-loaded engine.
- On init, check the version meets the minimum the adapter supports; if not, set
  `IsAvailable = false` so the platform degrades instead of calling an unsupported engine.

## Availability

- `IsAvailable` should reflect: native DLL present and loadable (right bitness — x64 vs x86),
  license valid, engine initialized. Probe cheaply; cache the result (see
  `vendor-sdk-health-check-pattern.md`).
- A missing or wrong-architecture DLL must yield `IsAvailable = false`, never a startup crash.

## Checklist

- [ ] ABI-correct P/Invoke signatures; explicit string marshalling; buffers freed.
- [ ] Native types never escape the adapter.
- [ ] `SafeHandle` + `IDisposable`; init/shutdown paired; state tracked.
- [ ] Concurrency model decided (serialize if not thread-safe).
- [ ] Crash isolation chosen (out-of-process host for fragile engines).
- [ ] `Version` from the SDK; minimum-version gate.
- [ ] `IsAvailable` covers presence + bitness + license + init.
