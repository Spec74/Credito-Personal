# E2E local completo: puerto MVC desde Web.csproj + proxy + verify + smoke.
# Uso (desde modern\):
#   .\deploy\scripts\run-local-strangler-e2e.ps1
#   .\deploy\scripts\run-local-strangler-e2e.ps1 -MvcPort 52099   # IIS Express
#   .\deploy\scripts\run-local-strangler-e2e.ps1 -Build -SkipMvcProbe

param(
    [int]$MvcPort = 0,
    [ValidateSet("Auto", "DevServer", "IISExpress")]
    [string]$Profile = "Auto",
    [switch]$Build,
    [switch]$SkipMvcProbe,
    [switch]$SkipSmoke,
    [switch]$DiagnoseOnly
)

$ErrorActionPreference = "Stop"
$scriptsDir = $PSScriptRoot

if ($DiagnoseOnly) {
    & (Join-Path $scriptsDir "diagnose-strangler-mvc.ps1")
    return
}

if ($MvcPort -lt 1) {
    $MvcPort = & (Join-Path $scriptsDir "resolve-mvc-dev-port.ps1") -Profile $Profile -AsInt
}

Write-Host "== E2E strangler local (MVC puerto $MvcPort, perfil $Profile)" -ForegroundColor Cyan
Write-Host "  F5 en Web. Si usas IIS Express: -Profile IISExpress (puerto 52099 en este repo)." -ForegroundColor Gray
Write-Host "  Si 502 o MVC colgado: .\deploy\scripts\diagnose-strangler-mvc.ps1" -ForegroundColor Gray

& (Join-Path $scriptsDir "set-mvc-upstream-port.ps1") -Port $MvcPort

if ($Build) {
    Write-Host "`n== Rebuild stack Docker" -ForegroundColor Cyan
    $modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")
    Push-Location $modernRoot
    try {
        & docker compose -f deploy/docker-compose.strangler.yml --env-file deploy/.env up -d --build
    }
    finally {
        Pop-Location
    }
}

& (Join-Path $scriptsDir "verify-strangler-proxy.ps1")

if (-not $SkipSmoke) {
    & (Join-Path $scriptsDir "smoke-strangler-proxy.ps1")
}

if (-not $SkipMvcProbe) {
    Write-Host "`n== Sondeo MVC http://localhost:9080/" -ForegroundColor Cyan
    try {
        $mvc = Invoke-WebRequest -Uri "http://localhost:9080/" -UseBasicParsing -TimeoutSec 8
        Write-Host "  OK MVC HTTP $($mvc.StatusCode)" -ForegroundColor Green
    }
    catch {
        $code = 0
        if ($_.Exception.Response) { $code = [int]$_.Exception.Response.StatusCode }
        if ($code -eq 502) {
            Write-Host "  AVISO: 502 - inicia Web en Visual Studio (puerto $MvcPort) y vuelve a ejecutar." -ForegroundColor Yellow
        }
        else {
            Write-Host "  MVC: $($_.Exception.Message)" -ForegroundColor Yellow
        }
    }
}

Write-Host "`nE2E local strangler finalizado." -ForegroundColor Green
Write-Host "  Produccion real: verify-production-config.ps1 en el SERVIDOR (no -RequireForwardedHeaders en local)." -ForegroundColor Gray
