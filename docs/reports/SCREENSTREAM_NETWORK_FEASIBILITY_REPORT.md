# LAF ScreenStream Assist — Network Feasibility Report

**Status: LAN_READY (honest).** This report states exactly where the screen-sharing sample
works and where it does not, without faking reachability.

## What works

- **Loopback (same PC):** works. Server on `http://localhost:5090`, client connects to
  `ws://localhost:5090/stream`.
- **LAN (same network):** works **with Windows Firewall allowed**. The first connection from
  another machine on the same network may prompt for, or require, a firewall allowance.

## What does NOT work out of the box

- **Internet:** the sample is **not** internet-ready. Reaching the server from outside the
  local network requires:
  1. A **reachable public address** — a **port-forward** on the router, or a **relay** service.
  2. **TLS / WSS** so the stream and token are encrypted in transit (the sample uses plain
     `ws://` today).

## Windows 11 Home / NAT reality

A typical Windows 11 Home machine sits behind a NAT router. "Send the client EXE to a friend
and it just connects over the internet" does **not** work without the operator setting up a
port-forward or relay and enabling TLS. This is network and security ownership that belongs to
the person running the server — it is **not** something the sample can do for them, and we do
not pretend otherwise.

## Verification

Reachability is checked, not assumed, by
`scripts/screenstream/test-network-reachability.ps1`. We deliberately do **not** fake internet
reachability in any report or test.
