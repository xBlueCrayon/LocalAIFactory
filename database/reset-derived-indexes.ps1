<#
.SYNOPSIS  R2-ACC-INDUSTRIAL: reset REGENERABLE derived structural data (symbols/edges/references).
.DESCRIPTION DRY-RUN by default: reports how many derived rows exist. These are rebuildable-from-raw on the
             next import/consolidation. -Execute clears ONLY derived structural tables — it NEVER touches
             KnowledgeItems, curated knowledge, audit, provenance, or imported raw artifacts. No DROP DATABASE.
#>
param([string]$ServerInstance = "(localdb)\MSSQLLocalDB", [string]$Database = "LocalAIFactory", [switch]$Execute)
$ErrorActionPreference = "Stop"
function N($t) { [int]((sqlcmd -S "$ServerInstance" -d $Database -E -C -h -1 -W -Q "SET NOCOUNT ON; SELECT COUNT(*) FROM $t;" 2>$null | Select-Object -First 1).Trim()) }
$edges = N "CodeEdges"; $refs = N "CodeSymbolReferences"; $syms = N "CodeSymbols"
Write-Host "Derived structural rows: CodeEdges=$edges CodeSymbolReferences=$refs CodeSymbols=$syms" -ForegroundColor Cyan
Write-Host "Protected (never touched): KnowledgeItems, BusinessRules, ApprovedCodeSnippets, AuditEvents, ProvenanceEvents, ImportedFiles." -ForegroundColor Yellow
if (-not $Execute) { Write-Host "DRY-RUN: nothing cleared. Re-run with -Execute to clear derived structural rows (they rebuild on next import)." -ForegroundColor Green; exit 0 }
Write-Host "Clearing derived structural rows (regenerable)..." -ForegroundColor Cyan
sqlcmd -S "$ServerInstance" -d $Database -E -C -b -Q "DELETE FROM CodeEdges; DELETE FROM CodeSymbolReferences; DELETE FROM CodeSymbols;"
if ($LASTEXITCODE -eq 0) { Write-Host "Derived structural data cleared. It will rebuild on the next import/consolidation." -ForegroundColor Green; exit 0 } else { exit 1 }
