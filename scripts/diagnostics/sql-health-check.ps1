<#
.SYNOPSIS R2-ACC-20X: SQL Server connectivity + size health (read-only). Never drops/alters anything.
.PARAMETER Server  SQL instance (default LocalDB).
.PARAMETER Database Database name (default LocalAIFactory).
#>
param([string]$Server = "(localdb)\MSSQLLocalDB", [string]$Database = "LocalAIFactory")
$sqlcmd = Get-Command sqlcmd -ErrorAction SilentlyContinue
if (-not $sqlcmd) { Write-Host "sqlcmd not found — install SQL command-line tools to run this check." -ForegroundColor Yellow; exit 0 }
$q = "SET NOCOUNT ON; SELECT 'reachable' AS s; SELECT name FROM sys.databases WHERE name = '$Database';"
$out = & sqlcmd -S $Server -d master -E -b -Q $q 2>&1
if ($LASTEXITCODE -ne 0) { Write-Host "SQL not reachable on $Server" -ForegroundColor Red; Write-Host $out; exit 1 }
Write-Host "SQL reachable on $Server" -ForegroundColor Green
if ($out -match $Database) {
  $size = & sqlcmd -S $Server -d $Database -E -b -h -1 -Q "SET NOCOUNT ON; SELECT CAST(SUM(size)*8.0/1024 AS DECIMAL(10,1)) FROM sys.database_files;" 2>&1
  Write-Host ("Database '{0}' present (~{1} MB)." -f $Database, ($size.Trim()))
} else {
  Write-Host ("Database '{0}' not found — run database/create-localdb.ps1 to create it." -f $Database) -ForegroundColor Yellow
}
Write-Host "SQL-HEALTH: OK" -ForegroundColor Green
