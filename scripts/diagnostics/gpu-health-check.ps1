<#
.SYNOPSIS R2-ACC-20X: NVIDIA GPU health (read-only). Degrades gracefully when no GPU/driver is present.
#>
$nvsmi = Get-Command nvidia-smi -ErrorAction SilentlyContinue
if (-not $nvsmi) { Write-Host "No nvidia-smi found — GPU optional; local AI will run on CPU or be disabled." -ForegroundColor Yellow; exit 0 }
$line = & nvidia-smi --query-gpu=name,driver_version,memory.total,memory.used,utilization.gpu,temperature.gpu --format=csv,noheader,nounits 2>$null | Select-Object -First 1
if (-not $line) { Write-Host "nvidia-smi present but returned no GPU." -ForegroundColor Yellow; exit 0 }
$p = $line -split ','
Write-Host ("GPU:    {0}" -f $p[0].Trim()) -ForegroundColor Cyan
Write-Host ("Driver: {0}" -f $p[1].Trim())
Write-Host ("VRAM:   {0} / {1} MB used" -f $p[3].Trim(), $p[2].Trim())
Write-Host ("Util:   {0}%   Temp: {1} C" -f $p[4].Trim(), $p[5].Trim())
Write-Host "GPU-HEALTH: OK" -ForegroundColor Green
