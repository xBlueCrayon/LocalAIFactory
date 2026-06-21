# LAF ScreenStream Assist — Server EXE Run Proof

**Status: LAN_READY.** This report records that the published server EXE was actually run.

## EXE launched

`C:\LAFScreenStreamAssist\Server\LAFScreenStream.Server.exe`

## Observed behavior

- **Dashboard:** `http://localhost:5090` returns **HTTP 200** and shows the **Generate Client**
  button.
- **Health:** `/api/health` returns `status=ok` and
  `serverWsUrl=ws://localhost:5090/stream`.
- **Initial client count:** `0` clients.
- **Runtime client generation:** `POST /api/generate-client` created a full **TestClient2**
  package at runtime (EXE + config + checksum + README + DLLs).

## What this proves

The server is a real, runnable Kestrel app — not a mockup. It serves the dashboard, reports
health honestly (including the WebSocket URL), and can mint a new client package on demand
while running. The WebSocket URL is `ws://` (plain) on loopback; internet use would require
`wss://` + TLS, which is not present (see Production-Grade Assessment).
