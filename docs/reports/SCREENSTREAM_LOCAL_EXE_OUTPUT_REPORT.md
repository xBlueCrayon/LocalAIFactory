# LAF ScreenStream Assist — Local EXE Output Report

**Status: LAN_READY.** This report records the exact local folder layout produced for the
screen-sharing sample.

## How it was produced

`generated-products/LAF-ScreenStreamAssist/scripts/publish-local-test-folder.ps1` publishes
the solution to `C:\LAFScreenStreamAssist`.

## Layout (verified real files)

```
C:\LAFScreenStreamAssist\
  README-START-HERE.txt
  Server\
    LAFScreenStream.Server.exe          <- double-clickable server
    Start-Server.bat
    appsettings.json
    README-FIRST.txt
  ClientTemplate\
    LAFScreenStream.Client.exe          <- template the server clones per client
  GeneratedClients\
    TestClient\
      LAFScreenStream.Client.exe        <- a ready-to-send client
      client-config.json
      checksum.txt
      README-CLIENT.txt
```

## File paths

- `C:\LAFScreenStreamAssist\README-START-HERE.txt`
- `C:\LAFScreenStreamAssist\Server\LAFScreenStream.Server.exe`
- `C:\LAFScreenStreamAssist\Server\Start-Server.bat`
- `C:\LAFScreenStreamAssist\Server\appsettings.json`
- `C:\LAFScreenStreamAssist\Server\README-FIRST.txt`
- `C:\LAFScreenStreamAssist\ClientTemplate\LAFScreenStream.Client.exe`
- `C:\LAFScreenStreamAssist\GeneratedClients\TestClient\LAFScreenStream.Client.exe`
- `C:\LAFScreenStreamAssist\GeneratedClients\TestClient\client-config.json`
- `C:\LAFScreenStreamAssist\GeneratedClients\TestClient\checksum.txt`
- `C:\LAFScreenStreamAssist\GeneratedClients\TestClient\README-CLIENT.txt`

These are real published EXEs (not stubs); the server EXE has been launched and the
dashboard served (see the Server EXE Run Proof report).
