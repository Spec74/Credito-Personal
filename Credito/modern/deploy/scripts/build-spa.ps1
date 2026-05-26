# Build SPA para cutover (/app/). Uso desde Credito\modern:
#   .\deploy\scripts\build-spa.ps1

$ErrorActionPreference = "Stop"
$scriptsDir = $PSScriptRoot
$modernRoot = Resolve-Path (Join-Path $scriptsDir "..\..")
$webRoot = Join-Path $modernRoot "Credito.Modern.Web"

if (-not (Test-Path (Join-Path $webRoot "package.json"))) {
    throw "No existe Credito.Modern.Web\package.json en $webRoot"
}

Write-Host ">> npm install (si hace falta)" -ForegroundColor Cyan
Push-Location $webRoot
try {
    if (-not (Test-Path "node_modules")) {
        npm install
        if ($LASTEXITCODE -ne 0) { throw "npm install fallo" }
    }

    Write-Host ">> npm run lint" -ForegroundColor Cyan
    npm run lint
    if ($LASTEXITCODE -ne 0) { throw "npm run lint fallo (exit $LASTEXITCODE)" }

    Write-Host ">> npm run lint" -ForegroundColor Cyan
    npm run lint
    if ($LASTEXITCODE -ne 0) { throw "npm run lint fallo (exit $LASTEXITCODE)" }

    Write-Host ">> npm run build (VITE_BASE_URL=/app/)" -ForegroundColor Cyan
    $env:VITE_BASE_URL = "/app/"
    npm run build
    if ($LASTEXITCODE -ne 0) { throw "npm run build fallo (exit $LASTEXITCODE)" }

    $distIndex = Join-Path $webRoot "dist\index.html"
    if (-not (Test-Path $distIndex)) {
        throw "No se genero dist/index.html"
    }
    $html = Get-Content -Raw $distIndex
    if ($html -notmatch '/app/assets/') {
        throw "dist/index.html no referencia /app/assets/ - revisar VITE_BASE_URL"
    }
    Write-Host "OK  SPA en $distIndex" -ForegroundColor Green
}
finally {
    Pop-Location
}
