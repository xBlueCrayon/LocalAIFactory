# SMTP Security Checklist

Run through this before enabling `Smtp.Enabled = true` against a real relay. None of these
items require secrets in the repo; all are config or relay-side controls.

## Transport (TLS / STARTTLS)

- [ ] `Port 587` with `UseStartTls: true` (submission with STARTTLS) is the default. Use
      implicit TLS (`465`) only if your relay requires it.
- [ ] Avoid plaintext `Port 25` for authenticated submission. Port 25 is acceptable only for
      an internal smart host on a trusted segment where TLS is terminated upstream.
- [ ] The relay presents a valid certificate; the app validates it (do not disable cert
      validation to "make it work" — fix the trust chain instead).

## Authentication

- [ ] `AuthMode` matches the relay: `None` (IP-allowlisted), `Basic`, or `M365`.
- [ ] For `None`, the relay restricts submission to the app server's source IP.
- [ ] For `Basic`/`M365`, the password is injected as `Smtp__Password` from a secret store,
      never written into committed config.
- [ ] Prefer modern auth (OAuth/app registration) for M365 over legacy Basic where available.

## Least-privilege send account

- [ ] The send identity can **only send**, ideally only as the configured `FromAddress`.
- [ ] No mailbox read access, no admin rights, no broad relay scope.
- [ ] Restrict allowed recipient domains on the relay where the use case is internal-only.

## No open relay

- [ ] The relay does **not** accept unauthenticated submission from arbitrary sources.
- [ ] Source-IP allowlist and/or authentication is enforced relay-side.
- [ ] Rate limits are in place so a misconfigured job cannot flood recipients.

## Domain reputation awareness (SPF / DKIM / DMARC)

These are owned by your mail/DNS team, but the integration must respect them:

- [ ] **SPF**: the relay's sending IPs are authorized in your domain's SPF record.
- [ ] **DKIM**: the relay signs outbound mail for `FromAddress`'s domain.
- [ ] **DMARC**: `FromAddress` aligns with SPF/DKIM so messages pass DMARC and are not
      quarantined or rejected by recipients.
- [ ] Do not send as a domain you are not authorized to send for.

## Credential & operational hygiene

- [ ] No password in `appsettings.smtp.example.json` or any committed file.
- [ ] `keys/` and local overrides are git-ignored.
- [ ] `smtp-test-send.ps1` is used against the dev sink for routine checks; external sends
      require explicit `-AllowExternal`.
- [ ] Rotate send credentials on a schedule and on staff changes.
- [ ] Log send failures (not message bodies/credentials) for auditing.
