# SQL Server Deployment Guide — LocalAIFactory

MSSQL is the **source of truth** for LocalAIFactory. This guide covers the three supported engines,
authentication, the create-if-absent contract, additive migrations, encryption, and verification —
all grounded in the committed scripts under `database/`.

---

## 1. Which engine?

| Engine | Default instance | Use | Create script |
|--------|------------------|-----|---------------|
| **LocalDB** | `(localdb)\MSSQLLocalDB` | Developer / demo | `database/create-localdb.ps1` |
| **SQL Express** | `.\SQLEXPRESS` | Pilot | `database/create-sqlexpress-db.ps1` |
| **Full SQL Server** | `localhost` (or your host) | Pilot / production | `database/create-full-mssql-db.ps1` |

All three share the **same safe contract** (see §3). The full-server script is a thin wrapper over the
Express script with a different default instance.

---

## 2. Trusted vs SQL authentication

**Trusted (Windows/Integrated) connection is the default everywhere** — no password is stored:

```powershell
# LocalDB
pwsh database/create-localdb.ps1 -Instance "(localdb)\MSSQLLocalDB" -Database "LocalAIFactory"

# SQL Express (trusted)
pwsh database/create-sqlexpress-db.ps1 -Instance ".\SQLEXPRESS" -Database "LocalAIFactory"

# Full SQL Server (trusted)
pwsh database/create-full-mssql-db.ps1 -Instance "YOUR_SQL_HOST" -Database "LocalAIFactory"
```

SQL authentication is **supported but never forced and never stored**. Pass `-User`/`-Password` only
when integrated auth is not possible:

```powershell
pwsh database/create-full-mssql-db.ps1 -Instance "YOUR_SQL_HOST" -Database "LocalAIFactory" `
  -User "app_user" -Password "<from-secret-store>"
```

### Secret handling (never commit credentials)

- The example configs default to `Trusted_Connection=True` and contain **no secrets**:
  `database/appsettings.LocalDB.example.json`, `appsettings.SqlExpress.example.json`,
  `appsettings.FullSqlServer.example.json`.
- If SQL auth is mandated, **inject the full connection string from an environment variable or secret
  store at deploy time** — never put credentials in committed config. The app reads
  `ConnectionStrings__DefaultConnection` from the environment (the migration scripts set exactly this).

```powershell
# Example: supply the connection string from the environment, not from a file
$env:ConnectionStrings__DefaultConnection = "Server=...;Database=LocalAIFactory;User Id=...;Password=...;Encrypt=True;TrustServerCertificate=False;MultipleActiveResultSets=true"
```

---

## 3. Create-if-absent contract (never drops)

Every create script is **non-destructive**:

- It checks `IF DB_ID('<Database>') IS NULL` and **creates the database only if it does not exist**.
- If the database already exists, it **migrates only** and prints `exists (no drop)` — it never drops,
  truncates, or overwrites.

This is why re-running an installer is safe: it converges on the current schema without risking data.

---

## 4. Migrations are additive

`database/apply-migrations.ps1` runs `dotnet ef database update` against the target connection:

```powershell
pwsh database/apply-migrations.ps1 `
  -ConnectionString "Server=.\SQLEXPRESS;Database=LocalAIFactory;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
```

Policy:

- Migrations are **additive and backward-compatible**. EF creates the database if absent; it
  **never drops or truncates**.
- **No destructive schema change** (drop/rename column or table, lossy type change, data deletion)
  ships without explicit human approval.
- The connection comes from `-ConnectionString` or the `ConnectionStrings__DefaultConnection`
  environment variable.

The migration log masks any `Password=...` token before printing the target connection.

---

## 5. Collation and encryption notes

- **Collation:** the create scripts do not force a server collation; the database is created with the
  server default. If your estate standardises on a specific collation, create the database with that
  collation **before** running migrations (the scripts will then migrate the existing database rather
  than create one).
- **Encryption in transit:** the **FullSqlServer** example uses
  `Encrypt=True;TrustServerCertificate=False` — TLS is on and the server certificate is validated.
  This requires a trusted certificate chain on the SQL host.
- The **LocalDB/Express** examples use `TrustServerCertificate=True`, appropriate for a local instance
  where the certificate is self-signed. **For production, prefer `Encrypt=True` with
  `TrustServerCertificate=False`** and a properly issued certificate.
- `MultipleActiveResultSets=true` is set in all connection strings (the app relies on MARS).

---

## 6. Verify

After create + migrate + seed, verify the knowledge base (read-only — safe anywhere):

```powershell
pwsh database/verify-knowledge-base.ps1 -ServerInstance ".\SQLEXPRESS" -Database "LocalAIFactory"
```

It checks, read-only:

- a `KnowledgePack` row exists,
- baseline item count ≥ `-MinItems` (default 100),
- no duplicate `Uid`s,
- all baseline items are curated (`Tier = 1`),
- pack-origin provenance is present,
- the source registry (`src:` tags) is referenced.

Live-proven on this host: **VERIFIED at 390 items**, all curated, no duplicate Uids. The script exits
non-zero on any failed check, so it is suitable as a deployment gate.

---

## 7. Seeding (idempotent)

The app seeds and installs the knowledge pack **idempotently on startup**. To do it as a scripted step:

```powershell
pwsh database/seed-professional-knowledge-base.ps1 -Instance ".\SQLEXPRESS" -Database "LocalAIFactory"
```

This applies migrations, starts the app briefly to install the pack, waits until at least 100 pack
items are present (or times out), stops the app, and verifies. **Re-running does not duplicate items.**
