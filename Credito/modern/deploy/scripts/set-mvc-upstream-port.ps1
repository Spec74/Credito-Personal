# Actualiza el puerto del MVC legado en nginx y reaplica el contenedor proxy.
# Edita solo deploy/nginx/default.conf (upstream + Host + proxy_redirect).
# Uso: .\deploy\scripts\set-mvc-upstream-port.ps1 -Port 4779
#      .\deploy\scripts\set-mvc-upstream-port.ps1   # puerto desde Web.csproj (4779)

param(
    [int]$Port = 0
)

if ($Port -lt 1) {
    $Port = & (Join-Path $PSScriptRoot "resolve-mvc-dev-port.ps1") -AsInt
    Write-Host "Puerto MVC desde Web.csproj: $Port" -ForegroundColor Gray
}

$ErrorActionPreference = "Stop"
$modernRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$defaultConf = Join-Path $modernRoot "deploy\nginx\default.conf"
$composeFile = Join-Path $modernRoot "deploy\docker-compose.strangler.yml"
$envFile = Join-Path $modernRoot "deploy\.env"

if (-not (Test-Path $defaultConf)) {
    throw "No existe $defaultConf"
}

$content = Get-Content $defaultConf -Raw

if ($content -notmatch 'host\.docker\.internal:\d+') {
    throw 'No se encontro upstream host.docker.internal:PUERTO en default.conf'
}

$newContent = $content
$newContent = $newContent -replace 'host\.docker\.internal:\d+', "host.docker.internal:$Port"
$newContent = $newContent -replace 'proxy_redirect http://localhost:\d+/', "proxy_redirect http://localhost:$Port/"
$newContent = $newContent -replace 'proxy_redirect http://127\.0\.0\.1:\d+/', "proxy_redirect http://127.0.0.1:$Port/"

if ($newContent -eq $content) {
    Write-Host "Puerto MVC ya era $Port en default.conf." -ForegroundColor Yellow
}
else {
    Set-Content -Path $defaultConf -Value $newContent -NoNewline
    Write-Host "Puerto MVC -> $Port (default.conf)" -ForegroundColor Green
}

Push-Location $modernRoot
try {
    # Importante: NO usar solo "restart". Si cambia compose/volumenes, hay que "up -d"
    # para recrear el contenedor y aplicar montajes nuevos.
    if (-not (Test-Path $envFile)) {
        throw 'Falta deploy\.env (copia desde deploy\.env.example)'
    }
    $prevEap = $ErrorActionPreference
    $ErrorActionPreference = 'SilentlyContinue'
    try {
        $lines = & docker compose -f $composeFile --env-file $envFile up -d strangler-proxy --force-recreate 2>&1
        foreach ($line in $lines) {
            if ($null -eq $line) { continue }
            if ($line -is [System.Management.Automation.ErrorRecord]) {
                $t = $line.Exception.Message.Trim()
                if ($t) { Write-Host $t -ForegroundColor Gray }
            }
            else {
                Write-Host ($line.ToString().Trim()) -ForegroundColor Gray
            }
        }
        if ($LASTEXITCODE -ne 0) {
            throw "docker compose up strangler-proxy salio con codigo $LASTEXITCODE"
        }
    }
    finally {
        $ErrorActionPreference = $prevEap
    }
    Write-Host 'Proxy aplicado. MVC en marcha -> http://localhost:9080/' -ForegroundColor Gray
}
finally {
    Pop-Location
}
