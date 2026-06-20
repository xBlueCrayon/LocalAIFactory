# COM SDK Boundary

When the real adapter wraps a **COM** SDK (an in-process or out-of-process COM server,
common for older Windows document/imaging engines), the adapter must manage COM's apartment,
lifetime, and isolation rules. The platform above still sees only `IVendorSdkAdapter`.

## Apartment threading

- Most legacy COM objects are **STA** (single-threaded apartment). They must be created and
  called on a thread whose apartment matches, and ideally always the **same** thread.
- Do **not** call an STA COM object from arbitrary thread-pool threads — cross-apartment calls
  are marshalled (slow) or fail outright, and concurrent calls corrupt state.
- Practical pattern: run the COM object on a **dedicated STA worker thread** (or out-of-process
  host) and funnel all `Process` calls to it via a queue. The adapter exposes the normal
  `IVendorSdkAdapter` synchronous contract; the STA serialization is hidden inside.
- If (and only if) the component is documented as MTA/free-threaded may you call it from
  multiple threads.

## Lifetime & release

- COM uses reference counting. Release every interface pointer you obtain, deterministically.
  In .NET use `Marshal.ReleaseComObject` / `FinalReleaseComObject` in a `finally`, or wrap the
  object and release on `Dispose`.
- Pair `CoCreate`/activation with release; do not rely on the GC/finalizer timing for COM
  cleanup — it leaks the server and can pin the apartment.
- Track creation state so `IsAvailable` reflects whether the server is actually live.

## Crash & fault isolation

- An in-process COM server shares your process — a bad call can corrupt or crash it. For
  fragile servers, host the COM object **out-of-process** (a separate surrogate/worker
  process the adapter talks to over IPC), so a fault kills only the worker. The adapter then
  reports `SDK_EXCEPTION`/`SDK_UNAVAILABLE`, recycles the worker, and continues.
- Catch and map `COMException` (and HRESULT failures) to stable codes — `RetryingSdkAdapter`
  turns these into `SDK_EXCEPTION:<type>` and never rethrows to callers.
- Enforce a timeout on the worker call so a hung COM call cannot stall the platform.

## Version detection

- Read the version from the component's own API (a version property/method) where available,
  or from the registered type-library/class info. Report it as `Version`.
- Confirm the COM server is **registered** and of the expected **bitness** (x64 host needs an
  x64-registered server, or an out-of-process surrogate to bridge bitness).
- Gate on a minimum supported version; below it, set `IsAvailable = false`.

## Availability

- `IsAvailable` should reflect: the CLSID/ProgID is registered, activation succeeds, license
  valid, and (if applicable) the STA worker is healthy. Probe cheaply and cache the result —
  see `vendor-sdk-health-check-pattern.md`.
- A missing registration or activation failure yields `IsAvailable = false`, never a crash at
  startup.

## Checklist

- [ ] Apartment model identified (usually STA); dedicated STA worker or out-of-process host.
- [ ] All calls serialized onto the correct apartment thread.
- [ ] Every interface pointer released deterministically (`finally`/`Dispose`).
- [ ] `COMException`/HRESULT mapped to stable error codes; timeout enforced.
- [ ] Out-of-process surrogate for fragile servers and for bitness bridging.
- [ ] `Version` from the component; registration + bitness verified; min-version gate.
- [ ] `IsAvailable` covers registration + activation + license + worker health.
