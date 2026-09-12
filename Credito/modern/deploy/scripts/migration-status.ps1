# Resumen de estado — migración strangler

param([switch]$RunChecks)

$ErrorActionPreference = "Continue"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")

Write-Host "=== Credito strangler ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "[CODIGO]" -ForegroundColor Green
@(
    "Candidato a go-live. Spec as-built: docs\ssd\ (SSD-00 … SSD-11).",
    "Veredicto: docs\migration\MIGRATION-CLOSURE.md",
    "Paridad de negocio: usp_* + docs\migration\BITACORA-DESVIACIONES.md",
    "SPA /app/ + API /api/v1. MVC permanece hasta smoke + OK de negocio."
) | ForEach-Object { Write-Host "  - $_" }

Write-Host ""
Write-Host "[AL SUBIR INFRA]" -ForegroundColor Yellow
@(
    "docs\migration\PHASE-5-OPERATIONS-CUTOVER.md",
    "docs\migration\DEPLOY-AL-SUBIR.md",
    "deploy\.env.preproduction.example / .env.production.example",
    "verify-preprod-cutover.ps1 -BaseUrl <URL real del entorno>"
) | ForEach-Object { Write-Host "  - $_" }

Write-Host ""
Write-Host "Smoke local: .\deploy\scripts\smoke-local-api.ps1" -ForegroundColor Gray
Write-Host "Contrato:    .\deploy\scripts\verify-production-config.ps1" -ForegroundColor Gray
Write-Host "Checklist:   .\deploy\scripts\preprod-cutover-checklist.ps1 -SkipInteractive" -ForegroundColor Gray

if ($RunChecks) {
    Write-Host ""
    Write-Host ">> verify-production-config.ps1" -ForegroundColor Cyan
    & (Join-Path $scriptsDir "verify-production-config.ps1")
}
