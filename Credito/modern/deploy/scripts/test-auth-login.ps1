# Prueba login API moderna (misma BD que MVC) detras del proxy o API directa.
# Uso:
#   .\deploy\scripts\test-auth-login.ps1 -NombreUsuario TU_USER -Clave TU_CLAVE -OficinaId 1
#   .\deploy\scripts\test-auth-login.ps1 -BaseUrl http://localhost:5080 -NombreUsuario ... -Clave ... -OficinaId 1
#   .\deploy\scripts\test-auth-login.ps1 ... -ClienteAcceso "1.2.3.4"   # tk MAESTRO.Acceso (como MVC)

param(
    [string]$BaseUrl = "http://localhost:9080",
    [Parameter(Mandatory = $true)]
    [string]$NombreUsuario,
    [Parameter(Mandatory = $true)]
    [string]$Clave,
    [int]$OficinaId = 1,
    [string]$ClienteAcceso = ""
)

$ErrorActionPreference = "Stop"

if ([string]::IsNullOrWhiteSpace($ClienteAcceso)) {
    try {
        $ipRes = Invoke-RestMethod -Uri "https://api.ipify.org?format=json" -TimeoutSec 10
        $ClienteAcceso = $ipRes.ip
        Write-Host "clienteAcceso (tk) desde ipify: $ClienteAcceso" -ForegroundColor Gray
    }
    catch {
        Write-Host "AVISO: no se obtuvo IP publica; login sin clienteAcceso (Staging no lo exige)." -ForegroundColor Yellow
    }
}

$body = @{
    nombreUsuario = $NombreUsuario
    clave           = $Clave
    oficinaId       = $OficinaId
}
if (-not [string]::IsNullOrWhiteSpace($ClienteAcceso)) {
    $body.clienteAcceso = $ClienteAcceso
}

$loginUrl = ($BaseUrl.TrimEnd("/") + "/api/v1/auth/login")
Write-Host "POST $loginUrl" -ForegroundColor Cyan

try {
    $res = Invoke-WebRequest -Uri $loginUrl -Method Post -Body ($body | ConvertTo-Json) -ContentType "application/json" -UseBasicParsing
}
catch {
    $resp = $_.Exception.Response
    if ($resp) {
        $reader = [System.IO.StreamReader]::new($resp.GetResponseStream())
        $detail = $reader.ReadToEnd()
        Write-Host "HTTP $([int]$resp.StatusCode)" -ForegroundColor Red
        Write-Host $detail
        throw "Login API fallo."
    }
    throw
}

Write-Host "OK login HTTP $($res.StatusCode)" -ForegroundColor Green
$json = $res.Content | ConvertFrom-Json
Write-Host "  usuarioId=$($json.usuarioId) oficinaId=$($json.oficinaId) expiresIn=$($json.expiresInSeconds)s"

$token = $json.accessToken
$meUrl = ($BaseUrl.TrimEnd("/") + "/api/v1/auth/me")
$me = Invoke-WebRequest -Uri $meUrl -Headers @{ Authorization = "Bearer $token" } -UseBasicParsing
Write-Host "OK GET auth/me HTTP $($me.StatusCode)" -ForegroundColor Green
Write-Host $me.Content

$menuUrl = ($BaseUrl.TrimEnd("/") + "/api/v1/menu")
$menu = Invoke-WebRequest -Uri $menuUrl -Headers @{ Authorization = "Bearer $token" } -UseBasicParsing
Write-Host "OK GET menu HTTP $($menu.StatusCode) (items en JSON)" -ForegroundColor Green

Write-Host "`nLogin API verificado." -ForegroundColor Green
