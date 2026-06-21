# LAF ScreenStream Assist — Client EXE Generation Proof

**Status: LAN_READY.** This report records that the server generates a real, runnable client
package.

## How a client is generated

From the running server dashboard (or `POST /api/generate-client`), the server clones the
`ClientTemplate` and writes a per-client package. Two proofs exist:

1. The published **TestClient** at
   `C:\LAFScreenStreamAssist\GeneratedClients\TestClient\`.
2. A **TestClient2** package created **at runtime** by `POST /api/generate-client` while the
   server EXE was running.

## Files in a generated client package

- `LAFScreenStream.Client.exe` — double-clickable client.
- `client-config.json` — the configured server address and session token.
- `checksum.txt` — integrity checksum of the package.
- `README-CLIENT.txt` — plain-language instructions for the recipient.
- Supporting `.dll` files.

## That the client runs

The client is a **visible WinForms window** showing status, a Disconnect button, and a
"your primary screen is being shared" warning. It performs real GDI BitBlt capture of the
**primary screen only** and streams over the token-authenticated WebSocket. The packager is
proven by tests `packager creates full package` and `packager friendly-error-not-500`; the
runtime generation is proven by the live `POST /api/generate-client` producing TestClient2.

The token is written into `client-config.json` next to the EXE — appropriate for LAN scope;
it is a per-server shared secret, not a rotating per-session credential.
