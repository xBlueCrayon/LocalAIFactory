<#
.SYNOPSIS  Adaptive (no fixed limit) ERP generation loop. Regenerate -> build -> test, measure progress,
           and stop ONLY on a documented convergence condition (see UNLIMITED_ERP_GENERATION_CONVERGENCE_POLICY.md).
.DESCRIPTION Each iteration regenerates ERP V3 from the templates + module spec + governed local-LLM proposal,
             builds, and runs the .NET tests, recording the passing-test count as the progress metric. The loop
             stops when two consecutive iterations show no measurable improvement (Stop B), a hard blocker is hit
             (Stop C), or a safety cap is reached (Stop D). Deterministic regeneration is expected to converge
             quickly — further gains require generator/knowledge/spec changes (the meta-loop the operator drives).
.PARAMETER SafetyCap  Absolute max iterations as a runtime safeguard (Stop D), not a target. Default 8.
#>
param(
    [string]$Target = "generated-products/LAF-EnterpriseERP-V3",
    [string]$ProductName = "LAF Enterprise ERP V3",
    [string]$Spec = "tools/LocalAIFactory.Generator/specs/erpnext-grade-modules.json",
    [string]$Requirement = "benchmarks/erpnext-study/laf-erp-v3-production-grade-requirement.md",
    [int]$SafetyCap = 8
)
$repo = (Resolve-Path "$PSScriptRoot/../..").Path
Set-Location $repo
$slnx = Join-Path $Target ((Split-Path $Target -Leaf) + ".slnx")
$testProj = Join-Path $Target "tests/LafErp.Tests/LafErp.Tests.csproj"
$loop = @{ policy = "adaptive-no-fixed-limit"; iterations = @() }
$noImprove = 0; $prevPass = -1; $stop = $null

for ($i = 1; $i -le $SafetyCap; $i++) {
    Write-Host "==== adaptive iteration $i ====" -ForegroundColor Cyan
    & dotnet run --project tools/LocalAIFactory.Generator -c Release -- --requirement $Requirement --module-spec $Spec --target $Target --product-name $ProductName --prefer-local-llm --attribution benchmarks/results/laf-erp-v3-generation-attribution.json --summary benchmarks/results/laf-erp-v3-generation-summary.json *> $null
    $b = & dotnet build $slnx -c Release --nologo 2>&1 | Out-String
    $build = ($b -match '0 Error\(s\)') ? "ok" : "fail"
    $pass = 0; $tstat = "n/a"
    if ($build -eq "ok") {
        $t = & dotnet test $testProj -c Release --nologo 2>&1 | Out-String
        if ($t -match 'Passed:\s+(\d+)') { $pass = [int]$Matches[1] }
        $tstat = ($t -match 'Passed!') ? "green" : "red"
    }
    $improved = ($pass -gt $prevPass)
    $it = [ordered]@{ iteration = $i; build = $build; tests = $tstat; passingTests = $pass; improvedVsPrev = $improved }
    $loop.iterations += $it
    Write-Host "  build=$build tests=$tstat passing=$pass improvedVsPrev=$improved" -ForegroundColor Gray

    if ($build -ne "ok" -or $tstat -eq "red") { $stop = "C: build/test not green - hard blocker, needs a generator fix"; break }
    if ($improved) { $noImprove = 0 } else { $noImprove++ }
    $prevPass = $pass
    if ($noImprove -ge 2) { $stop = "B: two consecutive iterations with no measurable improvement (converged at the generator's current capability)"; break }
    if ($i -eq $SafetyCap) { $stop = "D: safety cap reached (runtime safeguard, not a target)" }
}
$loop.stopReason = $stop
$loop.finalPassingTests = $prevPass
$loop | ConvertTo-Json -Depth 6 | Set-Content (Join-Path $repo "benchmarks/results/laf-erp-unlimited-generation-loop.json")
Write-Host "ADAPTIVE-LOOP STOP -> $stop" -ForegroundColor Yellow
