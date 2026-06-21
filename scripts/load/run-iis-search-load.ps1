<#
.SYNOPSIS  Search-heavy load simulation (DB-backed Base Knowledge search) against the IIS pilot. Wrapper over run-iis-smoke-load.
#>
param([string]$AppUrl = "https://localhost:8443", [int]$Concurrency = 20, [int]$DurationSeconds = 60)
& "$PSScriptRoot/run-iis-smoke-load.ps1" -AppUrl $AppUrl -Concurrency $Concurrency -DurationSeconds $DurationSeconds -Suite "iis-search-load" `
  -Paths @("/BaseKnowledge?q=OCR","/BaseKnowledge?q=Mauritius","/BaseKnowledge?q=market","/BaseKnowledge?q=insurance","/BaseKnowledge?q=leasing","/BaseKnowledge?q=VB6")
