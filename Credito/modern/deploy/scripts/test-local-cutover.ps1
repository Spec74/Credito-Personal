# Prueba local Corte A + B (Docker 9080 + SPA /app/)
# Uso: cd modern -> .\deploy\scripts\test-local-cutover.ps1
# Sin Docker: valida build + config; luego inicia Docker Desktop y vuelve a ejecutar.

param(
    [string]$BaseUrl = "http://localhost:9080",
    [switch]$SkipDockerStart,
    [switch]$SkipBuild,
    [switch]$PartialOnly,
    [switch]$OnlyRedirectsAndParidad
)

$ErrorActionPreference = "Stop"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")
$webRoot = Join-Path $modernRoot "Credito.Modern.Web"
$distIndex = Join-Path $webRoot "dist\index.html"

function Test-DockerRunning {
    if (-not (Get-Command docker -ErrorAction SilentlyContinue)) { return $false }
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        & docker info *> $null
        return $LASTEXITCODE -eq 0
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Test-Redirect302 {
    param([string]$Url, [string]$Label, [string]$ExpectedLocationFragment)
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $params = @{
            Uri             = $Url
            UseBasicParsing = $true
            MaximumRedirection = 0
            TimeoutSec      = 30
        }
        if ($PSVersionTable.PSVersion.Major -ge 7) {
            $params.SkipHttpErrorCheck = $true
        }
        $r = Invoke-WebRequest @params
        if ($r.StatusCode -ne 302) {
            throw "$Label HTTP $($r.StatusCode) (esperado 302)"
        }
        $loc = $null
        if ($r.Headers) {
            if ($r.Headers['Location']) { $loc = $r.Headers['Location'] }
            elseif ($r.Headers['location']) { $loc = $r.Headers['location'] }
        }
        if (-not $loc -and $r -is [System.Net.HttpWebResponse]) {
            $loc = $r.Headers['Location']
        }
        if (-not $loc) {
            throw "$Label sin cabecera Location (HTTP $($r.StatusCode))"
        }
        if ($loc -notlike "*$ExpectedLocationFragment*") {
            throw "$Label Location='$loc' no contiene '$ExpectedLocationFragment'"
        }
        Write-Host "OK  $Label -> 302 $loc" -ForegroundColor Green
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Test-Redirect302 {
    param([string]$Url, [string]$Label, [string]$ExpectedLocationFragment)
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $params = @{
            Uri             = $Url
            UseBasicParsing = $true
            MaximumRedirection = 0
            TimeoutSec      = 30
        }
        if ($PSVersionTable.PSVersion.Major -ge 7) {
            $params.SkipHttpErrorCheck = $true
        }
        $r = Invoke-WebRequest @params
        if ($r.StatusCode -ne 302) {
            throw "$Label HTTP $($r.StatusCode) (esperado 302)"
        }
        $loc = $null
        if ($r.Headers) {
            if ($r.Headers['Location']) { $loc = $r.Headers['Location'] }
            elseif ($r.Headers['location']) { $loc = $r.Headers['location'] }
        }
        if (-not $loc -and $r -is [System.Net.HttpWebResponse]) {
            $loc = $r.Headers['Location']
        }
        if (-not $loc) {
            throw "$Label sin cabecera Location (HTTP $($r.StatusCode))"
        }
        if ($loc -notlike "*$ExpectedLocationFragment*") {
            throw "$Label Location='$loc' no contiene '$ExpectedLocationFragment'"
        }
        Write-Host "OK  $Label -> 302 $loc" -ForegroundColor Green
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

function Test-HttpOk {
    param([string]$Url, [string]$Label, [int[]]$Allowed = @(200))
    $prev = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $params = @{
            Uri             = $Url
            UseBasicParsing = $true
            TimeoutSec      = 30
        }
        if ($PSVersionTable.PSVersion.Major -ge 7) {
            $params.SkipHttpErrorCheck = $true
        }
        $r = Invoke-WebRequest @params
        if ($r.StatusCode -notin $Allowed) {
            throw "$Label HTTP $($r.StatusCode) (esperado: $($Allowed -join ','))"
        }
        Write-Host "OK  $Label -> $($r.StatusCode)" -ForegroundColor Green
        return $r
    }
    catch {
        if ($_.Exception.Response -and $_.Exception.Response.StatusCode) {
            $code = [int]$_.Exception.Response.StatusCode
            if ($code -in $Allowed) {
                Write-Host "OK  $Label -> $code" -ForegroundColor Green
                return $null
            }
        }
        throw "$Label fallo ($Url): $($_.Exception.Message)"
    }
    finally {
        $ErrorActionPreference = $prev
    }
}

Write-Host ""
Write-Host "=== Prueba corte local (A: API/proxy + B: SPA /app/) ===" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host ""

$dockerOk = Test-DockerRunning
if (-not $dockerOk) {
    Write-Host "AVISO: Docker no esta en marcha. Modo parcial (build + config)." -ForegroundColor Yellow
    Write-Host "       Inicia Docker Desktop y vuelve a ejecutar este script." -ForegroundColor Yellow
    Write-Host ""
}

Push-Location $modernRoot
try {
    if (-not $SkipBuild) {
        Write-Host ">> npm run build (VITE_BASE_URL=/app/)" -ForegroundColor Yellow
        Push-Location $webRoot
        try {
            npm run build
            if ($LASTEXITCODE -ne 0) { throw "npm run build exit $LASTEXITCODE" }
        }
        finally {
            Pop-Location
        }
    }
    if (-not (Test-Path $distIndex)) {
        throw "dist/index.html no existe; ejecuta sin -SkipBuild"
    }

    $html = Get-Content -Raw $distIndex
    if ($html -notmatch '/app/assets/') {
        throw "dist/index.html no referencia /app/assets/ - revisar VITE_BASE_URL"
    }
    Write-Host "OK  dist con base /app/" -ForegroundColor Green

    if ($OnlyRedirectsAndParidad) {
        if (-not $dockerOk) {
            throw "Docker no esta en marcha; no se pueden probar redirects ni paridad."
        }
        Write-Host ""
        Write-Host ">> Corte C - redirects 5C-7 (nginx)" -ForegroundColor Cyan
        Test-Redirect302 -Url "$BaseUrl/" -Label "GET /" -ExpectedLocationFragment "/app/login"
        Test-Redirect302 -Url "$BaseUrl/Usuario" -Label "GET /Usuario" -ExpectedLocationFragment "/app/admin/usuarios"
        Test-Redirect302 -Url "$BaseUrl/CajaDiario" -Label "GET /CajaDiario" -ExpectedLocationFragment "/app/caja/diario"

        $ui = Invoke-RestMethod -Uri "$BaseUrl/api/v1/hosting/ui-config" -TimeoutSec 30
        if (-not $ui.useSpaForModule) {
            throw "ui-config.useSpaForModule debe ser true en Staging (Docker)"
        }
        Write-Host "OK  ui-config cutover activo (useSpaForModule=true)" -ForegroundColor Green

        Write-Host ""
        Write-Host ">> test-paridad-informes-fase4.ps1" -ForegroundColor Cyan
        & (Join-Path $scriptsDir "test-paridad-informes-fase4.ps1") -BaseUrl $BaseUrl -OficinaId 1 -UsuarioId 1

        Write-Host ""
        Write-Host "=== Redirects + paridad OK ===" -ForegroundColor Green
        return
    }

    Write-Host ""
    Write-Host ">> verify-production-config" -ForegroundColor Cyan
    & (Join-Path $scriptsDir "verify-production-config.ps1")

    Write-Host ""
    Write-Host ">> migration-status" -ForegroundColor Cyan
    & (Join-Path $scriptsDir "migration-status.ps1")

    if ($PartialOnly -or -not $dockerOk) {
        Write-Host ""
        Write-Host "=== Corte parcial OK (sin proxy Docker) ===" -ForegroundColor Green
        Write-Host "Siguiente: Docker Desktop -> .\deploy\scripts\test-local-cutover.ps1" -ForegroundColor White
        return
    }

    if (-not $SkipDockerStart) {
        Write-Host ""
        Write-Host ">> start-strangler.ps1 -Build" -ForegroundColor Yellow
        & (Join-Path $scriptsDir "start-strangler.ps1") -Build
        Start-Sleep -Seconds 10
    }

    Write-Host ""
    Write-Host ">> Corte A - API detras del proxy" -ForegroundColor Cyan
    Test-HttpOk -Url "$BaseUrl/health" -Label "GET /health"
    Test-HttpOk -Url "$BaseUrl/api/v1/reportes/politica-exportacion" -Label "GET politica sin JWT" -Allowed @(401)

    Write-Host ""
    Write-Host ">> Corte B - SPA en /app/" -ForegroundColor Cyan
    $spa = Test-HttpOk -Url "$BaseUrl/app/" -Label "GET /app/"
    if ($spa.Content -notmatch "root") {
        Write-Host "AVISO: /app/ no parece HTML de la SPA" -ForegroundColor Yellow
    }

    Write-Host ""
    Write-Host ">> smoke-strangler-proxy.ps1" -ForegroundColor Cyan
    & (Join-Path $scriptsDir "smoke-strangler-proxy.ps1") -BaseUrl $BaseUrl

    Write-Host ""
    Write-Host ">> Corte C - redirects 5C-7 (nginx)" -ForegroundColor Cyan
    Test-Redirect302 -Url "$BaseUrl/" -Label "GET /" -ExpectedLocationFragment "/app/login"
    Test-Redirect302 -Url "$BaseUrl/Usuario" -Label "GET /Usuario" -ExpectedLocationFragment "/app/admin/usuarios"
    Test-Redirect302 -Url "$BaseUrl/CajaDiario" -Label "GET /CajaDiario" -ExpectedLocationFragment "/app/caja/diario"

    $ui = Invoke-RestMethod -Uri "$BaseUrl/api/v1/hosting/ui-config" -TimeoutSec 30
    if (-not $ui.useSpaForModule) {
        throw "ui-config.useSpaForModule debe ser true en Staging (Docker)"
    }
    Write-Host "OK  ui-config cutover activo (useSpaForModule=true)" -ForegroundColor Green

    Write-Host ""
    Write-Host ">> test-paridad-informes-fase4.ps1" -ForegroundColor Cyan
    & (Join-Path $scriptsDir "test-paridad-informes-fase4.ps1") -BaseUrl $BaseUrl -OficinaId 1 -UsuarioId 1

    Write-Host ""
    Write-Host "=== Corte local OK ===" -ForegroundColor Green
    Write-Host "SPA:  $BaseUrl/app/" -ForegroundColor White
    Write-Host "MVC:  $BaseUrl/  (MVC en puerto 52099)" -ForegroundColor Gray
}
finally {
    Pop-Location
}

Write-Host ""
