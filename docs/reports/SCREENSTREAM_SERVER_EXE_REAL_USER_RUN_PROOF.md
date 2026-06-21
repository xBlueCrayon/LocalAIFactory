# LAF ScreenStream Assist — Server EXE Real-User Run Proof

**Status:** LAN_READY (not production-grade)
**Date:** 2026-06-21
**Machine:** Windows 11 (this host)

## Purpose

Prove that the **real published server EXE** runs the way a non-technical operator would
run it — by launching `Start-Server.bat` / `LAFScreenStream.Server.exe` and opening the
dashboard in a browser — and that it serves a healthy dashboard with no HTTP 500 and no
crash. All results below were observed live on this machine.

## What was run

- Launched `C:\LAFScreenStreamAssist\Server\Start-Server.bat`, which started
  `LAFScreenStream.Server.exe`.
- Opened the dashboard at `http://localhost:5090`.

## Verified results

| Check | Result |
|-------|--------|
| Dashboard `http://localhost:5090` | HTTP **200** |
| `GET /api/health` | `status = ok`, `serverWsUrl = ws://localhost:5090/stream` |
| "Generate Client" button on the dashboard | **present / visible** |
| HTTP 500 errors | **none** |
| Process crash | **none** |

## Interpretation

- The server starts from a plain double-click of the batch file — no command line or
  developer tooling required.
- The health endpoint reports `ok` and advertises the WebSocket stream URL
  (`ws://localhost:5090/stream`) that clients connect to.
- The dashboard renders cleanly and exposes the "Generate Client" action used in the
  companion generation proof.

## Honest scope note

This proves the server runs and serves a healthy dashboard **on this machine over
loopback**. The stream URL is plain `ws://` (not `wss://`), which is fine for loopback
and LAN but is **not** production-grade for the public internet. TLS/WSS and code-signing
remain prerequisites for production (see `SCREENSTREAM_TO_PRODUCTION_ROADMAP.md`).
