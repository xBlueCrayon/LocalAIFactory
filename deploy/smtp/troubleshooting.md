# SMTP Troubleshooting

Work top-down: **reachability → TLS → auth → relay policy → delivery.** Each layer must pass
before the next is meaningful. Use the scripts to isolate the failing layer.

## 1. Connection / port / firewall

**Symptom:** `smtp-health-check.ps1` reports `NOT reachable`; sends time out.

- Confirm host and port are correct (`587` submission, `465` implicit TLS, `25` legacy relay).
- A firewall or egress policy may block the port — many estates block outbound `25`.
- Test from the actual app server, not your workstation (allowlists are per-source-IP).

```powershell
./smtp-health-check.ps1 -SmtpHost smtp.your-relay.example -Port 587
Test-NetConnection smtp.your-relay.example -Port 587   # raw TCP detail
```

## 2. TLS / STARTTLS

**Symptom:** connects then fails during handshake; "secure connection" / certificate errors.

- Match the mode to the relay: STARTTLS on `587` (`UseStartTls: true`) vs implicit TLS on
  `465`. Using the wrong one fails the handshake.
- Certificate validation failure usually means an untrusted/expired cert or a name mismatch.
  **Fix the trust chain — do not disable validation.**
- Some relays require a recent TLS version; ensure the host OS isn't pinned to a disabled one.

## 3. Authentication

**Symptom:** `5xx` auth errors, "authentication required", "535 5.7.x".

- For `Basic`/`M365`, confirm `Smtp__Password` is actually present in the environment (a
  blank or unset secret is the most common cause).
- M365 legacy Basic auth may be disabled tenant-wide — switch to modern auth (OAuth).
- Verify `Username` matches the send identity and the account isn't locked/expired.

## 4. Relay denied / not allowed to send

**Symptom:** "550 relay denied", "5.7.1 unable to relay", "client was not authenticated".

- For `AuthMode: None`, the relay isn't allowlisting the app server's source IP — give the
  mail team the correct address.
- The relay may restrict allowed `FromAddress` domains or recipient domains.
- Confirm you are not unintentionally trying to relay to external domains through an
  internal-only smart host.

## 5. Accepted but not delivered (SPF / DKIM / DMARC)

**Symptom:** send succeeds, recipient never receives it or it lands in junk.

- Check the recipient's spam/quarantine first.
- SPF: the relay's sending IPs must be authorized for `FromAddress`'s domain.
- DKIM: the relay must sign for that domain.
- DMARC: `FromAddress` must align with SPF/DKIM or recipients reject/quarantine.
- These are DNS/mail-team items — the app cannot fix them, but a failed DMARC alignment is a
  frequent "it sends but nobody gets it" cause.

## 6. Dev-sink confusion

**Symptom:** `smtp-test-send.ps1` "Refusing to send to a non-dev host without -AllowExternal".

- That's the safety gate, not a bug. For a real relay add `-AllowExternal`.
- For the dev sink, ensure it's running on `localhost:1025` (see `mailpit-dev-guide.md`).

## Quick triage table

| Layer | Tool | Pass criterion |
|-------|------|----------------|
| Reachability | `smtp-health-check.ps1` | exit 0, `REACHABLE` |
| TLS/auth/send mechanics | `smtp-test-send.ps1` (dev sink) | message appears in sink UI |
| Real relay | `smtp-test-send.ps1 -AllowExternal` | `OK ->` and recipient receives |
| Delivery/reputation | recipient inbox + mail team | not junk, DMARC passes |
