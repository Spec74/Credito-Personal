# Genera carpeta de evidencias para revision docente (strangler local).
# Uso (desde modern\):
#   .\deploy\scripts\package-docente-demo.ps1
#   .\deploy\scripts\package-docente-demo.ps1 -OpenFolder
#
# Requisitos recomendados antes de ejecutar:
#   - .\deploy\scripts\finalize-migration.ps1 -RunDockerChecks
#   - Docker: .\deploy\scripts\start-strangler.ps1 -Build
#   - Visual Studio F5 en proyecto Web (IIS Express 52099)

param(
    [switch]$OpenFolder,
    [switch]$SkipDockerChecks,
    [ValidateSet("Auto", "DevServer", "IISExpress")]
    [string]$MvcProfile = "IISExpress"
)

$ErrorActionPreference = "Continue"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")
$repoRoot = Resolve-Path (Join-Path $modernRoot "..")
$stamp = Get-Date -Format "yyyy-MM-dd_HHmm"
$outDir = Join-Path $repoRoot "docs\migration\evidence\docente-$stamp"
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

function Write-LogFile {
    param([string]$Name, [scriptblock]$Block)
    $path = Join-Path $outDir $Name
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("=== $Name === $(Get-Date -Format 'o') ===")
    try {
        & $Block 2>&1 | ForEach-Object {
            if ($_ -is [System.Management.Automation.ErrorRecord]) { $lines.Add($_.Exception.Message) }
            else { $lines.Add($_.ToString()) }
        }
    }
    catch {
        $lines.Add("ERROR: $($_.Exception.Message)")
    }
    $lines | Set-Content -LiteralPath $path -Encoding UTF8
    return $path
}

Write-Host "Generando evidencias en:" -ForegroundColor Cyan
Write-Host "  $outDir" -ForegroundColor Gray

# Alinear nginx al puerto MVC del perfil elegido
try {
    $mvcPort = & (Join-Path $scriptsDir "resolve-mvc-dev-port.ps1") -Profile $MvcProfile -AsInt
    & (Join-Path $scriptsDir "set-mvc-upstream-port.ps1") -Port $mvcPort 2>&1 | Out-Null
}
catch {
    Write-Host "AVISO: no se pudo alinear puerto MVC: $($_.Exception.Message)" -ForegroundColor Yellow
}

Push-Location $modernRoot
try {
    Write-LogFile "01-dotnet-test.txt" { dotnet test --verbosity minimal --nologo }
    Write-LogFile "02-verify-mvc-auth.txt" { & (Join-Path $scriptsDir "verify-mvc-auth-coverage.ps1") }
    Write-LogFile "03-test-mvc-sql.txt" { & (Join-Path $scriptsDir "test-mvc-sql-connection.ps1") }
    Write-LogFile "04-test-mvc-ef.txt" { & (Join-Path $scriptsDir "test-mvc-ef-oficina.ps1") }
    Write-LogFile "05-diagnose-strangler-mvc.txt" { & (Join-Path $scriptsDir "diagnose-strangler-mvc.ps1") }
    if (-not $SkipDockerChecks) {
        Write-LogFile "06-verify-strangler-proxy.txt" { & (Join-Path $scriptsDir "verify-strangler-proxy.ps1") }
        Write-LogFile "07-smoke-strangler-proxy.txt" { & (Join-Path $scriptsDir "smoke-strangler-proxy.ps1") }
        Write-LogFile "08-test-local-cutover.txt" {
            & (Join-Path $scriptsDir "test-local-cutover.ps1") -SkipBuild -SkipDockerStart
        }
    }
    else {
        "Omitido (-SkipDockerChecks)" | Set-Content (Join-Path $outDir "06-verify-strangler-proxy.txt")
        "Omitido (-SkipDockerChecks)" | Set-Content (Join-Path $outDir "07-smoke-strangler-proxy.txt")
        "Omitido (-SkipDockerChecks)" | Set-Content (Join-Path $outDir "08-test-local-cutover.txt")
    }
}
finally {
    Pop-Location
}

$readme = @"
# Evidencias demo strangler — $stamp

## Como reproducir (revisor)

