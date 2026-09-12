# Smoke de lectura contra la API local (sin Docker).
# Requiere: API en marcha (https://localhost:7288). No arranca un segundo dotnet run.
#
#   .\deploy\scripts\smoke-local-api.ps1
#   .\deploy\scripts\smoke-local-api.ps1 -BaseUrl "https://localhost:7288" -SkipJwt

param(
    [string]$BaseUrl = "https://localhost:7288",
    [int]$UsuarioId = 1,
    [int]$OficinaId = 1,
    [string]$NombreUsuario = "",
    [string]$Clave = "",
    [switch]$SkipJwt
)

$ErrorActionPreference = "Stop"
[System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
if ($PSVersionTable.PSVersion.Major -lt 6) {
    add-type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class SmokeTrustAllCerts : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) { return true; }
}
"@
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object SmokeTrustAllCerts
}

function Get-ApiUrl([string]$path) {
    return ($BaseUrl.TrimEnd("/") + $path)
}

function Write-Step([string]$msg) { Write-Host "`n==> $msg" -ForegroundColor Cyan }

function Assert-Status($response, $allowed) {
    if ($response.StatusCode -notin $allowed) {
        throw "HTTP $($response.StatusCode) - se esperaba $($allowed -join ' o '). URI: $($response.RequestMessage.RequestUri)"
    }
}

function Invoke-ApiRequest {
    param(
        [string]$Uri,
        [Microsoft.PowerShell.Commands.WebRequestMethod]$Method = "Get",
        [hashtable]$Headers = @{},
        [string]$Body = $null
    )
    $params = @{
        Uri = $Uri
        Method = $Method
        Headers = $Headers
        UseBasicParsing = $true
    }
    if ($PSVersionTable.PSVersion.Major -ge 6) {
        $params.SkipCertificateCheck = $true
    }
    if ($Body) {
        $params.Body = $Body
        $params.ContentType = "application/json"
    }
    return Invoke-WebRequest @params
}

$correlationId = [guid]::NewGuid().ToString("N")
$proxyHeaders = @{ "X-Correlation-ID" = $correlationId }

Write-Step "Health ($BaseUrl/health)"
try {
    $health = Invoke-ApiRequest -Uri (Get-ApiUrl "/health") -Headers $proxyHeaders
}
catch {
    throw "No se alcanzo la API en $BaseUrl. Arranca: `$env:ASPNETCORE_ENVIRONMENT='Development'; dotnet run --project Credito.Modern.Api --launch-profile Credito.Modern.Api"
}

Assert-Status $health @(200)

Write-Step "401 sin JWT"
# /menu en Development (PermiteParametrosQuery) responde 400 si faltan oficina/usuario; no es 401.
$unauthUrls = @(
    (Get-ApiUrl "/api/v1/reportes/catalogo"),
    (Get-ApiUrl "/api/v1/credito/caja-diario-sesion"),
    (Get-ApiUrl "/api/v1/prendario/resumen")
)
foreach ($url in $unauthUrls) {
    try {
        Invoke-ApiRequest -Uri $url | Out-Null
        throw "Se esperaba 401 sin Bearer: $url"
    }
    catch {
        $resp = $_.Exception.Response
        if (-not $resp) { throw }
        $code = [int]$resp.StatusCode
        if ($code -ne 401) {
            throw "Sin JWT ($url): HTTP $code (esperado 401)"
        }
    }
}

if ($SkipJwt) {
    Write-Host "`nSmoke local basico OK (sin JWT)." -ForegroundColor Green
    exit 0
}

$accessToken = $null
if ([string]::IsNullOrWhiteSpace($NombreUsuario)) {
    Write-Step "Dev token POST /api/v1/dev/token"
    $tokenBody = @{ usuarioId = $UsuarioId; oficinaId = $OficinaId } | ConvertTo-Json
    try {
        $tokenRes = Invoke-ApiRequest -Uri (Get-ApiUrl "/api/v1/dev/token") -Method Post -Body $tokenBody
        Assert-Status $tokenRes @(200)
        $accessToken = ($tokenRes.Content | ConvertFrom-Json).accessToken
    }
    catch {
        Write-Host "dev/token no disponible. Usa -NombreUsuario/-Clave o -SkipJwt." -ForegroundColor Yellow
        Write-Host "`nSmoke local 401 OK; JWT omitido." -ForegroundColor Green
        exit 0
    }
}
else {
    Write-Step "Login POST /api/v1/auth/login"
    $loginBody = @{
        nombreUsuario = $NombreUsuario
        clave = $Clave
        oficinaId = $OficinaId
    } | ConvertTo-Json
    $loginRes = Invoke-ApiRequest -Uri (Get-ApiUrl "/api/v1/auth/login") -Method Post -Body $loginBody
    Assert-Status $loginRes @(200)
    $accessToken = ($loginRes.Content | ConvertFrom-Json).accessToken
}

$headers = @{ Authorization = "Bearer $accessToken"; "X-Correlation-ID" = $correlationId }

Write-Step "GET /api/v1/auth/me"
Assert-Status (Invoke-ApiRequest -Uri (Get-ApiUrl "/api/v1/auth/me") -Headers $headers) @(200)

Write-Step "GET /api/v1/menu"
Assert-Status (Invoke-ApiRequest -Uri (Get-ApiUrl "/api/v1/menu") -Headers $headers) @(200)

Write-Step "GET /api/v1/database-time"
Assert-Status (Invoke-ApiRequest -Uri (Get-ApiUrl "/api/v1/database-time")) @(200)

Write-Host "`nSmoke local API completado." -ForegroundColor Green
