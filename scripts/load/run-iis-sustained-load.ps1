<#
.SYNOPSIS  Sustained load simulation (longer duration) against the IIS pilot. Wrapper over run-iis-smoke-load.
.DESCRIPTION Default 5 minutes at moderate concurrency to check stability over time. LOCAL simulation only.
#>
param([string]$AppUrl = "https://localhost:8443", [int]$Concurrency = 15, [int]$DurationSeconds = 300)
& "$PSScriptRoot/run-iis-smoke-load.ps1" -AppUrl $AppUrl -Concurrency $Concurrency -DurationSeconds $DurationSeconds -Suite "iis-sustained-load"
