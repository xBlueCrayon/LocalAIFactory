<#
.SYNOPSIS  Backup / restore / health helpers for this generated ERP (SQLite portable mode + SQL Server note).
.PARAMETER Action  backup | restore | health
#>
param([ValidateSet("backup","restore","health")][string]$Action = "health",
      [string]$Db = "src/LafErp.Web/laferp.db", [string]$To = "", [string]$Url = "http://localhost:5000")
$repo = (Resolve-Path "$PSScriptRoot/..").Path
switch ($Action) {
  "backup" {
    $src = Join-Path $repo $Db
    if (-not (Test-Path $src)) { Write-Host "no SQLite db at $src (SQL Server: use SQL BACKUP DATABASE)"; break }
    $dst = if ($To) { $To } else { Join-Path $repo ("backup-" + (Get-Item $src).LastWriteTime.ToString("yyyyMMddHHmmss") + ".db") }
    Copy-Item $src $dst -Force; Write-Host "backed up -> $dst"
  }
  "restore" {
    if (-not $To -or -not (Test-Path $To)) { Write-Host "pass -To <backup.db>"; break }
    Copy-Item $To (Join-Path $repo $Db) -Force; Write-Host "restored from $To"
  }
  "health" {
    try { $r = Invoke-WebRequest "$Url/api/health" -UseBasicParsing -TimeoutSec 8; Write-Host "health: $($r.StatusCode)" }
    catch { try { $d = Invoke-WebRequest "$Url/" -UseBasicParsing -TimeoutSec 8; Write-Host "dashboard: $($d.StatusCode)" } catch { Write-Host "unreachable: $($_.Exception.Message)" } }
  }
}
# SQL Server production: use `sqlcmd ... BACKUP DATABASE [LafErpGold] TO DISK=...` and `RESTORE DATABASE`.
