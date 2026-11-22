# Build + push imagen Docker (con tesseract-ocr spa) y opcionalmente apunta App Service.
#
# Prerrequisitos:
#   - Docker Desktop en marcha
#   - docker login (Docker Hub) O az acr login
#
# Ejemplo Docker Hub:
#   .\deploy\scripts\publish-api-container.ps1 -ImageName "TU_USUARIO/credito-api:basic"
#
# Luego en Portal → api-credito-basic → Centro de implementación → Contenedor:
#   Imagen: TU_USUARIO/credito-api:basic
#   Puerto: 8080
#   Startup: (vacío; ENTRYPOINT del Dockerfile)

param(
    [Parameter(Mandatory = $true)]
    [string]$ImageName,
    [switch]$SkipPush,
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$modernRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$dockerfile = Join-Path $modernRoot "Dockerfile"
if (-not (Test-Path $dockerfile)) {
    throw "No existe Dockerfile en $modernRoot"
}

Write-Host "==> docker version" -ForegroundColor Cyan
docker version | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Docker no responde. Abre Docker Desktop y espera a que diga Running."
}

if (-not $SkipBuild) {
    Write-Host "==> docker build -t $ImageName (contexto: modern/)" -ForegroundColor Cyan
    docker build -t $ImageName -f $dockerfile $modernRoot
    if ($LASTEXITCODE -ne 0) { throw "docker build fallo ($LASTEXITCODE)" }
}

if (-not $SkipPush) {
    Write-Host "==> docker push $ImageName" -ForegroundColor Cyan
    docker push $ImageName
    if ($LASTEXITCODE -ne 0) {
        throw "docker push fallo. Ejecuta: docker login"
    }
}

Write-Host @"

Listo. Configura App Service como contenedor:

1) Portal → api-credito-basic → Centro de implementación (o Contenedor)
2) Origen: Docker Hub (o ACR)
3) Imagen: $ImageName
4) Puerto: 8080
5) Guarda y espera reinicio
6) Prueba: https://api-credito-basic-drdvfqfda2hjbcb6.westcentralus-01.azurewebsites.net/health

Variables de entorno (connection string, JWT, CORS) se conservan.
Verificar OCR en Kudu/SSH: tesseract --version

"@ -ForegroundColor Green
