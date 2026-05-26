# Puerto del MVC local segun Web.csproj y/o proceso en escucha.
# Uso:
#   .\resolve-mvc-dev-port.ps1
#   .\resolve-mvc-dev-port.ps1 -Profile IISExpress
#   .\resolve-mvc-dev-port.ps1 -AsInt

param(
    [ValidateSet("Auto", "DevServer", "IISExpress")]
    [string]$Profile = "Auto",
    [switch]$AsInt
)

$ErrorActionPreference = "Stop"
$modernRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$webCsproj = Join-Path $modernRoot "..\Web\Web.csproj"

if (-not (Test-Path $webCsproj)) {
    throw "No se encontro Web.csproj en $webCsproj"
}

$text = Get-Content $webCsproj -Raw
if ($text -notmatch '<DevelopmentServerPort>(\d+)</DevelopmentServerPort>') {
    throw "DevelopmentServerPort no encontrado en Web.csproj"
}
$devPort = [int]$Matches[1]

$iisPort = 0
if ($text -match '<IISUrl>https?://[^:]+:(\d+)/?</IISUrl>') {
    $iisPort = [int]$Matches[1]
}

function Test-TcpPort([int]$Port) {
    try {
        $client = New-Object System.Net.Sockets.TcpClient
        $client.Connect("127.0.0.1", $Port)
        $client.Close()
        return $true
    }
    catch {
        return $false
    }
}

$port = $devPort
$reason = "DevelopmentServerPort (servidor web VS)"

if ($Profile -eq "IISExpress") {
    if ($iisPort -lt 1) { throw "IISUrl no encontrado en Web.csproj" }
    $port = $iisPort
    $reason = "IIS Express (IISUrl)"
}
elseif ($Profile -eq "DevServer") {
    $port = $devPort
}
else {
    $iisListening = ($iisPort -gt 0) -and (Test-TcpPort $iisPort)
    $devListening = Test-TcpPort $devPort
    if ($iisListening) {
        $port = $iisPort
        $reason = "IIS Express escuchando en $iisPort"
    }
    elseif ($devListening) {
        $port = $devPort
        $reason = "Servidor dev VS escuchando en $devPort"
    }
    else {
        Write-Host "AVISO: ningun puerto MVC respondio en 127.0.0.1 ($devPort ni $iisPort). Usando IIS Express $iisPort si F5 con IIS." -ForegroundColor Yellow
        if ($iisPort -gt 0) {
            $port = $iisPort
            $reason = "IIS Express (por defecto Auto sin proceso)"
        }
    }
}

if (-not $AsInt) {
    Write-Host "Puerto MVC: $port ($reason)" -ForegroundColor Gray
    Write-Host "  DevServer=$devPort  IISExpress=$iisPort" -ForegroundColor DarkGray
}

if ($AsInt) { $port }
