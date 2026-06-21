<#
.SYNOPSIS  LocalAIFactory generated-ERP fix loop: re-generate, build, test, Playwright, and report failures.
.DESCRIPTION Drives the LocalAIFactory generator and validates its output. On failure it surfaces the
             diagnostics that feed the next generator fix (the fix is applied to the GENERATOR/templates,
             never hand-edited into the generated product, preserving generation autonomy). READ-MOSTLY:
             the only thing it writes is the regenerated product + a JSON status.
.PARAMETER MaxIterations  Max regenerate/validate cycles (default 5).
#>
param(
    [string]$Target = "generated-products/LAF-EnterpriseERP-LAFGenerated",
    [string]$ProductName = "LAF Enterprise ERP V2",
    [int]$MaxIterations = 5,
    [switch]$RunPlaywright
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $repo
$slnx = Join-Path $Target "LAF-EnterpriseERP-LAFGenerated.slnx"
$status = @{ iterations = @() }

for ($i = 1; $i -le $MaxIterations; $i++) {
    Write-Host "==== fix-loop iteration $i ====" -ForegroundColor Cyan
    $it = [ordered]@{ iteration = $i; generate = "?"; build = "?"; test = "?"; playwright = "skipped"; failures = @() }

    # 1. (Re)generate from templates + governed LLM proposal
    & dotnet run --project tools/LocalAIFactory.Generator -c Release -- --target $Target --product-name $ProductName --prefer-local-llm *> $null
    $it.generate = ($LASTEXITCODE -eq 0) ? "ok" : "fail"

    # 2. Build
    $b = & dotnet build $slnx -c Release --nologo 2>&1 | Out-String
    $it.build = ($b -match '0 Error\(s\)') ? "ok" : "fail"
    if ($it.build -eq "fail") { $it.failures += ([regex]::Matches($b, 'error \w+:.*') | ForEach-Object { $_.Value } | Select-Object -First 5) }

    # 3. Test
    if ($it.build -eq "ok") {
        $t = & dotnet test (Join-Path $Target "tests/LafErp.Tests/LafErp.Tests.csproj") -c Release --nologo 2>&1 | Out-String
        $it.test = ($t -match 'Passed!') ? "ok" : "fail"
        if ($it.test -eq "fail") { $it.failures += ([regex]::Matches($t, 'Failed LafErp[^\r\n]*') | ForEach-Object { $_.Value } | Select-Object -First 8) }
    }

    # 4. Playwright (optional)
    if ($RunPlaywright -and $it.build -eq "ok" -and $it.test -eq "ok") {
        Push-Location (Join-Path $Target "playwright")
        $p = & npx playwright test 2>&1 | Out-String
        $it.playwright = ($p -match '(\d+) passed' -and $p -notmatch 'failed') ? "ok" : "fail"
        Pop-Location
    }

    $status.iterations += $it
    if ($it.build -eq "ok" -and $it.test -eq "ok") {
        Write-Host "  GREEN at iteration $i" -ForegroundColor Green
        $status.finalStatus = "GREEN"; break
    }
    Write-Host "  failures -> feed to generator fix, then re-run:" -ForegroundColor Yellow
    $it.failures | ForEach-Object { Write-Host "    $_" }
    $status.finalStatus = "NEEDS_GENERATOR_FIX"
}

$status | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $repo "benchmarks/results/laf-erp-v2-fix-loop-status.json")
Write-Host "FIX-LOOP: $($status.finalStatus)" -ForegroundColor ($status.finalStatus -eq "GREEN" ? "Green" : "Yellow")
exit ([int]($status.finalStatus -ne "GREEN"))
