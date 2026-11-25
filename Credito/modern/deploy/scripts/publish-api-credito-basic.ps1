# Publica Credito.Modern.Api a App Service api-credito-basic (B1).
# Requisitos: Azure CLI (az login) o perfil de publicacion (.PublishSettings).
#
# Ejemplos:
#   .\deploy\scripts\publish-api-credito-basic.ps1
#   .\deploy\scripts\publish-api-credito-basic.ps1 -PublishProfilePath "$env:USERPROFILE\Downloads\api-credito-basic.PublishSettings"

param(
    [string]$ResourceGroup = "rg-credito-basic",
    [string]$AppName = "api-credito-basic",
    [string]$PublishProfilePath = "",
    [switch]$SkipBuild
)

$ErrorActionPreference = "Stop"
$root = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
if (-not (Test-Path (Join-Path $root "Credito.Modern.Api\Credito.Modern.Api.csproj"))) {
    $root = Join-Path (Split-Path $PSScriptRoot -Parent) ""
    $root = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
}
$modernRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
if (-not (Test-Path (Join-Path $modernRoot "Credito.Modern.Api\Credito.Modern.Api.csproj"))) {
    throw "No se encontro Credito.Modern.Api.csproj desde $modernRoot"
}

$outDir = Join-Path $modernRoot "deploy\out\api-credito-basic"
$zipPath = Join-Path $modernRoot "deploy\out\api-credito-basic.zip"

Write-Host "==> Publish Release -> $outDir" -ForegroundColor Cyan
if (-not $SkipBuild) {
    if (Test-Path $outDir) { Remove-Item $outDir -Recurse -Force }
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    dotnet publish (Join-Path $modernRoot "Credito.Modern.Api\Credito.Modern.Api.csproj") `
        -c Release -o $outDir --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw "dotnet publish fallo ($LASTEXITCODE)" }
}

if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
Compress-Archive -Path (Join-Path $outDir "*") -DestinationPath $zipPath -Force
Write-Host "ZIP: $zipPath ($([math]::Round((Get-Item $zipPath).Length / 1MB, 1)) MB)" -ForegroundColor Gray

function Deploy-WithAz {
    Write-Host "==> az webapp deploy ($AppName)" -ForegroundColor Cyan
    az webapp config set `
        --resource-group $ResourceGroup `
        --name $AppName `
        --linux-fx-version "DOTNETCORE|10.0" `
        --startup-file "dotnet Credito.Modern.Api.dll" `
        --only-show-errors | Out-Null
    az webapp deploy `
        --resource-group $ResourceGroup `
        --name $AppName `
        --src-path $zipPath `
        --type zip `
        --async false `
        --only-show-errors
    if ($LASTEXITCODE -ne 0) { throw "az webapp deploy fallo ($LASTEXITCODE)" }
    az webapp restart --resource-group $ResourceGroup --name $AppName --only-show-errors | Out-Null
}

function Deploy-WithPublishProfile([string]$profilePath) {
    Write-Host "==> Kudu zipdeploy via publish profile" -ForegroundColor Cyan
    [xml]$xml = Get-Content -Raw $profilePath
    $profile = $xml.publishData.publishProfile |
        Where-Object { $_.publishMethod -eq "MSDeploy" -or $_.publishMethod -eq "ZipDeploy" } |
        Select-Object -First 1
    if (-not $profile) { throw "No hay perfil MSDeploy/ZipDeploy en $profilePath" }

    $user = $profile.userName
    $pass = $profile.userPWD
    $scm = $null
    if ($profile.publishUrl -match "^(?<host>[^:]+)") {
        $hostName = $Matches.host
        if ($hostName -notlike "*.scm.*") {
            $hostName = $hostName -replace "\.azurewebsites\.net$", ".scm.azurewebsites.net"
        }
        $scm = "https://$hostName"
    }
    if (-not $scm) {
        $scm = "https://$AppName.scm.azurewebsites.net"
    }

    $pair = "{0}:{1}" -f $user, $pass
    $basic = [Convert]::ToBase64String([Text.Encoding]::ASCII.GetBytes($pair))
    $uri = "$scm/api/zipdeploy?isAsync=false"
    Write-Host "POST $uri" -ForegroundColor Gray
    Invoke-RestMethod -Uri $uri -Method Post -Headers @{ Authorization = "Basic $basic" } `
        -InFile $zipPath -ContentType "application/octet-stream" -TimeoutSec 600
}

$azCmd = Get-Command az -ErrorAction SilentlyContinue
if (-not $azCmd) {
    $azGuess = Join-Path ${env:ProgramFiles(x86)} "Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
    if (-not (Test-Path $azGuess)) {
        $azGuess = Join-Path $env:ProgramFiles "Microsoft SDKs\Azure\CLI2\wbin\az.cmd"
    }
    if (Test-Path $azGuess) { $azCmd = $azGuess }
}

if ($PublishProfilePath) {
    if (-not (Test-Path $PublishProfilePath)) { throw "No existe: $PublishProfilePath" }
    Deploy-WithPublishProfile $PublishProfilePath
}
elseif ($azCmd) {
    if ($azCmd -is [string]) {
        Set-Alias -Name az -Value $azCmd -Scope Script
    }
    $account = az account show 2>$null
    if (-not $account) {
        Write-Host "Inicia sesion Azure (se abrira el navegador)..." -ForegroundColor Yellow
        az login | Out-Null
    }
    Deploy-WithAz
}
else {
    throw @"
No hay Azure CLI ni -PublishProfilePath.
1) Instala Azure CLI y ejecuta: az login
   o
2) En Portal → api-credito-basic → Descargar perfil de publicacion
   y vuelve a correr con -PublishProfilePath ruta\al\archivo.PublishSettings
"@
}

$defaultHost = "https://$AppName.azurewebsites.net"
Write-Host "==> Esperando health..." -ForegroundColor Cyan
Start-Sleep -Seconds 20
try {
    $h = Invoke-WebRequest -Uri "$defaultHost/health" -UseBasicParsing -TimeoutSec 60
    Write-Host "GET /health -> $($h.StatusCode)" -ForegroundColor Green
}
catch {
    Write-Host "AVISO: /health aun no responde en $defaultHost (revisa dominio real en Overview y logs)." -ForegroundColor Yellow
    Write-Host $_.Exception.Message -ForegroundColor Yellow
}

Write-Host "`nDeploy listo. Dominio real: Overview del App Service (…azurewebsites.net)." -ForegroundColor Green
