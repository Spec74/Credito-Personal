# Resumen de estado migracion strangler (repo vs deploy).
param([switch]$RunChecks)

$ErrorActionPreference = "Continue"
$scriptsDir = $PSScriptRoot
$repoRoot = Resolve-Path (Join-Path $scriptsDir "..\..\..")

Write-Host "=== Credito strangler ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "[REPO - MIGRACION TERMINADA]" -ForegroundColor Green
$done = @(
    "Veredicto: docs\migration\MIGRACION-COMPLETA.md",
    "Fases 0-3, 3b, 4 (datos), 5, 5C cerradas en codigo",
    "690 tests; 43 informes JSON CSV PDF; solo-mvc datos=0; SPA 60 rutas",
    "SPA /app/ + API /api/v1 (validar: finalize-migration.ps1 -RunDockerChecks)",
    "Paridad informes: PARIDAD-INFORMES-FASE4.md",
    "Entrega / checklist ops: docs\migration\ENTREGA-FINAL.md",
    "Entrega / checklist ops: docs\migration\ENTREGA-FINAL.md"
)
foreach ($line in $done) { Write-Host "  - $line" }

Write-Host ""
Write-Host "[AL SUBIR INFRA - un solo checklist]" -ForegroundColor Yellow
$deploy = @(
    "docs\migration\DEPLOY-AL-SUBIR.md",
    "deploy\.env.preproduction.example / .env.production.example",
    "verify-preprod-cutover.ps1 -BaseUrl con URL real del entorno"
)
foreach ($line in $deploy) { Write-Host "  - $line" }

Write-Host ""
Write-Host "Build SPA:  .\deploy\scripts\build-spa.ps1  (o: cd Credito.Modern.Web; npm run build)" -ForegroundColor Gray
Write-Host "Cierre:     .\deploy\scripts\finalize-migration.ps1 -RunDockerChecks" -ForegroundColor Gray

if ($RunChecks) {
    Write-Host ""
    Write-Host ">> dotnet test (resumen)" -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot "modern")
    try {
        dotnet test --verbosity minimal --nologo 2>&1 | Select-Object -Last 3
    }
    finally {
        Pop-Location
    }
}
