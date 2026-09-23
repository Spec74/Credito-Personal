<#
.SYNOPSIS
  Ejecuta tests backend con cobertura (coverlet) y valida umbral de líneas.

.DESCRIPTION
  Genera Cobertura XML + JSON. Por defecto exige >= 40% líneas totales
  (baseline local ~46% con la suite actual).

.EXAMPLE
  .\deploy\scripts\test-coverage-backend.ps1
  .\deploy\scripts\test-coverage-backend.ps1 -Threshold 40
#>
[CmdletBinding()]
param(
    [ValidateRange(0, 100)]
    [int]$Threshold = 40,
    [string]$OutputDir = ""
)

$ErrorActionPreference = 'Stop'
$modernRoot = Resolve-Path (Join-Path $PSScriptRoot '..\..')
if (-not $OutputDir) {
    $OutputDir = Join-Path $modernRoot 'TestResults\coverage'
}

New-Item -ItemType Directory -Force -Path $OutputDir | Out-Null
# Ruta absoluta + separador final: coverlet escribe relativo al csproj si no.
$outDirAbs = (Resolve-Path $OutputDir).Path
if (-not $outDirAbs.EndsWith([IO.Path]::DirectorySeparatorChar)) {
    $outDirAbs += [IO.Path]::DirectorySeparatorChar
}
$outPrefix = Join-Path $outDirAbs 'backend'

Push-Location $modernRoot
try {
    Write-Host ">> dotnet test + coverlet (umbral lineas >= $Threshold%)" -ForegroundColor Cyan
    Write-Host "   CoverletOutput=$outPrefix" -ForegroundColor Gray
    & dotnet test Credito.Modern.Tests\Credito.Modern.Tests.csproj `
        --nologo `
        -c Release `
        /p:CollectCoverage=true `
        /p:CoverletOutputFormat=cobertura%2Cjson `
        "/p:CoverletOutput=$outPrefix" `
        /p:Exclude="[xunit*]*" `
        /p:ExcludeByAttribute="Obsolete%2CGeneratedCodeAttribute%2CCompilerGeneratedAttribute" `
        /p:Threshold=$Threshold `
        /p:ThresholdType=line `
        /p:ThresholdStat=total

    if ($LASTEXITCODE -ne 0) {
        throw "Cobertura backend por debajo del umbral ($Threshold% lineas) o tests fallaron."
    }

    $json = Get-ChildItem $outDirAbs -Filter 'backend*.json' -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1
    if ($json) {
        Write-Host "OK  coverlet JSON: $($json.FullName) (umbral $Threshold% lineas)" -ForegroundColor Green
    }
    else {
        Write-Host "OK  coverlet completado (ver $outDirAbs) — umbral $Threshold% lineas" -ForegroundColor Green
    }
}
finally {
    Pop-Location
}
