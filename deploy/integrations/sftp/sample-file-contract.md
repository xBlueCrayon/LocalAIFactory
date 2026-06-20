# Sample File-Exchange Contract (ORIGINAL example)

This is an **illustrative, original** file contract to show the shape of a partner exchange.
It copies **no vendor format** and makes no compatibility claim. Adapt the field names,
encoding, and codes to whatever you and your partner actually agree.

The exchange uses a classic **header / detail / trailer** layout with a separate `.done`
marker and a response file.

## File naming & markers

```
Outbound (we send):  LAF_OUT_<feed>_<yyyyMMdd>_<seq>.dat   + .done marker
Inbound  (we read):  LAF_IN_<feed>_<yyyyMMdd>_<seq>.dat    + .done marker
Response (we read):  LAF_RSP_<feed>_<yyyyMMdd>_<seq>.dat   + .done marker
```

- The payload `.dat` is written first; the zero-byte `<name>.done` marker is written **last**.
- The consumer ignores any payload lacking its `.done` marker.

## Encoding

- **UTF-8**, no BOM.
- Line terminator: LF (`\n`) — agree CRLF only if the partner requires it.
- Field delimiter in this example: pipe `|`. Fixed-width is an equally valid choice if agreed.

## Record structure (conceptual)

### Header (exactly one, first line)

| Field | Example | Notes |
|-------|---------|-------|
| `RecordType` | `H` | constant `H` |
| `FileId` | `9f2c...` | the **idempotency key**; unique per file |
| `Feed` | `PAYMENTS` | business feed name |
| `CreatedUtc` | `2026-06-21T09:30:00Z` | ISO-8601 UTC |
| `Sender` | `LAF` | originator code |
| `SchemaVersion` | `1.0` | contract version |

### Detail (zero or more lines)

| Field | Example | Notes |
|-------|---------|-------|
| `RecordType` | `D` | constant `D` |
| `RowKey` | `0001` | unique within file |
| `Reference` | `INV-100245` | business reference |
| `Amount` | `1234.56` | decimal, dot separator |
| `Currency` | `GBP` | ISO-4217 |
| `Status` | `NEW` | feed-specific |

### Trailer (exactly one, last line)

| Field | Example | Notes |
|-------|---------|-------|
| `RecordType` | `T` | constant `T` |
| `RecordCount` | `2` | number of `D` rows |
| `ControlSum` | `2469.12` | sum of `Amount` for integrity |
| `Sha256` | `c1a9...` | SHA-256 of the bytes above the trailer |

## Idempotency

- `FileId` (header) is the **idempotency key**. The consumer records processed `FileId`s and
  **skips** a re-delivered file with the same `FileId` (and matching content hash).
- The trailer's `RecordCount` and `ControlSum` are validated before processing; a mismatch
  quarantines the file.

## `.done` marker

- Presence of `LAF_IN_..._.dat.done` (suffix per `DoneMarkerSuffix`) signals the payload is
  complete and may be read. Absence means "still being written — do not touch."

## Response file

For each inbound file we process, we emit a response (and the partner does likewise):

```
H|<FileId-of-source>|PAYMENTS|2026-06-21T09:31:10Z|LAF|1.0
D|0001|INV-100245|ACCEPTED|
D|0002|INV-100246|REJECTED|REASON=AMOUNT_MISMATCH
T|2|0|<sha256>
```

- Response detail lines echo the source `RowKey`/`Reference` with an outcome
  (`ACCEPTED` / `REJECTED`) and, on rejection, a stable `REASON=` code agreed in the contract.
- The response file gets its own `.done` marker and is archived after the partner consumes it.

## Lifecycle

```
write payload -> write .done -> partner reads -> process -> emit response (+ .done)
            -> verify -> archive original (hashed) -> idempotency key recorded
```

See `sftp-archive-policy.md` for archive/replay rules and `sftp-security-checklist.md` for
transport security.
