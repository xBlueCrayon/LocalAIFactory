<#
.SYNOPSIS
  R2-ACC-20X: read-only system resource snapshot (CPU / RAM / disk / GPU / top processes).
.DESCRIPTION
  Safe, non-destructive. Prints a one-shot snapshot of the host suitable for a support bundle. No changes made.
#>
param([switch]$Json)

$ErrorActionPreference = "SilentlyContinue"
function GB($b) { [math]::Round($b/1GB,1) }

$os  = Get-CimInstance Win32_OperatingSystem
$cpu = Get-CimInstance Win32_Processor | Select-Object -First 1
$cs  = Get-CimInstance Win32_ComputerSystem
$cpuLoad = (Get-CimInstance Win32_Processor | Measure-Object -Property LoadPercentage -Average).Average

$snap = [ordered]@{
  TimestampUtc = (Get-Date).ToUniversalTime().ToString("s") + "Z"
  Machine      = $env:COMPUTERNAME
  OS           = $os.Caption + " " + $os.Version
  CPU          = $cpu.Name.Trim()
  CpuCores     = $cpu.NumberOfCores
  CpuLogical   = $cpu.NumberOfLogicalProcessors
  CpuLoadPct   = $cpuLoad
  RamTotalGB   = GB ($cs.TotalPhysicalMemory)
  RamFreeGB    = GB ($os.FreePhysicalMemory * 1KB)
}

# Disks
$disks = Get-CimInstance Win32_LogicalDisk -Filter "DriveType=3" |
  ForEach-Object { [ordered]@{ Drive=$_.DeviceID; FreeGB=GB $_.FreeSpace; TotalGB=GB $_.Size } }

# GPU (NVIDIA, optional)
$gpu = $null
$nvsmi = Get-Command nvidia-smi -ErrorAction SilentlyContinue
if ($nvsmi) {
  $line = & nvidia-smi --query-gpu=name,memory.total,memory.used,utilization.gpu --format=csv,noheader,nounits 2>$null | Select-Object -First 1
  if ($line) { $p = $line -split ','; $gpu = [ordered]@{ Name=$p[0].Trim(); VramTotalMB=[int]$p[1].Trim(); VramUsedMB=[int]$p[2].Trim(); UtilPct=[int]$p[3].Trim() } }
}

# Top 5 processes by working set
$top = Get-Process | Sort-Object WorkingSet64 -Descending | Select-Object -First 5 |
  ForEach-Object { [ordered]@{ Name=$_.ProcessName; Pid=$_.Id; RamMB=[math]::Round($_.WorkingSet64/1MB,0) } }

$result = [ordered]@{ System=$snap; Disks=$disks; Gpu=$gpu; TopProcesses=$top }

if ($Json) { $result | ConvertTo-Json -Depth 5; return }

Write-Host "== System ==" -ForegroundColor Cyan
$snap.GetEnumerator() | ForEach-Object { "{0,-14} {1}" -f $_.Key, $_.Value }
Write-Host "`n== Disks ==" -ForegroundColor Cyan
$disks | ForEach-Object { "{0}  {1} GB free / {2} GB" -f $_.Drive, $_.FreeGB, $_.TotalGB }
Write-Host "`n== GPU ==" -ForegroundColor Cyan
if ($gpu) { "{0}  VRAM {1}/{2} MB  util {3}%" -f $gpu.Name, $gpu.VramUsedMB, $gpu.VramTotalMB, $gpu.UtilPct } else { "No NVIDIA GPU detected (nvidia-smi absent)." }
Write-Host "`n== Top processes (working set) ==" -ForegroundColor Cyan
$top | ForEach-Object { "{0,-22} pid {1,-7} {2} MB" -f $_.Name, $_.Pid, $_.RamMB }
