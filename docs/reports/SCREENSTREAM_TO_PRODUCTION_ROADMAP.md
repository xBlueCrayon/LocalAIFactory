# LAF ScreenStream Assist — To-Production Roadmap

**Status today:** LAN_READY (not production-grade)
**Date:** 2026-06-21

## Where it stands

Verified working today:

- Server EXE runs from a double-click; dashboard returns HTTP 200; `/api/health` = `ok`.
- Client generation works (named clients with config, README, and checksum).
- Real loopback stream works with the published EXEs: real 2560x1440 capture, frames over
  WebSocket, live dashboard counter, working disconnect.
- Automated tests pass (12 .NET + 4 Playwright) and a source-scan test confirms **no
  surveillance APIs** (consent-based, visible client only).
- Networking: loopback OK; LAN OK with the Windows Firewall allowed.

What caps it at LAN-ready rather than production-grade:

- Transport is plain `ws://` (**no TLS/WSS**).
- The EXEs are **not code-signed**.
- Internet use requires operator-owned network setup (public address + port-forward or
  relay) that is **not** provided and **not** faked.

## Roadmap to production

None of the items below are stealth or surveillance features. They are standard
security, packaging, and operability work.

1. **TLS / WSS** — encrypt the transport so streams are protected over LAN and internet.
   This is the single biggest gap.
2. **Code signing** — sign `LAFScreenStream.Server.exe` and the generated
   `LAFScreenStream.Client.exe` so Windows SmartScreen/AV trust them.
3. **Installer** — a proper installer (MSI/EXE) instead of a copied folder + batch file.
4. **Firewall-rule helper** — a guided step that adds the needed inbound rule on the host,
   instead of relying on the first-run Allow prompt.
5. **Relay / tunnel option** — a documented, operator-owned relay so internet use does not
   require manual router port-forwarding.
6. **Per-client token expiry** — generated clients carry a token that expires, so old
   client folders stop working after a set time.
7. **Stronger authentication** — verify both ends (e.g. mutual token/identity check) before
   a stream starts.
8. **Audit log** — record who connected, when, from where, and when they disconnected.
9. **Optional auto-update** — keep deployed clients/servers current without re-sending
   folders.
10. **Support mode** — a guided helper for an operator to assist an end user (clear consent
    prompts, session start/stop visibility).
11. **Network diagnostics** — a built-in check that tells the operator whether loopback /
    LAN / internet reachability is OK and what's blocking it.
12. **Real two-PC LAN test** — execute and document a verified host↔client run across two
    physical machines on the same network.
13. **Real public-internet test** — execute and document a verified run across the internet
    using TLS/WSS plus the relay/port-forward path.

## Honest summary

The product is genuinely **LAN-ready and demonstrably real** (real capture, real streaming,
real disconnect). It is **not** production-grade yet — TLS/WSS and code-signing are the
hard gates, and a verified two-PC LAN test and a real internet test still need to be run.
