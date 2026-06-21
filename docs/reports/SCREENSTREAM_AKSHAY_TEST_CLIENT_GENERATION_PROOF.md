# LAF ScreenStream Assist — "AkshayTestClient" Generation Proof

**Status:** LAN_READY (not production-grade)
**Date:** 2026-06-21
**Machine:** Windows 11 (this host)

## Purpose

Prove that the running server can generate a **named, ready-to-run client** on demand, and
that the generated output contains all expected files. A client named **`AkshayTestClient`**
was generated live via the dashboard on this machine.

## What was generated

Generating a client named `AkshayTestClient` created the folder:

```
C:\LAFScreenStreamAssist\GeneratedClients\AkshayTestClient\
```

### Verified output files (all present)

| File | Purpose |
|------|---------|
| `LAFScreenStream.Client.exe` | the runnable client (copied from the server's `ClientTemplate`) |
| `client-config.json` | connection settings (server WebSocket URL, client identity) |
| `README-CLIENT.txt` | instructions for the person who runs the client |
| `checksum.txt` | integrity checksum for the generated artifacts |

## How to generate a client

### Option A — Dashboard (intended path)

1. Start the server (`Start-Server.bat`) and open `http://localhost:5090`.
2. Click **Generate Client**.
3. Enter a name (e.g. `AkshayTestClient`).
4. The server writes a new folder under `GeneratedClients\<name>\` with the four files above.

### Option B — Command fallback (POST to the API)

The dashboard button calls the same API endpoint. You can call it directly:

```
POST http://localhost:5090/api/generate-client
```

This is the underlying endpoint the **Generate Client** button invokes; it produces the
same `GeneratedClients\<name>\` output folder.

## Honest scope note

This proves a working **client generator**: one server can mint multiple named clients,
each self-contained with its own config and checksum. It does **not** add per-client token
expiry, signing of the generated EXE, or revocation — those are tracked in
`SCREENSTREAM_TO_PRODUCTION_ROADMAP.md`. Generated clients are intended for LAN/loopback
use today; the generated `client-config.json` points at a plain `ws://` URL.
