# Verificacion corte preprod/prod (5C-7.5 + Fase 3b) — uso operativo del equipo.
# Uso:
#   .\deploy\scripts\verify-preprod-cutover.ps1 -BaseUrl "https://credito-preprod.ejemplo"
#   .\deploy\scripts\verify-preprod-cutover.ps1 -BaseUrl "http://localhost:9080" -Entorno Local
#
# Requiere: API publicada, proxy con /api/v1 y (opcional) SPA en /app/

param(
    [Parameter(Mandatory = $true)]
    [string]$BaseUrl,
    [ValidateSet("PreProduction", "Production", "Local")]
    [string]$Entorno = "PreProduction",
    [ValidateSet(1, 2, 3)]
    [int]$PilotoFase = 1,
    [switch]$SkipParidad,
    [switch]$RequireForwardedHeaders
)

$ErrorActionPreference = "Stop"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")

if ($BaseUrl -match 'tu-dominio|ejemplo\.|placeholder') {
    throw "BaseUrl parece plantilla. Usa la URL real del proxy preprod."
}

Write-Host ""
Write-Host "=== Verificacion corte $Entorno (piloto fase $PilotoFase) ===" -ForegroundColor Cyan
Write-Host "BaseUrl: $BaseUrl"
Write-Host ""

$settingsName = switch ($Entorno) {
    "Local" { "appsettings.Staging.json" }
    "PreProduction" { "appsettings.PreProduction.json" }
    default { "appsettings.Production.json" }
}
$settingsPath = Join-Path $modernRoot "Credito.Modern.Api\$settingsName"

if ($Entorno -eq "Local") {
    Write-Host ">> Config: omitida en Local (usa appsettings.Staging + Docker)" -ForegroundColor Gray
}
else {
    Write-Host ">> Config plantilla: $settingsName" -ForegroundColor Yellow
    $vfArgs = @{ ProductionSettingsPath = $settingsPath }
    if ($RequireForwardedHeaders) { $vfArgs.RequireForwardedHeaders = $true }
    & (Join-Path $scriptsDir "verify-production-config.ps1") @vfArgs
}

$expectSpaModule = $PilotoFase -ge 2
$expectLoginSpa = $PilotoFase -ge 3
if ($Entorno -eq "Local") {
    $expectSpaModule = $true
    $expectLoginSpa = $true
}

Write-Host ""
Write-Host ">> hosting/ui-config" -ForegroundColor Yellow
$ui = Invoke-RestMethod -Uri ($BaseUrl.TrimEnd("/") + "/api/v1/hosting/ui-config") -TimeoutSec 45
if ($ui.useSpaForModule -ne $expectSpaModule) {
    throw "ui-config.useSpaForModule=$($ui.useSpaForModule) (esperado $expectSpaModule para piloto fase $PilotoFase)"
}
if ($ui.defaultLoginToSpa -ne $expectLoginSpa) {
    throw "ui-config.defaultLoginToSpa=$($ui.defaultLoginToSpa) (esperado $expectLoginSpa)"
}
Write-Host "OK  ui-config alineado (useSpaForModule=$($ui.useSpaForModule), defaultLoginToSpa=$($ui.defaultLoginToSpa))" -ForegroundColor Green

Write-Host ""
Write-Host ">> smoke-strangler-proxy.ps1" -ForegroundColor Yellow
& (Join-Path $scriptsDir "smoke-strangler-proxy.ps1") -BaseUrl $BaseUrl

if (-not $SkipParidad) {
    Write-Host ""
    Write-Host ">> test-paridad-informes-fase4.ps1" -ForegroundColor Yellow
    & (Join-Path $scriptsDir "test-paridad-informes-fase4.ps1") -BaseUrl $BaseUrl -OficinaId 1 -UsuarioId 1
}

if ($PilotoFase -ge 2) {
    Write-Host ""
    Write-Host ">> Redirects nginx (muestra)" -ForegroundColor Yellow
    $samples = @(
        @{ Path = "/Usuario"; Fragment = "/app/admin/usuarios" },
        @{ Path = "/CajaDiario"; Fragment = "/app/caja/diario" }
    )
    foreach ($s in $samples) {
        try {
            $r = Invoke-WebRequest -Uri ($BaseUrl.TrimEnd("/") + $s.Path) -MaximumRedirection 0 -UseBasicParsing -TimeoutSec 20 -ErrorAction SilentlyContinue
        }
        catch {
            if ($_.Exception.Response) {
                $code = [int]$_.Exception.Response.StatusCode
                if ($code -eq 302) {
                    $loc = $_.Exception.Response.Headers["Location"]
                    if ($loc -like "*$($s.Fragment)*") {
                        Write-Host "OK  $($s.Path) -> 302 $loc" -ForegroundColor Green
                        continue
                    }
                }
            }
            Write-Host "AVISO: redirect $($s.Path) no verificado (include spa-legacy-redirects.conf?)" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Corte $Entorno verificado ===" -ForegroundColor Green
Write-Host "Siguiente: subir piloto (fase $($PilotoFase + 1)) segun PHASE-5C-7-CUTOVER.md" -ForegroundColor Gray
Write-Host ""
