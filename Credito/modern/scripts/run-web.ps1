# Sirve dist/ en http://localhost:5173/app/ (base path /app).
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot -Parent
$webRoot = Join-Path $root 'Credito.Modern.Web'
$dist = Join-Path $webRoot 'dist'
$port = 5173

if (-not (Test-Path (Join-Path $dist 'index.html'))) {
  Write-Error "No se encontró dist/index.html en $dist"
}

Write-Host "Sirviendo SPA: http://localhost:${port}/app/"
Write-Host 'Ctrl+C para detener.'

# Estructura: /app -> dist (coincide con Vite base /app)
$appLink = Join-Path $webRoot 'app'
if (-not (Test-Path $appLink)) {
  cmd /c mklink /J "$appLink" "$dist" | Out-Null
}

Set-Location $webRoot
npx --yes serve . -l $port -s
