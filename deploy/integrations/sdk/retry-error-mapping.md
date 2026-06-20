# Retry & Error Mapping

`RetryingSdkAdapter` wraps any `IVendorSdkAdapter` to add retry/backoff and to map every
outcome to a **stable, deterministic error code**. Callers never see vendor-specific
exceptions or HRESULTs — only `SdkResult` with a known `ErrorCode`. This is what makes the
quarantine/audit path uniform across different SDKs.

## The policy

```csharp
public sealed record SdkRetryPolicy(int MaxAttempts = 3, int BaseDelayMs = 0);
```

- `MaxAttempts` — total attempts (default 3).
- `BaseDelayMs` — `0` keeps unit tests instant; production sets a real delay.

## Backoff

The tested wrapper applies **linear** backoff between attempts:

```csharp
if (_policy.BaseDelayMs > 0 && attempt < _policy.MaxAttempts)
    Thread.Sleep(_policy.BaseDelayMs * attempt);   // delay grows: base, 2*base, ...
```

Exponential backoff is a documented config option (multiply instead of scale linearly); pick
per the engine's behavior. Keep delays bounded so a stuck engine cannot stall a batch.

## Transient vs terminal

- **Transient** — a failure worth retrying (the adapter returns `ErrorCode == "TRANSIENT"`,
  e.g. a momentary engine/resource hiccup). The wrapper retries up to `MaxAttempts`.
- **Terminal** — retrying won't help. Once attempts are exhausted, the wrapper maps the
  outcome to a stable terminal code and stops.
- A success on any attempt returns immediately with the attempt count.

## Stable error codes

| Code | When | Attempts |
|------|------|----------|
| `TRANSIENT` | a single attempt failed but is retryable (inner adapter's signal) | the attempt number |
| `MAX_RETRIES_EXCEEDED` | all `MaxAttempts` failed (transient that never recovered) | `MaxAttempts` |
| `SDK_UNAVAILABLE` | `IsAvailable == false`; call **not attempted** | `0` |
| `SDK_EXCEPTION:<Type>` | the inner adapter threw; mapped, **never rethrown** | the attempt number |

Mapping on exhaustion (from the canonical code):

```csharp
private static string MapTerminal(string? code) => code switch
{
    null or ""   => "MAX_RETRIES_EXCEEDED",
    "TRANSIENT"  => "MAX_RETRIES_EXCEEDED",
    _            => code            // preserve a specific terminal code if the adapter set one
};
```

Exceptions are caught and mapped, so the wrapper **never throws on a vendor failure** —
callers handle integration failures uniformly via `SdkResult`.

## What the tests guarantee

`VendorSdkAdapterTests` pins this behavior:

- **Retry after transient** — `failFirst: 2`, `MaxAttempts: 3` → success on attempt 3
  (`Attempts == 3`).
- **Exhaustion** — always-failing → `MAX_RETRIES_EXCEEDED`, `Attempts == 3`.
- **Unavailable** → `SDK_UNAVAILABLE`, `Attempts == 0` (not attempted).
- **Success** path returns output and reports `Version`.

## Quarantine & audit on terminal failure

When `Process` returns a terminal code (`MAX_RETRIES_EXCEEDED`, `SDK_UNAVAILABLE`,
`SDK_EXCEPTION:*`), the caller should:

1. **Quarantine** the work item (the document/file) — do not drop it and do not mark it done.
   It can be reprocessed later (e.g. after the SDK is restored), consistent with the SFTP
   archive/replay rules.
2. **Audit** — record the stable `ErrorCode`, `Attempts`, adapter `Name`/`Version`, and a
   correlation id. Because the codes are stable, dashboards and alerts can key off them.
3. **Alert** on `SDK_UNAVAILABLE` (an environment/licensing problem) distinctly from
   `MAX_RETRIES_EXCEEDED` (a per-item problem) — they have different operator responses.

## Choosing a policy

- Set `MaxAttempts` to cover brief blips without masking a real outage (3–5 is typical).
- Set `BaseDelayMs` so retries don't hammer a struggling engine; cap total retry time.
- Treat genuinely terminal vendor errors as terminal — don't retry a malformed-input
  rejection that will fail identically every time.
