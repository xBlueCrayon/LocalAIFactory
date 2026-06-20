# SFTP File Exchange (deploy/integrations/sftp)

Batch file exchange with partner systems in the banking estate: the platform drops files for
a partner (outbound) and picks up files a partner produces (inbound), over SFTP. There is no
proprietary protocol and no claim of compatibility with any vendor's exchange format — this
is a generic, safe SFTP pattern.

Everything is **off by default** (`Sftp.Enabled = false`) and degrades gracefully: with SFTP
disabled or the endpoint unreachable, the app still runs (MSSQL-only mode unaffected).

## Files

| File | Purpose |
|------|---------|
| `appsettings.sftp.example.json` | Config template. Private-key auth, pinned host-key fingerprint, in/out/archive dirs, `.done` marker suffix. |
| `sftp-health-check.ps1` | Read-only TCP reachability + host-key pinning reminder. No auth, no transfer. |
| `sftp-upload-test.ps1` | Operator-gated upload test (uses Posh-SSH if installed). |
| `sftp-download-test.ps1` | Operator-gated download + response-processing test (idempotent). |

## The file-exchange pattern

1. **`.done` marker for atomic visibility.** A producer writes `payload.dat`, then writes a
   zero-byte `payload.dat.done` only after the payload is fully written. The consumer ignores
   any payload without its `.done` marker, so it never reads a half-written file. The marker
   suffix is configurable (`DoneMarkerSuffix`, default `.done`).
2. **Idempotency key.** Each file carries (or is identified by) a stable idempotency key —
   in practice the filename plus a content hash (SHA-256). A file already processed (same
   name + hash) is **skipped**, so re-delivery or replay never double-processes.
3. **Archive-after-success.** Only after a file is fully processed is it moved to
   `ArchiveDir`. A failure leaves the source in place (or quarantined) for retry — never
   silently consumed. See `sftp-archive-policy.md`.
4. **Response / rejection processing.** Inbound files may be acknowledgements, responses, or
   rejection codes. The consumer parses the response/rejection, records the outcome, and
   archives the response file too.
5. **Replay.** Because originals are archived with their hash, a file can be re-fed from the
   archive deliberately (operator action) to reprocess after a downstream fix.

Directories (`InboundDir` `/in`, `OutboundDir` `/out`, `ArchiveDir` `/archive`) are
configurable. Inbound = files we receive; outbound = files we send.

## Scripts usage

```powershell
# 1. Reachability + host-key reminder (no auth, no transfer)
./sftp-health-check.ps1 -SftpHost sftp.your-partner.example -Port 22

# 2. Upload test (operator-gated; needs an SFTP client + configured endpoint)
./sftp-upload-test.ps1 -Endpoint sftp.your-partner.example -Username laf-svc -LocalFile ./out/sample.dat

# 3. Download + response-processing test (idempotent)
./sftp-download-test.ps1 -Endpoint sftp.your-partner.example -Username laf-svc -RemoteDir /out -LocalDir ./incoming
```

The transfer scripts intentionally **do not** ship a hard-coded credential or auto-run a real
transfer. They detect whether an SFTP client module is present and, if not, print install
guidance and exit cleanly.

## Posh-SSH / WinSCP note

The test scripts use **Posh-SSH** if it is installed
(`Install-Module Posh-SSH -Scope CurrentUser`). **WinSCP** (its .NET assembly or CLI) is an
equally valid client for a real implementation. Neither is bundled. The production transfer
should connect with **key auth + a pinned host key**, supplied from your secret store — see
`sftp-security-checklist.md`. The sample file contract is in `sample-file-contract.md`.
