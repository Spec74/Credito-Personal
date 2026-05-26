# Verificacion final de cierre migracion strangler (repo).
# Uso: cd modern -> .\deploy\scripts\finalize-migration.ps1
#      .\deploy\scripts\finalize-migration.ps1 -RunDockerChecks

param(
    [switch]$RunDockerChecks,
    [string]$BaseUrl = "http://localhost:9080"
)

$ErrorActionPreference = "Stop"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")
$webRoot = Join-Path $modernRoot "Credito.Modern.Web"
$distIndex = Join-Path $webRoot "dist\index.html"

Write-Host "=== Finalize migration (Credito strangler) ===" -ForegroundColor Cyan
Write-Host ""

Push-Location $modernRoot
try {
    Write-Host "== dotnet test" -ForegroundColor Cyan
    dotnet test --nologo -v q
    if ($LASTEXITCODE -ne 0) { throw "dotnet test fallo (exit $LASTEXITCODE)" }
    Write-Host "OK tests" -ForegroundColor Green

    Write-Host ""
    Write-Host "== verify-production-config (plantilla repo, sin ForwardedHeaders)" -ForegroundColor Cyan
    & (Join-Path $scriptsDir "verify-production-config.ps1")

    if ($RunDockerChecks) {
        if (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
            Write-Host "AVISO: Docker no instalado; omitiendo verify/smoke proxy." -ForegroundColor Yellow
        }
        else {
            Write-Host ""
            Write-Host "== verify-strangler-proxy" -ForegroundColor Cyan
            & (Join-Path $scriptsDir "verify-strangler-proxy.ps1")

            Write-Host ""
            Write-Host "== smoke-strangler-proxy" -ForegroundColor Cyan
            & (Join-Path $scriptsDir "smoke-strangler-proxy.ps1") -BaseUrl $BaseUrl

            Write-Host ""
            Write-Host "== cutover redirects + paridad (5C-7)" -ForegroundColor Cyan
            & (Join-Path $scriptsDir "test-local-cutover.ps1") -BaseUrl $BaseUrl -SkipBuild -SkipDockerStart -OnlyRedirectsAndParidad
        }
    }
    else {
        Write-Host ""
        Write-Host "Tip: build SPA + Docker:" -ForegroundColor Gray
        Write-Host "  .\deploy\scripts\build-spa.ps1" -ForegroundColor Gray
        Write-Host "  .\deploy\scripts\finalize-migration.ps1 -RunDockerChecks" -ForegroundColor Gray
    }
}
finally {
    Pop-Location
}

Write-Host ""
& (Join-Path $scriptsDir "migration-status.ps1")

Write-Host ""
Write-Host "========================================" -ForegroundColor Green
Write-Host " MIGRACION EN REPO: TERMINADA" -ForegroundColor Green
Write-Host " Deploy al subir: docs\migration\DEPLOY-AL-SUBIR.md" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
