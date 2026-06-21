<#
.SYNOPSIS  MODE-A: verify the IIS-proof SQL Express database. READ-ONLY.
.DESCRIPTION Confirms the deployment database is migrated + seeded (migrations, packs, items, distinct/duplicate
             Uids) and reports the IIS app-pool login's database roles. Changes nothing. Non-zero on failure.
.PARAMETER Server / Database  SQL target. .PARAMETER AppPoolName  app-pool login to report. .PARAMETER ExpectPacks/ExpectItems
#>
param(
  [string]$Server = ".\SQLEXPRESS",
  [string]$Database = "LocalAIFactory_IISProof",
  [string]$AppPoolName = "LocalAIFactoryPilotPool",
  [int]$ExpectPacks = 4,
  [int]$ExpectItems = 438
)
$fail = 0
function Ok($m){ Write-Host "  [ OK ] $m" -ForegroundColor Green }
function Bad($m){ Write-Host "  [FAIL] $m" -ForegroundColor Red; $script:fail++ }
Write-Host "== Verify IIS-proof DB ($Server / $Database) ==" -ForegroundColor Cyan

$migs  = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.__EFMigrationsHistory" -h -1 2>$null | Out-String).Trim()
$packs = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgePacks" -h -1 2>$null | Out-String).Trim()
$items = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM dbo.KnowledgeItems WHERE KnowledgePackId IS NOT NULL" -h -1 2>$null | Out-String).Trim()
$uids  = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(DISTINCT Uid) FROM dbo.KnowledgeItems WHERE KnowledgePackId IS NOT NULL" -h -1 2>$null | Out-String).Trim()
$dups  = (sqlcmd -S $Server -d $Database -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM (SELECT Uid FROM dbo.KnowledgeItems GROUP BY Uid HAVING COUNT(*)>1) d" -h -1 2>$null | Out-String).Trim()

if ($migs -match '^\d+$' -and [int]$migs -ge 1) { Ok "migrations applied = $migs" } else { Bad "migrations = '$migs'" }
if ($packs -match '^\d+$' -and [int]$packs -ge $ExpectPacks) { Ok "installed packs = $packs (>= $ExpectPacks)" } else { Bad "packs = '$packs' (expected >= $ExpectPacks)" }
if ($items -match '^\d+$' -and [int]$items -ge $ExpectItems) { Ok "pack items = $items (>= $ExpectItems)" } else { Bad "items = '$items' (expected >= $ExpectItems)" }
if ($items -eq $uids) { Ok "distinct Uids = $uids (matches item count)" } else { Bad "distinct Uids $uids != items $items" }
if ($dups -eq "0") { Ok "no duplicate Uids" } else { Bad "duplicate Uids found: $dups" }

$login = "IIS APPPOOL\$AppPoolName"
$roles = (sqlcmd -S $Server -d $Database -h -1 -W -Q "SET NOCOUNT ON; SELECT r.name FROM sys.database_role_members m JOIN sys.database_principals r ON r.principal_id=m.role_principal_id JOIN sys.database_principals u ON u.principal_id=m.member_principal_id WHERE u.name=N'$login'" 2>$null | Where-Object { $_ -and $_ -notmatch 'rows affected' }) -join ', '
if ($roles) { Ok "app-pool login '$login' roles: $($roles.Trim())" } else { Write-Host "  [INFO] app-pool login not yet granted (run grant-iis-apppool-sql-access.ps1)" -ForegroundColor Yellow }

Write-Host "`nVERIFY-IIS-SQLEXPRESS-PROOF: $(if ($fail -eq 0) { 'PASS' } else { "FAIL ($fail)" })" -ForegroundColor ($fail -eq 0 ? "Green":"Red")
exit ([int]($fail -ne 0))
