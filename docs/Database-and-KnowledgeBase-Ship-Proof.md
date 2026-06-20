# Database & Knowledge-Base — Ship Proof

Exact commands and **observed outputs** (captured on the reference workstation, LocalDB, 2026-06-21). These are
real runs, not illustrations. MSSQL is the source of truth; the knowledge pack ships as source-controlled JSON
and is installed + verified in MSSQL.

## 1. Knowledge-base verification (read-only)

```powershell
./database/verify-knowledge-base.ps1
```
Observed:
```
== Knowledge-base verification: [LocalAIFactory] on (localdb)\MSSQLLocalDB ==
  [PASS] KnowledgePack installed (1 pack row(s))
  [PASS] Baseline item count (390 items (min 100))
  [PASS] No duplicate Uids (390 distinct of 390)
  [PASS] All baseline items curated (390 curated)
  [PASS] Pack-origin provenance present (390 provenance events)
  [PASS] Source registry referenced (17 src: tags)
  [PASS] Baseline distinct from imported (390 baseline vs 1035 imported-project)
KNOWLEDGE-BASE: VERIFIED
```

## 2. Backup (non-destructive)

```powershell
./database/backup-database.ps1 -BackupDir ./backups
```
Observed:
```
Processed 8880 pages for database 'LocalAIFactory' ...
BACKUP DATABASE successfully processed 8882 pages in 0.051 seconds (1360.523 MB/sec).
BACKUP OK: ...\LocalAIFactory-20260621-015734.bak   (69.5 MB)
```
> Note: `WITH COMPRESSION` is **not** supported on Express/LocalDB — the script omits it by default and
> exposes `-Compress` for editions that support it. This edition limit was found and handled during testing.

## 3. Restore verification (a backup is unproven until verified)

```powershell
./database/restore-verify-database.ps1 -BackupFile ./backups/LocalAIFactory-<stamp>.bak
```
Observed:
```
RESTORE VERIFYONLY on ...\LocalAIFactory-20260621-015734.bak
The backup set on file 1 is valid.
VERIFY OK
```

## 4. Derived-data reset (dry-run; curated knowledge never touched)

```powershell
./database/reset-derived-indexes.ps1
```
Observed:
```
Derived structural rows: CodeEdges=391 CodeSymbolReferences=1022 CodeSymbols=1772
Protected (never touched): KnowledgeItems, BusinessRules, ApprovedCodeSnippets, AuditEvents, ProvenanceEvents, ImportedFiles.
DRY-RUN: nothing cleared. ...
```

## 5. First install from scratch (the shipped path)

```powershell
./database/create-localdb.ps1                       # create-if-absent + apply migrations (additive)
./database/seed-professional-knowledge-base.ps1     # migrate + start app to install pack + verify
./database/verify-knowledge-base.ps1                # re-verify (read-only)
```
The app applies migrations, seeds reference data, and installs the knowledge pack **idempotently** on startup
(re-running does not duplicate items — proven by `KnowledgePackTests` and the "no duplicate Uids" check above).

## Safety properties demonstrated

- No `DROP DATABASE` anywhere; create scripts CREATE-if-absent.
- Backups/restores are non-destructive; restore targets a **verify** database (refuses production).
- Derived reset is dry-run by default and never touches curated knowledge, audit, or provenance.
- No passwords in any script or `appsettings.*.example.json`; trusted connection by default.

## Honest limitations

- These runs are on **LocalDB**. SQL Express and full SQL Server use the same scripts and contract but were not
  executed in this environment (no Express/full instance installed here); the create scripts detect reachability
  and fail clearly if the instance is absent. Proof-to-close: run `create-sqlexpress-db.ps1` /
  `create-full-mssql-db.ps1` against a real instance and capture the same verification output.
- `seed-professional-knowledge-base.ps1` starts the web app to perform the install (that is where the app's
  idempotent installer runs); a headless seeding entry point is a possible future refinement.
