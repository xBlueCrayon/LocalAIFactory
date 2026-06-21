# LAF ScreenStream Assist — Local Test Folder Verification

**Status:** LAN_READY (not production-grade)
**Date:** 2026-06-21
**Machine:** Windows 11 (this host)

## Purpose

Confirm that the local install folder `C:\LAFScreenStreamAssist` exists and contains
every file required to run the server, generate a client, and run a previously generated
test client. Every path below was verified present live on this machine.

## Verified contents of `C:\LAFScreenStreamAssist`

| Path | Verified |
|------|----------|
| `C:\LAFScreenStreamAssist\README-START-HERE.txt` | present |
| `C:\LAFScreenStreamAssist\Server\LAFScreenStream.Server.exe` | present |
| `C:\LAFScreenStreamAssist\Server\Start-Server.bat` | present |
| `C:\LAFScreenStreamAssist\Server\appsettings.json` | present |
| `C:\LAFScreenStreamAssist\Server\README-FIRST.txt` | present |
| `C:\LAFScreenStreamAssist\Server\ClientTemplate\LAFScreenStream.Client.exe` | present |
| `C:\LAFScreenStreamAssist\GeneratedClients\TestClient\...` | present |

## What each piece is for

- **`Server\LAFScreenStream.Server.exe`** — the published server. Hosts the dashboard
  and the WebSocket stream endpoint.
- **`Server\Start-Server.bat`** — convenience launcher; starts the server EXE and is the
  intended double-click entry point for a non-technical operator.
- **`Server\appsettings.json`** — server configuration (e.g. listen address/port).
- **`Server\README-FIRST.txt`** — operator-facing first-run notes for the server.
- **`Server\ClientTemplate\LAFScreenStream.Client.exe`** — the published client template
  that the server copies and configures when a new client is generated.
- **`GeneratedClients\TestClient\...`** — an earlier generated client, present from prior
  testing; demonstrates the generated-output layout.
- **`README-START-HERE.txt`** — top-level orientation file for the whole package.

## Honest scope note

This verification only confirms **file presence and folder layout**. Runtime behaviour
(server run, client generation, real loopback streaming) is proven in the companion
reports. This package is **LAN-ready**; it is **not** production-grade because TLS/WSS
and code-signing are not yet in place (see `SCREENSTREAM_TO_PRODUCTION_ROADMAP.md`).
