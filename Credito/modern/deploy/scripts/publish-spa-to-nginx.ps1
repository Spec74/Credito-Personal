# Copia dist/ de la SPA al volumen que monta nginx (cutover /app/).
# Uso desde Credito\modern:
#   .\deploy\scripts\build-spa.ps1
#   .\deploy\scripts\publish-spa-to-nginx.ps1
# Con Docker strangler: el compose ya monta ../Credito.Modern.Web/dist — solo hace falta build-spa.

param(
    [string]$TargetDir = "",
    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")
$webRoot = Join-Path $modernRoot "Credito.Modern.Web"
$dist = Join-Path $webRoot "dist"

if (-not (Test-Path (Join-Path $dist "index.html"))) {
    throw "No existe dist/index.html. Ejecuta primero: .\deploy\scripts\build-spa.ps1"
}

if (-not $TargetDir) {
    $TargetDir = Join-Path $modernRoot "deploy\nginx-www\credito-modern-web"
}

Write-Host ">> Origen:  $dist" -ForegroundColor Cyan
Write-Host ">> Destino: $TargetDir" -ForegroundColor Cyan

if ($WhatIf) {
    Write-Host "(WhatIf) Se copiaria el contenido de dist/" -ForegroundColor Yellow
    exit 0
}

if (Test-Path $TargetDir) {
    Remove-Item -Recurse -Force $TargetDir
}
New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
Copy-Item -Path (Join-Path $dist "*") -Destination $TargetDir -Recurse -Force

$index = Join-Path $TargetDir "index.html"
$html = Get-Content -Raw $index
if ($html -notmatch '/app/assets/') {
    throw "index.html en destino no referencia /app/assets/"
}

Write-Host "OK  SPA publicada en $TargetDir" -ForegroundColor Green
Write-Host "    Docker: volumen en docker-compose.strangler.yml -> dist/ del repo" -ForegroundColor Gray
