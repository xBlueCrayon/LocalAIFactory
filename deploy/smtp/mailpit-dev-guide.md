# Mailpit / MailHog Dev Sink Guide

In development, **no real mail should ever be sent.** Run a local SMTP sink that captures
every message and shows it in a web UI. LocalAIFactory's `smtp-test-send.ps1` defaults to
this sink (`localhost:1025`), so the safe path is the default path.

Mailpit and MailHog are interchangeable for this purpose; pick whichever your team already
uses. They are general-purpose dev mail catchers, not tied to this project.

## What it does

- Listens on a local **SMTP** port (commonly `1025`).
- Accepts and stores any message; **nothing leaves the machine**.
- Serves a **web UI** (Mailpit default `:8025`, MailHog default `:8025`) to inspect mail.

## Run it

Mailpit (single binary or Docker):

```powershell
# Docker
docker run -d --name mailpit -p 1025:1025 -p 8025:8025 axllent/mailpit
# Web UI: http://localhost:8025  | SMTP: localhost:1025
```

MailHog (Docker):

```powershell
docker run -d --name mailhog -p 1025:1025 -p 8025:8025 mailhog/mailhog
# Web UI: http://localhost:8025  | SMTP: localhost:1025
```

(Versions/ports may differ per release — check the tool's own docs. The project only assumes
SMTP on `1025`.)

## Point the app at the sink

```jsonc
"Smtp": {
  "Enabled": true,
  "Host": "localhost",
  "Port": 1025,
  "UseStartTls": false,      // dev sinks typically accept plaintext
  "FromAddress": "noreply@dev.local",
  "AuthMode": "None"
}
```

## Verify

```powershell
# Sink reachable?
./smtp-health-check.ps1 -SmtpHost localhost -Port 1025

# Send a test (default target IS the sink — no flags needed)
./smtp-test-send.ps1
```

Then open the web UI and confirm the message arrived with the expected From/To/subject/body.

## Why this is the safe default

`smtp-test-send.ps1` recognizes only `localhost`/`127.0.0.1` on ports `1025/1026/2525` as a
dev sink. Anything else is refused unless you pass `-AllowExternal`. This makes it hard to
accidentally email real people from a dev box: the friction is on the *external* path, not
the safe one.

## Tips

- Keep `UseStartTls: false` for the sink unless your sink build enables TLS.
- Wipe captured mail between test runs from the web UI so old messages don't confuse you.
- Use a recognizable `From`/`To` (`*.dev.local`) so dev mail is obvious in the UI.
