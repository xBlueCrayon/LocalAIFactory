# SMTP Integration (deploy/smtp)

Outbound email for LocalAIFactory: notifications, operator alerts, and import/job
reports. The platform sends through an **enterprise relay** — it does not run its own
mail transfer agent and makes no claim of compatibility with any specific vendor product.

This folder holds the configuration template and two safe operator scripts. Everything is
**off by default** (`Smtp.Enabled = false`) and degrades gracefully: if SMTP is disabled or
unreachable, the app must still run (MSSQL-only mode is unaffected).

## Files

| File | Purpose |
|------|---------|
| `appsettings.smtp.example.json` | Config template. Copy into your real `appsettings`, fill non-secret values, inject the password from a secret store. |
| `smtp-health-check.ps1` | Read-only TCP reachability probe. Sends no mail, uses no credentials. |
| `smtp-test-send.ps1` | Sends one test message. Defaults to a local dev sink (`localhost:1025`); refuses external hosts without `-AllowExternal`. |

## Relay patterns

Pick the pattern that matches your estate. All use `Port 587` + STARTTLS unless noted.

1. **Existing enterprise relay (IP-allowlisted, `AuthMode: None`).** The most common
   internal pattern: the relay trusts the app server by source IP, so no credentials are
   stored. Lock the relay to the app server's address and to known recipient domains.
2. **M365 / O365 relay (`AuthMode: M365`).** Authenticated submission to a Microsoft 365
   tenant. Prefer modern auth (OAuth/app registration) over legacy Basic auth where the
   tenant allows it. Submission host typically `smtp.<tenant-relay>.example:587` STARTTLS.
3. **Authenticated provider (`AuthMode: Basic`).** Generic relay or transactional provider
   using a username and a password/app-key. The password is **never** in config — it is
   injected as `Smtp__Password` from the secret store at deploy time.
4. **Internal relay by IP (`AuthMode: None`).** A dedicated internal smart host for the
   banking estate. Same as pattern 1 but operated by your own infrastructure team.
5. **IIS SMTP legacy note.** Some legacy Windows estates still front a pickup/relay via the
   old IIS SMTP feature. It works as a plain relay target (`Host`/`Port`), but it is
   deprecated by Microsoft — treat it as a transitional smart host, prefer a maintained
   relay, and still require TLS on the next hop where possible.
6. **Mailpit / MailHog dev sink.** For development only: a local SMTP sink on
   `localhost:1025` with a web UI. No mail leaves the machine. See `mailpit-dev-guide.md`.

## Configuration

Copy the template and fill it in:

```jsonc
"Smtp": {
  "Enabled": true,
  "Host": "smtp.your-relay.example",
  "Port": 587,
  "UseStartTls": true,
  "FromAddress": "noreply@your-domain.example",
  "FromName": "LocalAIFactory",
  "AuthMode": "None",        // None | Basic | M365
  "Username": ""             // set only for Basic/M365
}
```

The password is supplied out-of-band as the environment variable `Smtp__Password`
(double underscore = nested config key). It is **not** a field in the JSON.

## Using the scripts

```powershell
# 1. Confirm the relay is reachable (no mail, no credentials)
./smtp-health-check.ps1 -SmtpHost smtp.your-relay.example -Port 587

# 2. Send a test to a local dev sink (safe default — nothing leaves the box)
./smtp-test-send.ps1                       # -> localhost:1025

# 3. Send through the real relay (explicit operator intent required)
$cred = Get-Credential
./smtp-test-send.ps1 -SmtpHost smtp.your-relay.example -Port 587 `
    -From noreply@your-domain.example -To you@your-domain.example `
    -UseStartTls -Credential $cred -AllowExternal
```

`smtp-test-send.ps1` only treats `localhost`/`127.0.0.1` on ports `1025/1026/2525` as a dev
sink. Any other target requires `-AllowExternal`, a deliberate safety gate.

## Credential handling

- **Never commit credentials.** The template carries placeholders only.
- Inject `Smtp__Password` from a secret store (environment variable, Windows credential
  store, or your platform's secret manager) at deploy time.
- Scripts take credentials via `-Credential`; they never embed or persist them.
- Prefer `AuthMode: None` (IP-allowlisted internal relay) where the estate supports it, so
  there is no send password at all.

See also: `smtp-security-checklist.md`, `smtp-relay-setup-guide.md`, `troubleshooting.md`.
