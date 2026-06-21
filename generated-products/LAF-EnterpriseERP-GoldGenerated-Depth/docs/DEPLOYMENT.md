# LAF Enterprise ERP GoldGenerated-Depth — Local Production Deployment

This guide deploys the ERP locally. It runs **portable** on SQLite with zero configuration, or on
**SQL Server / SQL Express** with a committed EF Core migration history. No internet or paid service is required.

## 1. Prerequisites

- .NET 10 SDK
- (SQL Server mode) SQL Server or SQL Express, plus the EF tool: `dotnet tool install --global dotnet-ef`

## 2. Choose a database mode

| Mode | When | Schema |
|------|------|--------|
| SQLite (portable) | demos, single-box, tests | `EnsureCreated` at startup (no migration history) |
| SQL Server / Express | production | committed EF migrations via `Database.Migrate()` |

For SQL Server, set the connection string before running:

```powershell
$env:ConnectionStrings__Default = "Server=.\SQLEXPRESS;Database=LafErpGold;Trusted_Connection=True;TrustServerCertificate=True"
```

## 3. Prepare the database (SQL Server / Express)

```powershell
./scripts/setup-sql-express.ps1          # creates the database if absent, then applies migrations
# or, against an existing DB:
./scripts/apply-migrations.ps1 -Connection "<your connection string>"
# review the schema as SQL first:
./scripts/generate-sql-script.ps1        # writes db/laferp-gold-schema.sql
```

The portable SQLite mode needs none of the above — the schema is created on first run.

## 4. Publish and run

```powershell
./scripts/publish-local-production.ps1   # Release publish to a local folder
# then, from the publish folder:
dotnet LafErp.Web.dll                     # serves http://localhost:5000
```

## 5. First login

Seeded demo users (PBKDF2-hashed): `admin / Admin#12345`, `alice / Alice#12345`, `bob / Bob#12345`.
**Change these before any real use.** Auth is hardened: failed-login lockout, password policy,
anti-forgery on forms, secure sliding-session cookie, and audited login/logout/lockout/reset events.

## 6. Operations

```powershell
./scripts/run-production-smoke.ps1       # health + real login + no-HTTP-500 smoke proof
./scripts/backup-db.ps1                  # backup (SQLite copy; SQL Server BACKUP DATABASE guidance)
./scripts/restore-db.ps1 -From <path>    # restore
./scripts/backup-restore-health.ps1 -Action health   # health probe
```

## 7. Security checklist before production

- [ ] Replace all seeded passwords; set `MustChangePassword` where needed.
- [ ] Run behind HTTPS (a reverse proxy or Kestrel TLS); the auth cookie upgrades to `Secure` automatically.
- [ ] Restrict the SQL login to the application database.
- [ ] Schedule `backup-db.ps1` (or SQL Server Agent BACKUP).

## 8. Honest limitations (not yet in scope)

- No MFA / SSO / OIDC / Windows authentication (documented extension points only).
- Manufacturing, HR/payroll, POS and e-commerce are CRUD skeletons, not full engines.
- TLS here is self-managed; a CA-signed certificate and an external security review are still required for
  a regulated production rollout.
