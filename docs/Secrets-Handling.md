# Secrets Handling

How LocalAIFactory stores, protects, and keeps secrets out of the repository. This reflects the
actual R2-P0B implementation, verified by tests and a release-time no-secrets audit.

Principle: **no secret is ever committed.** Secrets live either encrypted at rest (Data Protection)
or outside the repo entirely (environment / secret store). Committed config files contain
placeholders only.

---

## 1. API keys — encrypted at rest via Data Protection

API keys (e.g. for optional external services) are **encrypted at rest** using ASP.NET Core Data
Protection. They are never stored in plaintext and never committed.

The Data Protection key ring is configured in `src/LocalAIFactory.Web/Program.cs`:

```csharp
var keysDir = Path.Combine(builder.Environment.ContentRootPath, "keys");
Directory.CreateDirectory(keysDir);
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(keysDir))
    .SetApplicationName("LocalAIFactory");
```

- The key ring is persisted to a **git-ignored `keys/`** directory.
- `SetApplicationName("LocalAIFactory")` pins the purpose so keys are not silently shared with
  another app.
- The `keys/` directory is created on startup if missing.

### Operational consequences of the key ring

- **Back it up.** If `keys/` is lost, all values encrypted with that ring (stored API keys) become
  unrecoverable and must be re-entered. Include `keys/` in operational backups (stored securely,
  with restricted ACLs) — but **never** in the git repo.
- **Protect it.** File-system ACLs on `keys/` should restrict access to the app's service account.
- **Per-host by default.** File-system-persisted keys are local to the host; a multi-host
  deployment that must share encrypted values needs a shared, protected key store (out of pilot
  scope today).

---

## 2. Connection strings

Connection strings that carry credentials live in **environment variables or a secret store**,
never in committed config.

- Committed `appsettings.*.example.json` use **Trusted Connection** (integrated Windows auth to
  SQL Server) or **placeholders** only — no real server, user, or password.
- A real deployment supplies the connection string via environment variable or a host-level secret
  store and uses a git-ignored local override for developer machines.
- Prefer Trusted Connection (integrated security) where possible so there is no SQL password to
  store at all.

---

## 3. Git-ignored secret surfaces

The following are git-ignored and must stay that way:

- `keys/` — Data Protection key ring (encryption keys for stored secrets).
- `.env` — local environment/secret overrides.
- Local `appsettings.*.json` overrides that carry real values (the committed files are
  `*.example.json` with placeholders only).

Verify the ignore rules are present before adding any file that could carry a secret. Never use
`git add -f` to force-add an ignored secret file.

---

## 4. No secrets in committed config

The repository contains **example** configuration only:

- `appsettings.*.example.json` — Trusted Connection / placeholder values, safe to commit.
- No API keys, passwords, tokens, or credential-bearing connection strings appear in any committed
  file.

This is enforced two ways: automated **tests** assert the absence of committed secrets, and a
release-time **no-secrets audit** (`Release-Checklist.md`) scans the working tree before tagging.

---

## 5. Secret-store guidance

For a production-grade deployment beyond the pilot:

- Use a host or platform secret store (Windows Credential Manager / DPAPI-backed store, a managed
  vault, or environment variables injected by the host) for connection strings and API keys.
- Keep the Data Protection key ring on protected storage; if moving to multi-host, back the ring
  with a shared protected store rather than per-host files.
- Grant the application service account least-privilege access to SQL Server.

---

## 6. Rotation

- **API keys:** rotate by entering the new key in-app (it is re-encrypted with the current Data
  Protection ring) and revoking the old key at the provider. No code change or redeploy required.
- **Connection-string credentials:** rotate at the SQL Server / directory side, then update the
  environment variable or secret store and restart the app. Prefer Trusted Connection to avoid
  storing a rotatable password at all.
- **Data Protection key ring:** Data Protection rolls keys automatically on its own schedule;
  retain superseded keys in `keys/` so previously encrypted values remain decryptable. Do not
  delete old key-ring files without first re-encrypting everything that depends on them.

---

## 7. Honest notes

- There is **no automated secret-rotation pipeline** and **no integration with an external vault**
  in the pilot; rotation is the manual procedure above.
- Secret protection depends on file-system ACLs around `keys/` and on operators not committing
  overrides. The no-secrets audit catches accidental commits; it does not harden the host.
