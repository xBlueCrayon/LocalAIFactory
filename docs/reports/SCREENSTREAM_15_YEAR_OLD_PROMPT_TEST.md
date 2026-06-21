# LAF ScreenStream Assist — 15-Year-Old Prompt Test

**Status: LAN_READY (not production-grade).** This report records how LocalAIFactory
turned a single plain-English request from a non-coder into a working consent-based
screen-sharing sample.

## 1. The only product input

The sole input LocalAIFactory received was the teen-user prompt at
`benchmarks/screen-stream/study/teen-user-screenstream-request.md`:

> Hi, I am 15 and I do not know coding. I need a simple Windows app. On my computer I
> open a Server EXE. It shows a dashboard and a button to create a Client EXE. I send
> the Client EXE to another person. When they double-click it, it connects to my server
> using the address I configured and shares only their main screen. The client app must
> be visible and must have a Disconnect button. No hidden stuff, no remote control, no
> files, no keyboard, no webcam, no microphone. I want it to work on Windows 11 Home
> with .NET 10 installed. Please make everything simple and give me the EXE files in a
> folder.

## 2. How LAF decomposed the request

| Plain-English ask | Resulting component |
|---|---|
| "I open a Server EXE … dashboard … button to create a Client EXE" | `LafScreenStream.Server` (Kestrel dashboard + `/api/generate-client`) |
| "send the Client EXE … double-click … connects … shares only their main screen" | `LafScreenStream.Client` (visible WinForms app, GDI BitBlt of primary screen) |
| "must be visible … Disconnect button" | Client window shows status, a "your primary screen is being shared" warning, and a Disconnect button |
| "No hidden stuff / remote control / files / keyboard / webcam / microphone" | Hard safety rules + a source-scan test |
| "give me the EXE files in a folder" | `publish-local-test-folder.ps1` → `C:\LAFScreenStreamAssist` |

Shared protocol, token, and the `IScreenSource` abstraction live in
`LafScreenStream.Shared`; the client-package generator lives in
`LafScreenStream.Packager`.

## 3. Safety rules extracted from the prompt

- Client is **always visible** — no stealth, persistence, autostart, service, or UAC bypass.
- **No** keyboard, clipboard, file, webcam, or microphone capture.
- **No** remote control — view-only.
- **Primary screen only.**
- A mandatory **session token** on every stream; there is no unauthenticated streaming endpoint.

These are enforced, not just documented: the test `Source_contains_no_surveillance_apis`
scans the source for forbidden APIs (`SetWindowsHookEx`, `GetAsyncKeyState`, `keybd_event`,
`Clipboard`, `waveIn`, `MediaCapture`) and passes because none are present.

## 4. Product requirements derived

- .NET 10 solution, 5 projects (Shared, Packager, Server, Client, Tests).
- Token-authenticated WebSocket `/stream`; `/api/generate-client`; health endpoint.
- Real GDI BitBlt capture of the primary screen in the client.
- Output as double-clickable EXEs in a folder, with a server EXE and generated client packages.

## 5. Code + tests generated

- 12 xUnit tests pass (token valid/invalid, frame round-trip, fake source, health,
  dashboard renders, invalid-token rejected, valid token streams fake frames + dashboard
  counts them, disconnect stops the stream, packager friendly-error-not-500, packager
  creates full package, source security scan, safety manifest).
- 4 Playwright (Chromium) tests pass (dashboard + Generate Client + health; token present;
  simulated browser-WebSocket client streams + dashboard shows frames + disconnect; no HTTP 500).
- Automated tests use the deterministic `FakeScreenSource`; the real GDI capture is exercised
  in manual client runs (documented distinction).

## 6. Missing assumptions LAF handled honestly

The prompt assumed "send the Client EXE to another person" would just work. The other
person is usually behind a NAT router, and Windows 11 Home does not change that. LAF did
**not** fake internet reachability:

- **Loopback** (same PC) works.
- **LAN** works with Windows Firewall allowed.
- **Internet** needs a reachable public address (port-forward or relay) plus TLS/WSS — this
  is operator-owned network setup, validated via
  `scripts/screenstream/test-network-reachability.ps1`.

## 7. Final folder where the EXEs are

Published to `C:\LAFScreenStreamAssist` (see
`generated-products/LAF-ScreenStreamAssist/scripts/publish-local-test-folder.ps1`). The
server EXE is at `C:\LAFScreenStreamAssist\Server\LAFScreenStream.Server.exe`.
