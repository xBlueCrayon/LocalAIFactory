<#
.SYNOPSIS  Diagnose local Ollama availability + installed models. READ-ONLY. Optional component.
#>
param([string[]]$Expected=@("qwen2.5-coder:14b","nomic-embed-text"))
Write-Host "== Ollama model check ==" -ForegroundColor Cyan
try{ $t=Invoke-RestMethod "http://localhost:11434/api/tags" -TimeoutSec 5; $names=$t.models.name }
catch{ Write-Host "  [INFO] Ollama not reachable (OPTIONAL — app runs MSSQL-only without it)" -ForegroundColor Yellow; exit 0 }
Write-Host "  installed: $($names -join ', ')"
foreach($m in $Expected){ if($names -contains $m){ Write-Host "  [ OK ] $m present" -ForegroundColor Green } else { Write-Host "  [INFO] $m NOT pulled (optional; pull with: ollama pull $m)" -ForegroundColor Yellow } }
