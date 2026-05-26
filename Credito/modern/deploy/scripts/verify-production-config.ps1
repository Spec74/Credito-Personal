# Valida plantilla Production y lista variables para corte (sin tocar secretos).
# Uso: desde Credito\modern -> .\deploy\scripts\verify-production-config.ps1

param(
    [string]$ProductionSettingsPath = "",
    [switch]$RequireForwardedHeaders
)

$ErrorActionPreference = "Stop"

$modernRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
if ([string]::IsNullOrWhiteSpace($ProductionSettingsPath)) {
    $ProductionSettingsPath = Join-Path $modernRoot "Credito.Modern.Api\appsettings.Production.json"
}

if (-not (Test-Path $ProductionSettingsPath)) {
    throw "No existe $ProductionSettingsPath"
}

Write-Host "== appsettings.Production.json" -ForegroundColor Cyan
$json = Get-Content -Raw -Path $ProductionSettingsPath | ConvertFrom-Json

$errors = [System.Collections.Generic.List[string]]::new()

if ($json.Hosting.AllowDevToken -ne $false) {
    $errors.Add("Hosting:AllowDevToken debe ser false en produccion.")
}
if ($json.Menu.PermiteParametrosQuery -ne $false) {
    $errors.Add("Menu:PermiteParametrosQuery debe ser false en produccion.")
}
if ($json.Auth.RequerirClienteAcceso -ne $true) {
    $errors.Add("Auth:RequerirClienteAcceso debe ser true salvo decision explicita de negocio.")
}
if ($json.RateLimiting.Disabled -eq $true) {
    $errors.Add("RateLimiting:Disabled no debe ser true en produccion.")
}

$fwdEnabled = $json.Hosting.ForwardedHeaders.Enabled
$knownCount = @($json.Hosting.ForwardedHeaders.KnownProxies).Count
if ($RequireForwardedHeaders) {
    if (-not $fwdEnabled) {
        $errors.Add("Hosting:ForwardedHeaders:Enabled debe ser true detras de IIS/nginx.")
    }
    if ($knownCount -lt 1) {
        $errors.Add("Hosting:ForwardedHeaders:KnownProxies debe incluir la IP del proxy inmediato.")
    }
}
elseif ($fwdEnabled -and $knownCount -lt 1) {
    $errors.Add("ForwardedHeaders.Enabled=true requiere al menos una IP en KnownProxies.")
}

$corsCount = @($json.BrowserCors.AllowedOrigins).Count
if ($corsCount -lt 1) {
    Write-Host "AVISO: BrowserCors:AllowedOrigins vacio - configurar antes del SPA en prod." -ForegroundColor Yellow
}
else {
    Write-Host "  OK CORS: $corsCount origen(es)" -ForegroundColor Gray
}

Write-Host "  AllowDevToken: $($json.Hosting.AllowDevToken)" -ForegroundColor Gray
Write-Host "  ForwardedHeaders.Enabled: $fwdEnabled (KnownProxies: $knownCount)" -ForegroundColor Gray
Write-Host "  RequerirClienteAcceso: $($json.Auth.RequerirClienteAcceso)" -ForegroundColor Gray

if ($errors.Count -gt 0) {
    foreach ($e in $errors) {
        Write-Host "ERROR: $e" -ForegroundColor Red
    }
    if ($RequireForwardedHeaders) {
        Write-Host "`nNOTA: -RequireForwardedHeaders es para el SERVIDOR de preprod/prod." -ForegroundColor Yellow
        Write-Host "  En local con Docker staging NO uses ese flag; usa:" -ForegroundColor Yellow
        Write-Host "    .\deploy\scripts\run-local-strangler-e2e.ps1" -ForegroundColor White
        Write-Host "  En el servidor: variables Hosting__ForwardedHeaders__* (ver deploy\.env.production.example)." -ForegroundColor Yellow
    }
    throw "Configuracion Production no lista para corte."
}

Write-Host "OK contrato Production (plantilla repo)." -ForegroundColor Green

Write-Host "`n== Variables de entorno en el host API (ejemplo)" -ForegroundColor Cyan
@(
    "ASPNETCORE_ENVIRONMENT=Production",
    "CreditoDatabase__ConnectionString=[cadena SQL prod]",
    "Jwt__SigningKey=[min 32 bytes UTF-8]",
    "Jwt__Issuer / Jwt__Audience / Jwt__RefreshAudience",
    "BrowserCors__AllowedOrigins__0=https://app.tu-dominio",
    "Hosting__ForwardedHeaders__Enabled=true",
    "Hosting__ForwardedHeaders__KnownProxies__0=[IP proxy IIS/nginx]"
) | ForEach-Object { Write-Host "  $_" -ForegroundColor Gray }

Write-Host "`n== Proxy fachada (despues de API)" -ForegroundColor Cyan
Write-Host "  Plantilla IIS: deploy\iis-arr-web.config.example" -ForegroundColor Gray
Write-Host "  Plantilla nginx: deploy\nginx\production.conf.example" -ForegroundColor Gray
Write-Host "  Staging local OK: verify-strangler-proxy.ps1 + smoke-strangler-proxy.ps1" -ForegroundColor Gray

Write-Host "`nVerificacion Production config OK." -ForegroundColor Green
