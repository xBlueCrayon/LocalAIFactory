# SMTP Relay Setup Guide

Step-by-step configuration for each supported relay pattern. Every pattern follows the same
shape: **configure → reachability check → test-send to the dev sink → (deliberately) test
the real relay**. Start disabled; enable only after the checks pass.

Common prerequisites:

- Copy `appsettings.smtp.example.json` into your real config; leave `Enabled: false` until
  verified.
- Have the relay host, port, and `FromAddress` from your mail team.
- Read `smtp-security-checklist.md` first.

## Pattern A — Existing enterprise relay (IP-allowlisted)

1. Ask the mail team to allowlist the app server's IP and the `FromAddress` domain.
2. Config:
   ```jsonc
   "Smtp": { "Enabled": true, "Host": "smtp.internal-relay.example", "Port": 587,
             "UseStartTls": true, "FromAddress": "noreply@your-domain.example",
             "AuthMode": "None", "Username": "" }
   ```
3. Verify reachability:
   ```powershell
   ./smtp-health-check.ps1 -SmtpHost smtp.internal-relay.example -Port 587
   ```
4. Prove send mechanics against the dev sink first, then the real relay with `-AllowExternal`.

## Pattern B — M365 / O365 relay

1. Create/obtain a send identity in the tenant; prefer an app registration (OAuth) over
   legacy Basic auth.
2. Config: `AuthMode: M365`, `Host` = your tenant's submission host, `Port 587`,
   `UseStartTls: true`, `Username` = the send identity.
3. Inject the secret at deploy time: `setx`/environment `Smtp__Password=<app-secret>` (or
   wire OAuth token acquisition per your tenant).
4. Reachability:
   ```powershell
   ./smtp-health-check.ps1 -SmtpHost smtp.<tenant-relay>.example -Port 587
   ```
5. Confirm SPF/DKIM/DMARC alignment for `FromAddress` before going live (see checklist).

## Pattern C — Authenticated provider (Basic)

1. Obtain a username + app-key/password from the provider.
2. Config: `AuthMode: Basic`, `Username` set, `Port 587`, `UseStartTls: true`.
3. Provide the secret as `Smtp__Password` from the secret store — never in JSON.
4. Reachability check, then test-send.

## Pattern D — Internal relay by IP

Same as Pattern A but operated by your own infrastructure team on a trusted segment. If the
relay terminates TLS upstream and only accepts plaintext on the trusted hop, `Port 25` with
`UseStartTls: false` may apply — confirm the segment is trusted and document the exception.

## Pattern E — IIS SMTP legacy smart host

Treat the legacy IIS SMTP service as a plain relay target: point `Host`/`Port` at it,
`AuthMode: None`, and enforce TLS on the next hop where possible. This is transitional —
plan migration to a maintained relay.

## Verify (all patterns)

```powershell
# 1. Reachable?
./smtp-health-check.ps1 -SmtpHost <host> -Port <port>

# 2. Send mechanics OK against the dev sink (no real mail)
./smtp-test-send.ps1                          # localhost:1025

# 3. Real relay send — explicit operator intent
$cred = Get-Credential                        # only for Basic/M365
./smtp-test-send.ps1 -SmtpHost <host> -Port <port> -UseStartTls `
    -From noreply@your-domain.example -To you@your-domain.example `
    -Credential $cred -AllowExternal
```

A non-zero exit from `smtp-health-check.ps1` means host/port/firewall — fix that before
touching auth or TLS. See `troubleshooting.md`.