1. SQL Server `CREDITO` en `localhost,14330` (misma cadena que `Web\Web.config`).
2. `cd modern` → `copy deploy\.env.example deploy\.env` (password SQL + JWT_SIGNING_KEY).
3. `.\deploy\scripts\start-strangler.ps1 -Build`
4. Visual Studio **F5** en proyecto **Web** (IIS Express puerto **52099**).
5. `.\deploy\scripts\package-docente-demo.ps1` (este paquete).

## URLs de demo

| URL | Que muestra |
|-----|-------------|
| http://localhost:9080/ | Redirect 302 → `/app/login` (cutover 5C-7.5 local) |
| http://localhost:9080/app/ | SPA React |
| http://localhost:9080/health | API detras del proxy |
| http://localhost:52099/ | MVC directo (IIS Express) |
| http://localhost:5080/health | API directa (depuracion) |

## Documentacion principal

- docs/migration/ENTREGA-DOCENTE.md
- docs/migration/STRANGLER-PROGRESS-REPORT.md
- docs/migration/STRANGLER-MIGRATION.md
- docs/migration/PHASE-3B-PROXY-E2E.md
- docs/migration/PHASE-5C-7-CUTOVER.md
- docs/migration/PARIDAD-INFORMES-FASE4.md

## Archivos en esta carpeta

- 01-dotnet-test.txt — tests modernos (meta: 682 OK)
- 02-verify-mvc-auth.txt — todos los controladores con [Autenticado]
- 03-test-mvc-sql.txt — conexion SQL desde scripts
- 04-test-mvc-ef.txt — consulta EF login (oficinas)
- 05-diagnose-strangler-mvc.txt — puertos, HTTP, proxy
- 06-verify-strangler-proxy.txt — Docker + proxy
- 07-smoke-strangler-proxy.txt — JWT, catalogo 43/40, MVC 302
- 08-test-local-cutover.txt — cutover 5C-7.5 + paridad informes Fase 4

## Alcance declarado (strangler por fases)

- **Fases 0–3:** API, escrituras P0, export datos — cerrado ([MIGRATION-CLOSURE.md](../MIGRATION-CLOSURE.md)).
- **Fase 4 datos:** 43 informes JSON+CSV+PDF; paridad BL verificada ([PARIDAD-INFORMES-FASE4.md](../PARIDAD-INFORMES-FASE4.md)).
- **Fase 5C dev:** SPA + cutover local (redirects, ui-config) — cerrado en repo ([PHASE-5C-CLOSURE.md](../PHASE-5C-CLOSURE.md)).
- **Pendiente ops:** preprod/prod ([PHASE-5-OPERATIONS-CUTOVER.md](../PHASE-5-OPERATIONS-CUTOVER.md), [PHASE-5C-7-CUTOVER.md](../PHASE-5C-7-CUTOVER.md)).
- **Fase 4 layout RDLC (6B):** opcional si negocio exige ticket pixel-perfect.
"@
$readme | Set-Content -LiteralPath (Join-Path $outDir "README.md") -Encoding UTF8

# Copiar guias de entrega / paridad junto a la evidencia
foreach ($rel in @(
        "docs\migration\ENTREGA-DOCENTE.md",
        "docs\migration\PARIDAD-INFORMES-FASE4.md",
        "docs\migration\PHASE-5C-CLOSURE.md"
    )) {
    $src = Join-Path $repoRoot $rel
    if (Test-Path $src) {
        $name = Split-Path $src -Leaf
        Copy-Item -LiteralPath $src -Destination (Join-Path $outDir $name) -Force
    }
}
# Ultimo log de paridad suelto (si existe)
$paridadGlob = Join-Path $repoRoot "docs\migration\evidence\paridad-fase4-*.txt"
$latestParidad = Get-ChildItem -Path $paridadGlob -ErrorAction SilentlyContinue | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($latestParidad) {
    Copy-Item -LiteralPath $latestParidad.FullName -Destination (Join-Path $outDir "09-paridad-informes-fase4.txt") -Force
}

Write-Host ""
Write-Host "Listo. Carpeta de evidencias:" -ForegroundColor Green
Write-Host "  $outDir" -ForegroundColor White
Write-Host "Comparte README.md + archivos 01-08 (+ 09 si hay paridad) con el docente." -ForegroundColor Gray

if ($OpenFolder) {
    Start-Process explorer.exe $outDir
}
