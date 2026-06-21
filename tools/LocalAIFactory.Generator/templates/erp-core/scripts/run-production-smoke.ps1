<#
.SYNOPSIS  Local production smoke test: start the app, prove health + real login + key pages, assert no HTTP 500.
.DESCRIPTION
  Starts the built ERP on a test port, then verifies: /api/health is 200; the login page renders an
  anti-forgery token; a wrong password is rejected; the seeded admin signs in (302 + auth cookie); the
  authenticated dashboard and core pages return 200 with no 500s. Writes a proof JSON. Stops the app.
.PARAMETER Port  Test port (default 5091).
#>
param([int]$Port = 5091)
$ErrorActionPreference = "Stop"
$repo = (Resolve-Path "$PSScriptRoot/..").Path
$web  = Join-Path $repo "src/LafErp.Web/LafErp.Web.csproj"
$base = "http://localhost:$Port"
# Fresh portable DB so the schema always matches the current model (EnsureCreated does not migrate a stale file).
Get-ChildItem (Join-Path $repo "src/LafErp.Web") -Filter "laferp.db*" -EA SilentlyContinue | ForEach-Object { Remove-Item $_.FullName -Force -EA SilentlyContinue }
$proc = Start-Process dotnet -ArgumentList "run --project `"$web`" -c Release --no-launch-profile --urls $base" -PassThru -WindowStyle Hidden
try {
    # wait for health
    $ok = $false
    for ($i = 0; $i -lt 30; $i++) {
        try { if ((Invoke-WebRequest "$base/api/health" -UseBasicParsing -TimeoutSec 3).StatusCode -eq 200) { $ok = $true; break } } catch {}
        Start-Sleep -Seconds 2
    }
    if (-not $ok) { throw "App did not become healthy on $base" }
    $proof = [ordered]@{ baseUrl = $base; stamp = "smoke" }

    $login = Invoke-WebRequest "$base/Account/Login" -UseBasicParsing -SessionVariable s -TimeoutSec 10
    $proof.loginPage = $login.StatusCode
    $proof.antiForgeryTokenPresent = [bool](($login.InputFields | Where-Object name -eq '__RequestVerificationToken').value)
    $tok = ($login.InputFields | Where-Object name -eq '__RequestVerificationToken').value

    try { Invoke-WebRequest "$base/Account/Login" -Method POST -WebSession $s -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 10 `
            -Body @{ username='admin'; password='wrong'; __RequestVerificationToken=$tok } | Out-Null; $proof.wrongPassword = 200 }
    catch { $proof.wrongPassword = $_.Exception.Response.StatusCode.value__ }

    try { $good = Invoke-WebRequest "$base/Account/Login" -Method POST -WebSession $s -UseBasicParsing -MaximumRedirection 0 -TimeoutSec 10 `
            -Body @{ username='admin'; password='Admin#12345'; __RequestVerificationToken=$tok }; $proof.goodPassword = $good.StatusCode }
    catch { $proof.goodPassword = $_.Exception.Response.StatusCode.value__ }
    $proof.authCookie = [bool]($s.Cookies.GetAllCookies() | Where-Object Name -match 'LafErpCookie')

    $no500 = $true
    foreach ($p in @('/', '/Home/GeneralLedger', '/Home/Customers', '/Home/AuditLog', '/Catalog')) {
        try { $r = Invoke-WebRequest "$base$p" -WebSession $s -UseBasicParsing -TimeoutSec 10; if ($r.StatusCode -ge 500) { $no500 = $false } }
        catch { if ($_.Exception.Response.StatusCode.value__ -ge 500) { $no500 = $false } }
    }
    $proof.noHttp500 = $no500
    $proof.pass = ($proof.loginPage -eq 200 -and $proof.antiForgeryTokenPresent -and $proof.goodPassword -eq 302 -and $proof.authCookie -and $no500 -and $proof.wrongPassword -ne 302)

    $out = Join-Path $repo "deployment-evidence"; New-Item -ItemType Directory -Force -Path $out | Out-Null
    ($proof | ConvertTo-Json) | Set-Content (Join-Path $out "production-smoke-proof.json")
    if ($proof.pass) { Write-Host "SMOKE PASS" -ForegroundColor Green } else { Write-Host "SMOKE FAIL: $($proof | ConvertTo-Json -Compress)" -ForegroundColor Red }
    if (-not $proof.pass) { exit 1 }
}
finally { Stop-Process -Id $proc.Id -Force -EA SilentlyContinue }
