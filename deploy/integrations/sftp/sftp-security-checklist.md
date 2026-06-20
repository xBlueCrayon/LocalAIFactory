# SFTP Security Checklist

Complete before enabling `Sftp.Enabled = true` against a real partner endpoint. No secrets
belong in the repo; keys and passwords come from a secret store.

## Host-key fingerprint pinning

- [ ] Obtain the partner's SFTP **host-key fingerprint** out-of-band (not from the connection
      itself) and set `HostKeyFingerprint` (e.g. `SHA256:...`) in config.
- [ ] The client **verifies** the presented host key against the pinned fingerprint on every
      connection and **refuses** to proceed on mismatch. This defeats man-in-the-middle.
- [ ] `sftp-health-check.ps1` reminds you to pin; pinning itself is enforced by the real
      client (Posh-SSH/WinSCP) at connect time.
- [ ] Re-pin only through a documented change process when the partner rotates host keys.

## Authentication — private key preferred

- [ ] `AuthMode: PrivateKey` is the default and preferred mode.
- [ ] `PrivateKeyPath` points to a key **outside the repo** (e.g. `/secure/keys/laf-svc`).
      The key file is **never** committed.
- [ ] Use `Password` auth **only where the partner cannot support keys**, and then supply the
      password from the secret store — never in JSON.
- [ ] Protect the private key with a passphrase where supported; supply the passphrase from
      the secret store too.

## Key & credential storage

- [ ] Keys live in a protected location with OS ACLs restricting them to the service account.
- [ ] `keys/` and local overrides are git-ignored; nothing sensitive is in committed config.
- [ ] Rotate keys/passwords on a schedule and on staff changes; remove the old public key
      from the partner on rotation.

## Least privilege

- [ ] The service account (`Username`, e.g. `laf-svc`) can access **only** its designated
      `InboundDir`/`OutboundDir`/`ArchiveDir` — not the whole filesystem.
- [ ] No interactive shell on the SFTP account where the partner can enforce chroot/SFTP-only.
- [ ] Separate accounts/keys per partner where feasible, so a compromise is contained.

## Quarantine on error

- [ ] A file that fails validation/processing is moved to a **quarantine** area, not archived
      and not silently deleted.
- [ ] Malformed, oversized, or unexpected files are quarantined and alerted, never auto-fed
      into processing.
- [ ] Verify file integrity (SHA-256) before processing; a hash mismatch quarantines.

## Operational hygiene

- [ ] Transfers verify success before archiving (see `sftp-archive-policy.md`).
- [ ] Logs record connections, transfers, and outcomes — but never key material or passwords.
- [ ] Test scripts are operator-gated and store no credentials.
